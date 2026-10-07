namespace RaidManager.Companion.Client.Features.Sync.Queue;

/// <summary>Locates the files the sync keeps in the user's profile.</summary>
/// <param name="Folder">The folder, <c>%LOCALAPPDATA%\RaidManager</c> for players.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-07<br/>
/// Purpose: Keeps the upload queue and the sync settings next to <c>companion.dat</c>, never under the WoW folder
/// (ADR-0030, #550); tests point it at a temporary folder.
/// </remarks>
public sealed record SyncFileLocation(string Folder)
{
    #region Public Properties
    /// <summary>Gets the path of the upload queue.</summary>
    public string QueuePath => Path.Combine(Folder, "sync-queue.json");

    /// <summary>Gets the path of the sync settings.</summary>
    public string SettingsPath => Path.Combine(Folder, "sync-settings.json");
    #endregion Public Properties

    #region Factory Methods
    /// <summary>Gets the location in the current user's local application data.</summary>
    /// <returns>The location.</returns>
    public static SyncFileLocation ForCurrentUser() => new(Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RaidManager"));
    #endregion Factory Methods
}
