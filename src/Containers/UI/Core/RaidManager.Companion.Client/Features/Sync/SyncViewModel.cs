using System.ComponentModel;
using System.Globalization;
using Microsoft.Extensions.Logging;
using RaidManager.Companion.Client.Features.Pairing;
using RaidManager.Companion.Client.Features.Shared;

namespace RaidManager.Companion.Client.Features.Sync;

/// <summary>Shows the background sync and takes the player's sync choices.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-08<br/>
/// Purpose: Boards 1 to 9 of <c>companion-sync</c> over the sync of #550 (#551, story #17's first and third criteria):
/// the watched folders with their accounts, a folder added through the picker, the pause, last success, queued
/// uploads, recent activity and the problems that need the player. The sync's status arrives on a background thread
/// and is brought to the window's thread before it is shown. Account choices wait for "Save and sync".
/// </remarks>
public sealed partial class SyncViewModel : ViewModelBase, IDisposable
{
    #region Constants
    /// <summary>Defines how many rows board 2's recent activity shows.</summary>
    private const int ActivityRowsShown = 4;
    #endregion Constants

    #region Fields
    /// <summary>Stores the properties that follow the status, raised together when it changes.</summary>
    private static readonly string[] StatusProperties =
    [
        nameof(Screen), nameof(IsWatchedFolders), nameof(IsSyncing), nameof(IsPaused), nameof(IsOffline),
        nameof(IsNeedingAttention), nameof(IsSearching), nameof(LastSuccessText), nameof(QueuedText),
        nameof(AccountsText), nameof(Activity), nameof(Problem), nameof(HasNoFolders),
    ];

    /// <summary>Stores the sync.</summary>
    private readonly ISnapshotSync _sync;

    /// <summary>Stores the folder picker.</summary>
    private readonly IFolderPicker _picker;

    /// <summary>Stores the window's thread.</summary>
    private readonly IUiThread _ui;

    /// <summary>Stores the pairing, for the footer.</summary>
    private readonly PairingViewModel _pairing;

    /// <summary>Stores the clock.</summary>
    private readonly TimeProvider _time;

    /// <summary>Stores the logger.</summary>
    private readonly ILogger<SyncViewModel> _logger;

    /// <summary>Stores the source that stops the player's actions when the companion ends.</summary>
    private readonly CancellationTokenSource _lifetime = new();

    /// <summary>Stores the status shown.</summary>
    private SyncStatus _status;

    /// <summary>Stores whether the player is on the watched folders (boards 1 and 9).</summary>
    private bool _showingFolders;

    /// <summary>Stores whether the last folder added wasn't a WoW installation (board 9).</summary>
    private bool _isFolderRejected;

    /// <summary>Stores the installations and saved exclusions the account rows were built from.</summary>
    private string _installationsKey = string.Empty;

    /// <summary>Stores the installation cards of board 1.</summary>
    private IReadOnlyList<WatchedInstallationViewModel> _installations = [];

    /// <summary>Stores whether the view model was disposed.</summary>
    private bool _disposed;
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="SyncViewModel"/> class.</summary>
    /// <param name="sync">The sync.</param>
    /// <param name="picker">The folder picker.</param>
    /// <param name="ui">The window's thread.</param>
    /// <param name="pairing">The pairing, whose player and computer the footer names.</param>
    /// <param name="time">The clock, in the player's time zone.</param>
    /// <param name="logger">The logger.</param>
    public SyncViewModel(
        ISnapshotSync sync,
        IFolderPicker picker,
        IUiThread ui,
        PairingViewModel pairing,
        TimeProvider time,
        ILogger<SyncViewModel> logger)
    {
        ArgumentNullException.ThrowIfNull(sync);
        ArgumentNullException.ThrowIfNull(pairing);
        _sync = sync;
        _picker = picker;
        _ui = ui;
        _pairing = pairing;
        _time = time;
        _logger = logger;
        _status = sync.Status;
        RebuildInstallations();
        SaveCommand = new AsyncRelayCommand(SaveAsync, OnActionFailed);
        FindFoldersAgainCommand = new AsyncRelayCommand(FindFoldersAgainAsync, OnActionFailed);
        AddFolderCommand = new AsyncRelayCommand(AddFolderAsync, OnActionFailed);
        PauseCommand = new AsyncRelayCommand(() => _sync.PauseAsync(_lifetime.Token), OnActionFailed);
        ResumeCommand = new AsyncRelayCommand(() => _sync.ResumeAsync(_lifetime.Token), OnActionFailed);
        RetryCommand = new AsyncRelayCommand(() => _sync.ReadAgainAsync(_lifetime.Token), OnActionFailed);
        RetryNowCommand = new RelayCommand(_sync.RetryNow);
        ShowFoldersCommand = new RelayCommand(ShowFolders);
        _sync.StatusChanged += OnStatusChanged;
        _pairing.PropertyChanged += OnPairingChanged;
    }
    #endregion Constructors

    #region Public Properties
    /// <summary>Gets the command behind "Save and sync" (board 1).</summary>
    public AsyncRelayCommand SaveCommand { get; }

    /// <summary>Gets the command behind "Find folders again" (board 1).</summary>
    public AsyncRelayCommand FindFoldersAgainCommand { get; }

    /// <summary>Gets the command behind "Add a folder" (board 1).</summary>
    public AsyncRelayCommand AddFolderCommand { get; }

    /// <summary>Gets the command behind "Pause sync" (board 2).</summary>
    public AsyncRelayCommand PauseCommand { get; }

    /// <summary>Gets the command behind "Resume sync" (board 3).</summary>
    public AsyncRelayCommand ResumeCommand { get; }

    /// <summary>Gets the command behind the retry of boards 5 to 8: every watched file is read again.</summary>
    public AsyncRelayCommand RetryCommand { get; }

    /// <summary>Gets the command behind "Retry now" (board 4).</summary>
    public RelayCommand RetryNowCommand { get; }

    /// <summary>Gets the command behind "Watched folders" (board 2).</summary>
    public RelayCommand ShowFoldersCommand { get; }

    /// <summary>Gets the board shown.</summary>
    public SyncScreen Screen
    {
        get
        {
            if (_showingFolders)
            {
                return SyncScreen.WatchedFolders;
            }

            if (SyncProblem.For(_status) is { } problem)
            {
                return problem.Screen;
            }

            return _status switch
            {
                { Paused: true } => SyncScreen.Paused,
                { Connection: Upload.UploadConnection.Offline } => SyncScreen.Offline,
                _ => SyncScreen.Syncing,
            };
        }
    }

    /// <summary>Gets a value indicating whether the watched folders are shown (boards 1 and 9).</summary>
    public bool IsWatchedFolders => Screen == SyncScreen.WatchedFolders;

    /// <summary>Gets a value indicating whether board 2 is shown.</summary>
    public bool IsSyncing => Screen == SyncScreen.Syncing;

    /// <summary>Gets a value indicating whether board 3 is shown.</summary>
    public bool IsPaused => Screen == SyncScreen.Paused;

    /// <summary>Gets a value indicating whether board 4 is shown.</summary>
    public bool IsOffline => Screen == SyncScreen.Offline;

    /// <summary>Gets a value indicating whether one of boards 5 to 8 is shown.</summary>
    public bool IsNeedingAttention => Screen is SyncScreen.IncompleteSnapshot or SyncScreen.RefusedSnapshot
        or SyncScreen.UnsupportedAddon or SyncScreen.UnreadableFile;

    /// <summary>Gets a value indicating whether the companion is searching for WoW folders.</summary>
    public bool IsSearching => _status.Searching;

    /// <summary>Gets a value indicating whether the last folder added wasn't a WoW installation (board 9).</summary>
    public bool IsFolderRejected
    {
        get => _isFolderRejected;
        private set => SetProperty(ref _isFolderRejected, value);
    }

    /// <summary>Gets a value indicating whether no WoW folder is known yet.</summary>
    public bool HasNoFolders => _installations.Count == 0;

    /// <summary>Gets the installation cards of board 1.</summary>
    public IReadOnlyList<WatchedInstallationViewModel> Installations
    {
        get => _installations;
        private set
        {
            if (SetProperty(ref _installations, value))
            {
                OnPropertyChanged(nameof(HasNoFolders));
            }
        }
    }

    /// <summary>Gets when RaidManager last accepted a snapshot, such as "Today, 14:05".</summary>
    public string LastSuccessText => _status.LastSuccess is { } at ? When(at) : "Not yet";

    /// <summary>Gets the number of snapshots waiting, such as "2 snapshots".</summary>
    public string QueuedText => _status.Queued == 1 ? "1 snapshot" : $"{_status.Queued} snapshots";

    /// <summary>Gets the accounts watched, such as "3 of 4 watched".</summary>
    public string AccountsText => $"{_status.WatchedAccountCount} of {_status.AccountCount} watched";

    /// <summary>Gets the rows of board 2's recent activity.</summary>
    public IReadOnlyList<ActivityRowViewModel> Activity =>
        [.. _status.Activity.Take(ActivityRowsShown).Select(row => new ActivityRowViewModel(
            row.Character.Name,
            row.Character.Realm,
            row.State,
            row.At is { } at ? $"Uploaded {When(at, lowerCase: true)}" : string.Empty))];

    /// <summary>Gets the problem boards 5 to 8 show, or <see langword="null"/> when nothing needs the player.</summary>
    public SyncProblem? Problem => SyncProblem.For(_status);

    /// <summary>Gets the footer, such as "Paired with Bryn · BRYN-DESKTOP".</summary>
    public string PairedText => $"Paired with {_pairing.PlayerName} · {_pairing.ComputerLabel}";
    #endregion Public Properties

    #region Public Methods
    /// <summary>Shows the board the status calls for, or the watched folders while none is watched (owner decision on #551).</summary>
    public void Open()
    {
        _showingFolders = _status.WatchedAccountCount == 0;
        RaiseStatusProperties();
    }

    /// <summary>Shows the watched folders (board 1), as "Choose folders" and "Watched folders" do.</summary>
    public void ShowFolders()
    {
        _showingFolders = true;
        RebuildInstallations(force: true);
        RaiseStatusProperties();
    }

    /// <summary>Stops listening to the sync and cancels the player's actions; a second call does nothing.</summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _sync.StatusChanged -= OnStatusChanged;
        _pairing.PropertyChanged -= OnPairingChanged;
        _lifetime.Cancel();
        _lifetime.Dispose();
    }
    #endregion Public Methods

    #region Private Helpers
    /// <summary>Writes that an action of the player failed.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="exception">The failure.</param>
    [LoggerMessage(Level = LogLevel.Warning, Message = "A sync action of the player failed.")]
    private static partial void LogActionFailed(ILogger logger, Exception exception);

    /// <summary>Saves the account choices and shows the status (board 2 or the board it calls for).</summary>
    /// <returns>A task that completes when the choices are saved.</returns>
    private async Task SaveAsync()
    {
        foreach (var account in _installations.SelectMany(installation => installation.Accounts).Where(account => account.IsWatched != account.WasWatched))
        {
            await _sync.SetAccountWatchedAsync(account.Id, account.IsWatched, _lifetime.Token);
        }

        IsFolderRejected = false;
        _showingFolders = false;
        Refresh();
    }

    /// <summary>Searches every fixed drive again.</summary>
    /// <returns>A task that completes when the search is done.</returns>
    private async Task FindFoldersAgainAsync()
    {
        IsFolderRejected = false;
        await _sync.FindFoldersAgainAsync(_lifetime.Token);
        Refresh();
    }

    /// <summary>Opens the folder picker and watches the folder chosen, or shows board 9 when it isn't a WoW installation.</summary>
    /// <returns>A task that completes when the folder is added or refused.</returns>
    private async Task AddFolderAsync()
    {
        if (await _picker.PickFolderAsync(_lifetime.Token) is not { } folder)
        {
            return;
        }

        IsFolderRejected = !await _sync.AddFolderAsync(folder, _lifetime.Token);
        Refresh();
    }

    /// <summary>Brings a status change to the window's thread.</summary>
    /// <param name="sender">The sync.</param>
    /// <param name="e">The event data.</param>
    private void OnStatusChanged(object? sender, EventArgs e) => _ui.Post(Refresh);

    /// <summary>Updates the footer when the pairing's player changes.</summary>
    /// <param name="sender">The pairing.</param>
    /// <param name="e">The event data.</param>
    private void OnPairingChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PairingViewModel.PlayerName))
        {
            OnPropertyChanged(nameof(PairedText));
        }
    }

    /// <summary>Reads the sync's status and updates what the boards show; on the window's thread.</summary>
    private void Refresh()
    {
        _status = _sync.Status;
        RebuildInstallations();
        RaiseStatusProperties();
    }

    /// <summary>Raises the change of every property that follows the status.</summary>
    private void RaiseStatusProperties()
    {
        foreach (var property in StatusProperties)
        {
            OnPropertyChanged(property);
        }
    }

    /// <summary>Builds the account rows again when the installations or the saved exclusions changed, so the player's
    /// unsaved choices survive the sync's regular checks.</summary>
    /// <param name="force">Whether to drop the unsaved choices anyway.</param>
    private void RebuildInstallations(bool force = false)
    {
        var key = string.Join(
            '\n',
            _status.Installations.Select(installation => installation.Folder + ":" + string.Join(',', installation.Accounts.Select(account => account.Id)))
                .Append(string.Join(',', _status.ExcludedAccounts.Order(StringComparer.Ordinal))));
        if (!force && key == _installationsKey)
        {
            return;
        }

        _installationsKey = key;
        Installations =
        [
            .. _status.Installations.Select(installation => new WatchedInstallationViewModel(
                installation.Folder,
                [.. installation.Accounts.Select(account => new WatchedAccountViewModel(
                    account.Id, account.Name, account.CharacterCount, !_status.ExcludedAccounts.Contains(account.Id)))])),
        ];
    }

    /// <summary>Writes a time as the boards do: "Today, 14:05", "Yesterday, 09:12" or "3 Oct, 14:05", in the player's time zone.</summary>
    /// <param name="at">The time.</param>
    /// <param name="lowerCase">Whether the text follows other words, as in "Uploaded today, 14:05".</param>
    /// <returns>The text.</returns>
    private string When(DateTimeOffset at, bool lowerCase = false)
    {
        var zone = _time.LocalTimeZone;
        var local = TimeZoneInfo.ConvertTime(at, zone);
        var today = TimeZoneInfo.ConvertTime(_time.GetUtcNow(), zone).Date;
        var day = local.Date == today ? "Today"
            : local.Date == today.AddDays(-1) ? "Yesterday"
            : local.ToString("d MMM", CultureInfo.InvariantCulture);
        if (lowerCase && local.Date >= today.AddDays(-1))
        {
            day = day.ToLowerInvariant();
        }

        return string.Create(CultureInfo.InvariantCulture, $"{day}, {local:HH:mm}");
    }

    /// <summary>Records that an action failed; the boards keep showing the sync's status.</summary>
    /// <param name="exception">The failure.</param>
    private void OnActionFailed(Exception exception) => LogActionFailed(_logger, exception);
    #endregion Private Helpers
}
