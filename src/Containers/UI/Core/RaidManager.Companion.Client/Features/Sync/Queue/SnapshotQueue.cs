using System.Text.Json;
using Microsoft.Extensions.Logging;
using RaidManager.Companion.Client.Features.Sync.SavedVariables;

namespace RaidManager.Companion.Client.Features.Sync.Queue;

/// <summary>Keeps the snapshots waiting to upload in a file of the user's profile.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-07<br/>
/// Purpose: The queue of #550: nothing read from <c>RaidManager.lua</c> is lost while RaidManager can't be reached or
/// the companion stops. A character has at most one queued snapshot, the newest (owner decision on #550), and a snapshot
/// as old as the latest accepted one isn't queued again. Every change is written through a temporary file and a replace,
/// so a crash never leaves half a file; a damaged file counts as an empty queue.
/// </remarks>
internal sealed partial class SnapshotQueue
{
    #region Constants
    /// <summary>Defines the suffix of the temporary file written before the replace.</summary>
    private const string TemporarySuffix = ".tmp";
    #endregion Constants

    #region Fields
    /// <summary>Stores the lock that serializes changes from the sync loop and the player's actions.</summary>
    private readonly Lock _gate = new();

    /// <summary>Stores the queue file's path.</summary>
    private readonly string _filePath;

    /// <summary>Stores the snapshots waiting, oldest first.</summary>
    private readonly List<QueuedSnapshot> _queued;

    /// <summary>Stores the latest accepted snapshot of each character, with when it was accepted.</summary>
    private readonly Dictionary<CharacterKey, UploadedSnapshot> _uploaded;
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="SnapshotQueue"/> class and reads its file.</summary>
    /// <param name="location">Where the sync's files are.</param>
    /// <param name="logger">The logger.</param>
    public SnapshotQueue(SyncFileLocation location, ILogger<SnapshotQueue> logger)
    {
        ArgumentNullException.ThrowIfNull(location);
        _filePath = location.QueuePath;
        var stored = Load(_filePath, logger);
        _queued = [.. stored.Queued];
        _uploaded = stored.Uploaded.ToDictionary(uploaded => uploaded.Character);
    }
    #endregion Constructors

    #region Public Properties
    /// <summary>Gets the number of snapshots waiting.</summary>
    public int Count
    {
        get
        {
            lock (_gate)
            {
                return _queued.Count;
            }
        }
    }

    /// <summary>Gets the latest accepted snapshot of each character, with when it was accepted when known.</summary>
    public IReadOnlyList<UploadedSnapshot> Uploaded
    {
        get
        {
            lock (_gate)
            {
                return [.. _uploaded.Values];
            }
        }
    }

    /// <summary>Gets the snapshots waiting, oldest first.</summary>
    public IReadOnlyList<QueuedSnapshot> Waiting
    {
        get
        {
            lock (_gate)
            {
                return [.. _queued];
            }
        }
    }
    #endregion Public Properties

    #region Public Methods
    /// <summary>Queues a snapshot unless the same or a newer one of its character is queued or was accepted.</summary>
    /// <param name="snapshot">The snapshot.</param>
    /// <returns><see langword="true"/> when it was queued, replacing an older one of its character.</returns>
    public bool Offer(QueuedSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        lock (_gate)
        {
            var key = snapshot.Key;
            if (_uploaded.TryGetValue(key, out var accepted) && accepted.CapturedAt >= snapshot.CapturedAt)
            {
                return false;
            }

            var index = _queued.FindIndex(queued => queued.Key == key);
            if (index >= 0 && _queued[index].CapturedAt >= snapshot.CapturedAt)
            {
                return false;
            }

            if (index >= 0)
            {
                _queued.RemoveAt(index);
            }

            _queued.Add(snapshot);
            Save();
            return true;
        }
    }

    /// <summary>Gets the oldest waiting snapshot.</summary>
    /// <returns>The snapshot, or <see langword="null"/> when the queue is empty.</returns>
    public QueuedSnapshot? Peek()
    {
        lock (_gate)
        {
            return _queued.Count == 0 ? null : _queued[0];
        }
    }

    /// <summary>Removes a snapshot RaidManager accepted, and remembers it as its character's latest.</summary>
    /// <param name="snapshot">The snapshot.</param>
    /// <param name="uploadedAt">When RaidManager accepted it.</param>
    public void Accept(QueuedSnapshot snapshot, DateTimeOffset uploadedAt)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        lock (_gate)
        {
            _queued.Remove(snapshot);
            if (!_uploaded.TryGetValue(snapshot.Key, out var accepted) || accepted.CapturedAt < snapshot.CapturedAt)
            {
                _uploaded[snapshot.Key] = new UploadedSnapshot(snapshot.Realm, snapshot.Name, snapshot.CapturedAt, uploadedAt);
            }

            Save();
        }
    }

    /// <summary>Removes a snapshot without uploading it.</summary>
    /// <param name="snapshot">The snapshot.</param>
    public void Drop(QueuedSnapshot snapshot)
    {
        lock (_gate)
        {
            if (_queued.Remove(snapshot))
            {
                Save();
            }
        }
    }

    /// <summary>Removes every snapshot of an account (owner decision on #550: excluding an account drops them).</summary>
    /// <param name="accountId">The hashed account folder.</param>
    /// <returns>The number of snapshots removed.</returns>
    public int DropAccount(string accountId)
    {
        lock (_gate)
        {
            var removed = _queued.RemoveAll(queued => queued.AccountId == accountId);
            if (removed > 0)
            {
                Save();
            }

            return removed;
        }
    }
    #endregion Public Methods

    #region Private Helpers
    /// <summary>Writes that the queue file couldn't be read.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="exception">The failure.</param>
    [LoggerMessage(Level = LogLevel.Warning, Message = "The upload queue file is damaged; the companion starts with an empty queue.")]
    private static partial void LogDamagedFile(ILogger logger, Exception exception);

    /// <summary>Reads the queue file.</summary>
    /// <param name="filePath">The file's path.</param>
    /// <param name="logger">The logger.</param>
    /// <returns>The stored queue, or an empty one when there is no readable file.</returns>
    private static SnapshotQueueFile Load(string filePath, ILogger logger)
    {
        var empty = new SnapshotQueueFile([], []);
        if (!File.Exists(filePath))
        {
            return empty;
        }

        try
        {
            return JsonSerializer.Deserialize(File.ReadAllBytes(filePath), SyncJsonContext.Default.SnapshotQueueFile) ?? empty;
        }
        catch (JsonException exception)
        {
            LogDamagedFile(logger, exception);
            return empty;
        }
    }

    /// <summary>Writes the queue file through a temporary file and a replace; the caller holds the lock.</summary>
    private void Save()
    {
        var stored = new SnapshotQueueFile(
            [.. _queued],
            [.. _uploaded.Values]);
        Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
        var temporaryPath = _filePath + TemporarySuffix;
        File.WriteAllBytes(temporaryPath, JsonSerializer.SerializeToUtf8Bytes(stored, SyncJsonContext.Default.SnapshotQueueFile));
        File.Move(temporaryPath, _filePath, overwrite: true);
    }
    #endregion Private Helpers
}
