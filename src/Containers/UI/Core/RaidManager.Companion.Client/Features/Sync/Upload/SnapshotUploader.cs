using Microsoft.Extensions.Logging;
using RaidManager.Companion.Client.Features.Sync.Queue;
using RaidManager.Companion.Client.Features.Sync.SavedVariables;
using RaidManager.Companion.Client.Features.Tokens;

namespace RaidManager.Companion.Client.Features.Sync.Upload;

/// <summary>Uploads the queued snapshots and decides when to try again.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-07<br/>
/// Purpose: The upload rules of #550 and its owner decisions. A snapshot leaves the queue when RaidManager answers 202
/// or 200, and when it answers 400 (shown as a refusal). A network failure, a timeout or a server error keeps it and
/// waits 5 seconds, doubling to at most 5 minutes, each wait up to a fifth longer at random so many companions don't
/// retry together. A 429 waits as long as <c>Retry-After</c> says (a minute when it doesn't). A 401 stops uploads
/// until the stored token changes, that is until the companion pairs again.
/// </remarks>
internal sealed partial class SnapshotUploader
{
    #region Constants
    /// <summary>Defines how many recent uploads the activity list keeps.</summary>
    private const int RecentUploadsKept = 10;
    #endregion Constants

    #region Fields
    /// <summary>Stores the first wait after a failure.</summary>
    private static readonly TimeSpan FirstRetry = TimeSpan.FromSeconds(5);

    /// <summary>Stores the longest wait between two attempts.</summary>
    private static readonly TimeSpan LongestRetry = TimeSpan.FromMinutes(5);

    /// <summary>Stores the wait after a 429 that doesn't say how long.</summary>
    private static readonly TimeSpan DefaultSlowDown = TimeSpan.FromMinutes(1);

    /// <summary>Stores the lock that guards the state the sync screens read.</summary>
    private readonly Lock _gate = new();

    /// <summary>Stores the queue.</summary>
    private readonly SnapshotQueue _queue;

    /// <summary>Stores the API client.</summary>
    private readonly ISnapshotApi _api;

    /// <summary>Stores the token store.</summary>
    private readonly ITokenStore _tokens;

    /// <summary>Stores the clock.</summary>
    private readonly TimeProvider _time;

    /// <summary>Stores the source of the random part of each wait.</summary>
    private readonly Random _random;

    /// <summary>Stores the logger.</summary>
    private readonly ILogger<SnapshotUploader> _logger;

    /// <summary>Stores the latest refusal of each character.</summary>
    private readonly Dictionary<CharacterKey, RefusedSnapshot> _refusals = [];

    /// <summary>Stores the latest accepted uploads, newest first.</summary>
    private readonly List<UploadedCharacter> _recentUploads = [];

    /// <summary>Stores the device token RaidManager refused, so uploads wait for a new one.</summary>
    private string? _refusedToken;

    /// <summary>Stores the number of failed attempts in a row.</summary>
    private int _failures;

    /// <summary>Stores the time before which no upload is attempted.</summary>
    private DateTimeOffset _notBefore = DateTimeOffset.MinValue;

    /// <summary>Stores whether the uploads reach RaidManager.</summary>
    private UploadConnection _connection = UploadConnection.Unknown;

    /// <summary>Stores when a snapshot was last accepted.</summary>
    private DateTimeOffset? _lastSuccess;

    /// <summary>Stores the character being uploaded.</summary>
    private CharacterKey? _uploading;
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="SnapshotUploader"/> class.</summary>
    /// <param name="queue">The queue.</param>
    /// <param name="api">The API client.</param>
    /// <param name="tokens">The token store.</param>
    /// <param name="time">The clock.</param>
    /// <param name="logger">The logger.</param>
    public SnapshotUploader(SnapshotQueue queue, ISnapshotApi api, ITokenStore tokens, TimeProvider time, ILogger<SnapshotUploader> logger)
        : this(queue, api, tokens, time, logger, Random.Shared)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="SnapshotUploader"/> class with a given random source.</summary>
    /// <param name="queue">The queue.</param>
    /// <param name="api">The API client.</param>
    /// <param name="tokens">The token store.</param>
    /// <param name="time">The clock.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="random">The source of the random part of each wait.</param>
    internal SnapshotUploader(SnapshotQueue queue, ISnapshotApi api, ITokenStore tokens, TimeProvider time, ILogger<SnapshotUploader> logger, Random random)
    {
        _queue = queue;
        _api = api;
        _tokens = tokens;
        _time = time;
        _logger = logger;
        _random = random;
    }
    #endregion Constructors

    #region Events
    /// <summary>Occurs when the connection, the last success, the refusals or the character being uploaded change.</summary>
    public event EventHandler? Changed;
    #endregion Events

    #region Public Properties
    /// <summary>Gets whether the uploads reach RaidManager.</summary>
    public UploadConnection Connection
    {
        get
        {
            lock (_gate)
            {
                return _connection;
            }
        }
    }

    /// <summary>Gets when a snapshot was last accepted since the companion started.</summary>
    public DateTimeOffset? LastSuccess
    {
        get
        {
            lock (_gate)
            {
                return _lastSuccess;
            }
        }
    }

    /// <summary>Gets the character being uploaded, if any.</summary>
    public CharacterKey? Uploading
    {
        get
        {
            lock (_gate)
            {
                return _uploading;
            }
        }
    }

    /// <summary>Gets the snapshots RaidManager refused, the latest of each character.</summary>
    public IReadOnlyList<RefusedSnapshot> Refusals
    {
        get
        {
            lock (_gate)
            {
                return [.. _refusals.Values];
            }
        }
    }

    /// <summary>Gets the latest accepted uploads, newest first, at most ten.</summary>
    public IReadOnlyList<UploadedCharacter> RecentUploads
    {
        get
        {
            lock (_gate)
            {
                return [.. _recentUploads];
            }
        }
    }

    /// <summary>Gets the time before which no upload is attempted.</summary>
    public DateTimeOffset NotBefore
    {
        get
        {
            lock (_gate)
            {
                return _notBefore;
            }
        }
    }
    #endregion Public Properties

    #region Public Methods
    /// <summary>Uploads the queued snapshots, oldest first, until the queue is empty or an upload must wait.</summary>
    /// <param name="cancellationToken">A token to stop.</param>
    /// <returns>A task that completes when nothing more can be uploaded now.</returns>
    public async Task UploadDueAsync(CancellationToken cancellationToken)
    {
        while (_queue.Peek() is { } snapshot && _time.GetUtcNow() >= NotBefore)
        {
            var token = await _tokens.LoadAsync(cancellationToken);
            if (!CanUploadWith(token?.DeviceToken))
            {
                return;
            }

            SetUploading(snapshot.Key);
            var result = await _api.UploadAsync(token!.DeviceToken, snapshot, cancellationToken);
            if (!Apply(snapshot, result, token.DeviceToken))
            {
                return;
            }
        }
    }

    /// <summary>Lets the next upload start at once, whatever the wait (the "Retry now" of board 4).</summary>
    public void RetryNow()
    {
        lock (_gate)
        {
            _notBefore = DateTimeOffset.MinValue;
        }
    }

    /// <summary>Forgets the refused snapshots, as the retry of board 6 does; a new refusal shows again.</summary>
    public void ForgetRefusals()
    {
        lock (_gate)
        {
            _refusals.Clear();
        }

        OnChanged();
    }
    #endregion Public Methods

    #region Private Helpers
    /// <summary>Writes that RaidManager refused a snapshot.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="errorCode">The problem's error code, if any.</param>
    [LoggerMessage(Level = LogLevel.Warning, Message = "RaidManager refused a character snapshot with {ErrorCode}; it was dropped.")]
    private static partial void LogRefused(ILogger logger, string? errorCode);

    /// <summary>Writes that RaidManager refused the device token.</summary>
    /// <param name="logger">The logger.</param>
    [LoggerMessage(Level = LogLevel.Warning, Message = "RaidManager refused the device token; uploads wait until the companion pairs again.")]
    private static partial void LogTokenRefused(ILogger logger);

    /// <summary>Checks that a device token exists and wasn't refused, and records why not.</summary>
    /// <param name="deviceToken">The stored device token, if any.</param>
    /// <returns><see langword="true"/> when uploads can use it.</returns>
    private bool CanUploadWith(string? deviceToken)
    {
        var connection = deviceToken is null ? UploadConnection.NotPaired
            : deviceToken == _refusedToken ? UploadConnection.NeedsPairing
            : (UploadConnection?)null;
        if (connection is null)
        {
            return true;
        }

        Update(() => _connection = connection.Value);
        return false;
    }

    /// <summary>Applies RaidManager's answer to the queue and the state.</summary>
    /// <param name="snapshot">The uploaded snapshot.</param>
    /// <param name="result">The answer.</param>
    /// <param name="deviceToken">The device token used.</param>
    /// <returns><see langword="true"/> when the next snapshot can be uploaded at once.</returns>
    private bool Apply(QueuedSnapshot snapshot, SnapshotUploadResult result, string deviceToken)
    {
        var now = _time.GetUtcNow();
        switch (result.Outcome)
        {
            case SnapshotUploadOutcome.Accepted:
                _queue.Accept(snapshot);
                Update(() =>
                {
                    _failures = 0;
                    _connection = UploadConnection.Online;
                    _lastSuccess = now;
                    _refusals.Remove(snapshot.Key);
                    _recentUploads.RemoveAll(uploaded => uploaded.Character == snapshot.Key);
                    _recentUploads.Insert(0, new UploadedCharacter(snapshot.Key, now));
                    if (_recentUploads.Count > RecentUploadsKept)
                    {
                        _recentUploads.RemoveAt(RecentUploadsKept);
                    }
                });
                return true;
            case SnapshotUploadOutcome.Refused:
                _queue.Drop(snapshot);
                LogRefused(_logger, result.ErrorCode);
                Update(() =>
                {
                    _failures = 0;
                    _connection = UploadConnection.Online;
                    _refusals[snapshot.Key] = new RefusedSnapshot(snapshot.Key, result.ErrorCode, now);
                });
                return true;
            case SnapshotUploadOutcome.Unauthorized:
                LogTokenRefused(_logger);
                Update(() =>
                {
                    _refusedToken = deviceToken;
                    _connection = UploadConnection.NeedsPairing;
                });
                return false;
            case SnapshotUploadOutcome.SlowDown:
                Update(() => _notBefore = now + (result.RetryAfter ?? DefaultSlowDown));
                return false;
            default:
                Update(() =>
                {
                    _failures++;
                    _connection = UploadConnection.Offline;
                    _notBefore = now + RetryWait(_failures);
                });
                return false;
        }
    }

    /// <summary>Computes the wait after a number of failures in a row.</summary>
    /// <param name="failures">The failures in a row, from 1.</param>
    /// <returns>5 seconds doubled per earlier failure, at most 5 minutes, plus up to a fifth at random.</returns>
    private TimeSpan RetryWait(int failures)
    {
        var doublings = Math.Min(failures - 1, 6);
        var wait = TimeSpan.FromTicks(Math.Min(FirstRetry.Ticks << doublings, LongestRetry.Ticks));
        return wait + (wait * (_random.NextDouble() / 5));
    }

    /// <summary>Records the character being uploaded.</summary>
    /// <param name="character">The character.</param>
    private void SetUploading(CharacterKey character)
    {
        lock (_gate)
        {
            _uploading = character;
        }

        OnChanged();
    }

    /// <summary>Changes the state under the lock, clears the character being uploaded, and raises <see cref="Changed"/>.</summary>
    /// <param name="change">The change.</param>
    private void Update(Action change)
    {
        lock (_gate)
        {
            _uploading = null;
            change();
        }

        OnChanged();
    }

    /// <summary>Raises <see cref="Changed"/>.</summary>
    private void OnChanged() => Changed?.Invoke(this, EventArgs.Empty);
    #endregion Private Helpers
}
