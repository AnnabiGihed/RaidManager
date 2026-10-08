using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RaidManager.Companion.Client.Features.Sync.Discovery;
using RaidManager.Companion.Client.Features.Sync.Queue;
using RaidManager.Companion.Client.Features.Sync.SavedVariables;
using RaidManager.Companion.Client.Features.Sync.Settings;
using RaidManager.Companion.Client.Features.Sync.Upload;
using RaidManager.Companion.Client.Features.Sync.Watch;

namespace RaidManager.Companion.Client.Features.Sync;

/// <summary>Runs the companion's background sync: finds WoW, watches the addon's files, queues and uploads snapshots.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-07<br/>
/// Purpose: The background sync of #550 (story #17's second criterion, and the discovery, exclusion and pause behind
/// its first). Every second it uploads what is due unless paused; every five seconds it checks the watched files.
/// At first start it searches the usual places of every fixed drive. A pause stops uploads only: files are still read
/// and queued, and resuming uploads at once (owner decision on #550). The player's choices and the queue are kept in
/// the user's profile. One lock serializes the loop and the player's actions.
/// </remarks>
internal sealed partial class SnapshotSync : BackgroundService, ISnapshotSync
{
    #region Fields
    /// <summary>Stores how long after start the first step runs, so the companion's window and tray come up first.</summary>
    public static readonly TimeSpan StartDelay = TimeSpan.FromSeconds(2);

    /// <summary>Stores how often the loop runs.</summary>
    public static readonly TimeSpan TickInterval = TimeSpan.FromSeconds(1);

    /// <summary>Stores how often the watched files are checked.</summary>
    public static readonly TimeSpan ScanInterval = TimeSpan.FromSeconds(5);

    /// <summary>Stores the lock that serializes the loop and the player's actions.</summary>
    private readonly SemaphoreSlim _gate = new(1, 1);

    /// <summary>Stores the lock that guards the state the status reads.</summary>
    private readonly Lock _state = new();

    /// <summary>Stores the settings file.</summary>
    private readonly SyncSettingsStore _settingsStore;

    /// <summary>Stores the folder search.</summary>
    private readonly WowInstallationFinder _finder;

    /// <summary>Stores the queue.</summary>
    private readonly SnapshotQueue _queue;

    /// <summary>Stores the uploader.</summary>
    private readonly SnapshotUploader _uploader;

    /// <summary>Stores the clock.</summary>
    private readonly TimeProvider _time;

    /// <summary>Stores the logger.</summary>
    private readonly ILogger<SnapshotSync> _logger;

    /// <summary>Stores the watch of each watched account, by hash.</summary>
    private readonly Dictionary<string, SavedVariablesWatch> _watches = new(StringComparer.Ordinal);

    /// <summary>Stores the account problems, by hash.</summary>
    private readonly Dictionary<string, AccountProblem> _problems = new(StringComparer.Ordinal);

    /// <summary>Stores the characters of a cut file still waiting for WoW, by account hash.</summary>
    private readonly Dictionary<string, IReadOnlyList<CharacterKey>> _waitingForWow = new(StringComparer.Ordinal);

    /// <summary>Stores the player's choices.</summary>
    private SyncSettings _settings;

    /// <summary>Stores the watched installations.</summary>
    private IReadOnlyList<WowInstallation> _installations = [];

    /// <summary>Stores whether a folder search runs.</summary>
    private bool _searching;

    /// <summary>Stores when the watched files are checked next.</summary>
    private DateTimeOffset _nextScan = DateTimeOffset.MinValue;
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="SnapshotSync"/> class, reads the player's choices and their folders.</summary>
    /// <param name="settingsStore">The settings file.</param>
    /// <param name="finder">The folder search.</param>
    /// <param name="queue">The queue.</param>
    /// <param name="uploader">The uploader.</param>
    /// <param name="time">The clock.</param>
    /// <param name="logger">The logger.</param>
    public SnapshotSync(
        SyncSettingsStore settingsStore,
        WowInstallationFinder finder,
        SnapshotQueue queue,
        SnapshotUploader uploader,
        TimeProvider time,
        ILogger<SnapshotSync> logger)
    {
        _settingsStore = settingsStore;
        _finder = finder;
        _queue = queue;
        _uploader = uploader;
        _time = time;
        _logger = logger;
        _settings = settingsStore.Load();
        Refresh();
        _uploader.Changed += (_, _) => OnStatusChanged();
    }
    #endregion Constructors

    #region Events
    /// <inheritdoc />
    public event EventHandler? StatusChanged;
    #endregion Events

    #region Public Properties
    /// <inheritdoc />
    public SyncStatus Status
    {
        get
        {
            lock (_state)
            {
                return new SyncStatus(
                    _settings.Paused,
                    _searching,
                    _uploader.Connection,
                    _uploader.LastSuccess,
                    _queue.Count,
                    _installations,
                    [.. _settings.ExcludedAccounts],
                    [.. _settings.ExcludedFolderList],
                    Activity(),
                    [.. _problems.Values],
                    _uploader.Refusals);
            }
        }
    }
    #endregion Public Properties

    #region Public Methods
    /// <summary>Runs one step of the loop: the first search, the scan when due, then the uploads when not paused.</summary>
    /// <param name="cancellationToken">A token to stop.</param>
    /// <returns>A task that completes when the step is done.</returns>
    public async Task TickAsync(CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (_settings.FoundFolders is null)
            {
                Search(cancellationToken);
            }

            var now = _time.GetUtcNow();
            if (now >= _nextScan)
            {
                _nextScan = now + ScanInterval;
                Scan(now);
            }
        }
        finally
        {
            _gate.Release();
        }

        if (!_settings.Paused)
        {
            await _uploader.UploadDueAsync(cancellationToken);
        }
    }

    /// <inheritdoc />
    public Task PauseAsync(CancellationToken cancellationToken) =>
        ChangeAsync(() => Save(_settings with { Paused = true }), cancellationToken);

    /// <inheritdoc />
    public Task ResumeAsync(CancellationToken cancellationToken) =>
        ChangeAsync(
            () =>
            {
                Save(_settings with { Paused = false });
                _uploader.RetryNow();
            },
            cancellationToken);

    /// <inheritdoc />
    public void RetryNow() => _uploader.RetryNow();

    /// <inheritdoc />
    public Task ReadAgainAsync(CancellationToken cancellationToken) =>
        ChangeAsync(
            () =>
            {
                lock (_state)
                {
                    foreach (var watch in _watches.Values)
                    {
                        watch.ReadAgain();
                    }

                    // Back to "waiting for WoW" until the next read says otherwise, as board 5's retry leads to board 2.
                    foreach (var problem in _problems.Values.Where(problem => problem.Kind == AccountProblemKind.IncompleteSnapshot))
                    {
                        _waitingForWow[problem.AccountId] = problem.Characters;
                    }

                    _problems.Clear();
                }

                _uploader.ForgetRefusals();
                _nextScan = DateTimeOffset.MinValue;
            },
            cancellationToken);

    /// <inheritdoc />
    public Task FindFoldersAgainAsync(CancellationToken cancellationToken) =>
        ChangeAsync(() => Search(cancellationToken), cancellationToken);

    /// <inheritdoc />
    public async Task<bool> AddFolderAsync(string folder, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(folder);
        var added = false;
        await ChangeAsync(
            () =>
            {
                var installation = WowInstallationFinder.Describe(folder);
                if (installation is null)
                {
                    return;
                }

                added = true;
                Save(_settings with { AddedFolders = [.. _settings.AddedFolders, installation.Folder] });
            },
            cancellationToken);
        return added;
    }

    /// <inheritdoc />
    public Task SetAccountWatchedAsync(string accountId, bool watched, CancellationToken cancellationToken) =>
        ChangeAsync(
            () =>
            {
                var excluded = _settings.ExcludedAccounts.Where(id => id != accountId);
                Save(_settings with { ExcludedAccounts = watched ? [.. excluded] : [.. excluded, accountId] });
                if (!watched)
                {
                    _queue.DropAccount(accountId);
                }
            },
            cancellationToken);

    /// <inheritdoc />
    public Task SetFolderWatchedAsync(string folder, bool watched, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(folder);
        return ChangeAsync(
            () =>
            {
                var excluded = _settings.ExcludedFolderList.Where(path => !string.Equals(path, folder, StringComparison.OrdinalIgnoreCase));
                Save(_settings with { ExcludedFolders = watched ? [.. excluded] : [.. excluded, folder] });
                if (!watched && WowInstallationFinder.Describe(folder) is { } installation)
                {
                    foreach (var account in installation.Accounts)
                    {
                        _queue.DropAccount(account.Id);
                    }
                }
            },
            cancellationToken);
    }

    /// <inheritdoc />
    public override void Dispose()
    {
        _gate.Dispose();
        base.Dispose();
    }
    #endregion Public Methods

    #region Protected Methods
    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(StartDelay, _time, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await TickAsync(stoppingToken);
                await Task.Delay(TickInterval, _time, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                // A drive or file that disappears mid-scan must not stop the sync; the next tick tries again.
                LogTickFailed(_logger, exception);
                await Task.Delay(TickInterval, _time, stoppingToken);
            }
        }
    }
    #endregion Protected Methods

    #region Private Helpers
    /// <summary>Writes that a step of the loop failed.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="exception">The failure.</param>
    [LoggerMessage(Level = LogLevel.Warning, Message = "A step of the character sync failed; it tries again.")]
    private static partial void LogTickFailed(ILogger logger, Exception exception);

    /// <summary>Writes how many installations a search found.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="count">The number found.</param>
    [LoggerMessage(Level = LogLevel.Information, Message = "The search for World of Warcraft found {Count} installations.")]
    private static partial void LogSearchFinished(ILogger logger, int count);

    /// <summary>Runs a change of the player's choices under the lock on a pool thread, then refreshes the watched folders.</summary>
    /// <param name="change">The change.</param>
    /// <param name="cancellationToken">A token to cancel the wait for the lock.</param>
    /// <returns>A task that completes when the change is done.</returns>
    private async Task ChangeAsync(Action change, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            // Off the caller's thread: the player's actions come from the UI thread, and a search reads whole drives.
            await Task.Run(
                () =>
                {
                    change();
                    Refresh();
                },
                cancellationToken);
        }
        finally
        {
            _gate.Release();
        }

        OnStatusChanged();
    }

    /// <summary>Searches every fixed drive and keeps what it found; the caller holds the lock.</summary>
    /// <param name="cancellationToken">A token to stop the search.</param>
    private void Search(CancellationToken cancellationToken)
    {
        SetSearching(true);
        try
        {
            var found = _finder.Search(cancellationToken);
            LogSearchFinished(_logger, found.Count);
            Save(_settings with { FoundFolders = found });
            Refresh();
        }
        finally
        {
            SetSearching(false);
        }
    }

    /// <summary>Saves the player's choices; the caller holds the lock.</summary>
    /// <param name="settings">The new choices.</param>
    private void Save(SyncSettings settings)
    {
        _settingsStore.Save(settings);
        lock (_state)
        {
            _settings = settings;
        }
    }

    /// <summary>Reads the watched folders' accounts again and keeps one watch per watched account; the caller holds the lock.</summary>
    private void Refresh()
    {
        var installations = _settings.Folders.Select(WowInstallationFinder.Describe).OfType<WowInstallation>().ToList();
        var watched = installations
            .Where(installation => !_settings.ExcludedFolderList.Contains(installation.Folder, StringComparer.OrdinalIgnoreCase))
            .SelectMany(installation => installation.Accounts)
            .Where(account => !_settings.ExcludedAccounts.Contains(account.Id))
            .ToDictionary(account => account.Id, StringComparer.Ordinal);
        lock (_state)
        {
            _installations = installations;
            foreach (var id in _watches.Keys.Where(id => !watched.ContainsKey(id)).ToList())
            {
                _watches.Remove(id);
                _problems.Remove(id);
                _waitingForWow.Remove(id);
            }

            foreach (var (id, account) in watched)
            {
                if (!_watches.ContainsKey(id))
                {
                    _watches[id] = new SavedVariablesWatch(account);
                }
            }
        }
    }

    /// <summary>Checks every watched file, queues what was read and records problems; the caller holds the lock.</summary>
    /// <param name="now">The time of the scan.</param>
    private void Scan(DateTimeOffset now)
    {
        Refresh();
        var changed = false;
        foreach (var watch in _watches.Values.ToList())
        {
            if (watch.Check(now) is { } read)
            {
                Apply(watch.Account, read);
                changed = true;
            }
        }

        if (changed)
        {
            OnStatusChanged();
        }
    }

    /// <summary>Queues the complete characters of a read, and records what needs the player.</summary>
    /// <param name="account">The account the file belongs to.</param>
    /// <param name="read">The read.</param>
    private void Apply(WowAccount account, WatchRead read)
    {
        var file = read.File;
        foreach (var snapshot in file.Characters)
        {
            _queue.Offer(new QueuedSnapshot(
                account.Id, snapshot.Character.Realm, snapshot.Character.Name, snapshot.CapturedAt, file.AddonVersion, snapshot.Body));
        }

        lock (_state)
        {
            _waitingForWow.Remove(account.Id);
            _problems.Remove(account.Id);
            switch (file.Status)
            {
                case SavedVariablesReadStatus.Incomplete when !read.Final:
                    _waitingForWow[account.Id] = file.IncompleteCharacters;
                    break;
                case SavedVariablesReadStatus.Incomplete:
                    _problems[account.Id] = new AccountProblem(account.Id, AccountProblemKind.IncompleteSnapshot, file.IncompleteCharacters);
                    break;
                case SavedVariablesReadStatus.Unreadable:
                    _problems[account.Id] = new AccountProblem(account.Id, AccountProblemKind.UnreadableFile, []);
                    break;
                case SavedVariablesReadStatus.UnsupportedSchema:
                    _problems[account.Id] = new AccountProblem(account.Id, AccountProblemKind.UnsupportedSchema, [], file.AddonVersion);
                    break;
                default:
                    break;
            }
        }
    }

    /// <summary>Builds the recent activity: the upload in progress, the characters waiting for WoW, then the latest uploads.</summary>
    /// <returns>The activity; the caller holds the status lock.</returns>
    private List<CharacterActivity> Activity()
    {
        var activity = new List<CharacterActivity>();
        if (_uploader.Uploading is { } uploading)
        {
            activity.Add(new CharacterActivity(uploading, CharacterActivityState.Uploading));
        }

        activity.AddRange(_waitingForWow.Values.SelectMany(characters => characters)
            .Select(character => new CharacterActivity(character, CharacterActivityState.WaitingForWow)));
        activity.AddRange(_uploader.RecentUploads
            .Where(uploaded => activity.TrueForAll(row => row.Character != uploaded.Character))
            .Select(uploaded => new CharacterActivity(uploaded.Character, CharacterActivityState.Uploaded, uploaded.UploadedAt)));
        return activity;
    }

    /// <summary>Records whether a search runs, and tells the screens.</summary>
    /// <param name="searching">Whether a search runs.</param>
    private void SetSearching(bool searching)
    {
        lock (_state)
        {
            _searching = searching;
        }

        OnStatusChanged();
    }

    /// <summary>Raises <see cref="StatusChanged"/>.</summary>
    private void OnStatusChanged() => StatusChanged?.Invoke(this, EventArgs.Empty);
    #endregion Private Helpers
}
