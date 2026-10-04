namespace RaidManager.Companion.Client.Features.Tokens;

/// <summary>Names the file the token store writes.</summary>
/// <param name="FilePath">The file's full path.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: Puts the token in the user's local application data (ADR-0030), and lets tests use a temporary folder.
/// </remarks>
public sealed record TokenFileLocation(string FilePath)
{
    #region Factory Methods
    /// <summary>Gets the companion's file in the current user's local application data.</summary>
    /// <returns><c>%LOCALAPPDATA%\RaidManager\companion.dat</c> on Windows.</returns>
    public static TokenFileLocation ForCurrentUser() => new(Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RaidManager", "companion.dat"));
    #endregion Factory Methods
}
