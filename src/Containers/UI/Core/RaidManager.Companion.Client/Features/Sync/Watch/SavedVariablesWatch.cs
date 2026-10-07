using System.Text;
using RaidManager.Companion.Client.Features.Sync.Discovery;
using RaidManager.Companion.Client.Features.Sync.SavedVariables;

namespace RaidManager.Companion.Client.Features.Sync.Watch;

/// <summary>Watches one account's <c>RaidManager.lua</c> and reads it once WoW has finished writing it.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-07<br/>
/// Purpose: The complete-write detection of #550. WoW writes the file on <c>/reload</c>, logout or exit. The sync checks
/// the file's size and time at every scan; a change is read only at a later scan that finds them unchanged and the file
/// free to open, and is read again if they changed during the read. A file that still ends early is retried at each
/// scan while its characters show as waiting for WoW, and reported incomplete once it has been unchanged for 30 seconds
/// (board 5 of <c>companion-sync</c>). A file is read once per change; <see cref="ReadAgain"/> reads it again.
/// </remarks>
internal sealed class SavedVariablesWatch
{
    #region Fields
    /// <summary>Stores how long a cut file stays unchanged before it is reported incomplete.</summary>
    public static readonly TimeSpan IncompleteAfter = TimeSpan.FromSeconds(30);

    /// <summary>Stores the size and time last seen.</summary>
    private FileStamp? _seen;

    /// <summary>Stores the size and time of the last version read for good.</summary>
    private FileStamp? _read;

    /// <summary>Stores when the size and time last changed.</summary>
    private DateTimeOffset _unchangedSince;
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="SavedVariablesWatch"/> class.</summary>
    /// <param name="account">The account whose file is watched.</param>
    public SavedVariablesWatch(WowAccount account)
    {
        Account = account;
    }
    #endregion Constructors

    #region Public Properties
    /// <summary>Gets the account whose file is watched.</summary>
    public WowAccount Account { get; }

    /// <summary>Gets a value indicating whether WoW is writing the file, as far as the last scan could tell.</summary>
    public bool IsWriting { get; private set; }
    #endregion Public Properties

    #region Public Methods
    /// <summary>Checks the file, and reads it when a change is complete.</summary>
    /// <param name="now">The time of the scan.</param>
    /// <returns>The file read, or <see langword="null"/> when there is nothing new to read yet.</returns>
    public WatchRead? Check(DateTimeOffset now)
    {
        var stamp = FileStamp.Of(Account.SavedVariablesPath);
        if (stamp is null || stamp == _read)
        {
            IsWriting = false;
            return null;
        }

        if (stamp != _seen)
        {
            _seen = stamp;
            _unchangedSince = now;
            IsWriting = true;
            return null;
        }

        var text = TryRead(Account.SavedVariablesPath);
        if (text is null || FileStamp.Of(Account.SavedVariablesPath) != stamp)
        {
            // WoW still holds the file, or wrote it again during the read: try at the next scan.
            IsWriting = true;
            return null;
        }

        IsWriting = false;
        var file = SavedVariablesReader.Read(text);
        var final = file.Status != SavedVariablesReadStatus.Incomplete || now - _unchangedSince >= IncompleteAfter;
        if (final)
        {
            _read = stamp;
        }

        return new WatchRead(file, final);
    }

    /// <summary>Reads the file again at the next scan, as the "Retry" of board 5 asks.</summary>
    public void ReadAgain()
    {
        _read = null;
        _seen = null;
    }
    #endregion Public Methods

    #region Private Helpers
    /// <summary>Reads the file if WoW doesn't hold it open for writing.</summary>
    /// <param name="path">The file's path.</param>
    /// <returns>The text, or <see langword="null"/> when the file can't be opened now.</returns>
    private static string? TryRead(string path)
    {
        try
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var reader = new StreamReader(stream, Encoding.UTF8);
            return reader.ReadToEnd();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }
    #endregion Private Helpers

    #region Nested Types
    /// <summary>Represents a file's size and last write time.</summary>
    /// <param name="Length">The size in bytes.</param>
    /// <param name="LastWriteUtc">The last write time.</param>
    private sealed record FileStamp(long Length, DateTime LastWriteUtc)
    {
        /// <summary>Reads a file's size and time.</summary>
        /// <param name="path">The file's path.</param>
        /// <returns>The stamp, or <see langword="null"/> when the file doesn't exist.</returns>
        public static FileStamp? Of(string path)
        {
            var info = new FileInfo(path);
            return info.Exists ? new FileStamp(info.Length, info.LastWriteTimeUtc) : null;
        }
    }
    #endregion Nested Types
}
