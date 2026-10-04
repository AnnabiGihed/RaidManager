namespace RaidManager.Companion.Client.Features.Notices;

/// <summary>Names the file that records that the keeps-running notice was shown.</summary>
/// <param name="FilePath">The file's full path.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: Keeps the marker next to the token file in the user's local application data, and lets tests use a
/// temporary folder.
/// </remarks>
public sealed record NoticeMarkerLocation(string FilePath)
{
    #region Factory Methods
    /// <summary>Gets the marker in the current user's local application data.</summary>
    /// <returns><c>%LOCALAPPDATA%\RaidManager\keeps-running-notice.shown</c> on Windows.</returns>
    public static NoticeMarkerLocation ForCurrentUser() => new(Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RaidManager", "keeps-running-notice.shown"));
    #endregion Factory Methods
}
