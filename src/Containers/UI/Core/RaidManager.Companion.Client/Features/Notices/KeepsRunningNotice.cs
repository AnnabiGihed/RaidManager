namespace RaidManager.Companion.Client.Features.Notices;

/// <summary>Remembers whether the player has seen that the companion keeps running when its window closes.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: The first-close rule of #528 and board 21: the notice shows the first time the window is closed, and never
/// again, across restarts. A marker file records it; if the file can't be written, the notice may show once more,
/// which is harmless.
/// </remarks>
public sealed class KeepsRunningNotice
{
    #region Fields
    /// <summary>Stores the marker file's path.</summary>
    private readonly string _markerPath;
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="KeepsRunningNotice"/> class.</summary>
    /// <param name="location">Where the marker file is.</param>
    public KeepsRunningNotice(NoticeMarkerLocation location)
    {
        ArgumentNullException.ThrowIfNull(location);
        _markerPath = location.FilePath;
    }
    #endregion Constructors

    #region Public Properties
    /// <summary>Gets a value indicating whether the notice hasn't been shown yet, so this close is the first.</summary>
    public bool IsFirstClose => !File.Exists(_markerPath);
    #endregion Public Properties

    #region Public Methods
    /// <summary>Records that the notice was shown.</summary>
    public void RecordShown()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_markerPath)!);
            File.WriteAllText(_markerPath, string.Empty);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // Without the marker the notice shows again at the next first close; nothing else depends on it.
        }
    }
    #endregion Public Methods
}
