namespace RaidManager.Companion.Client.Features.Sync.Discovery;

/// <summary>Lists the folders the search for World of Warcraft starts from.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-07<br/>
/// Purpose: The fixed drives on the player's computer; tests give temporary folders instead (#550).
/// </remarks>
public interface IDriveRoots
{
    #region Public Methods
    /// <summary>Gets the root folder of every fixed drive that is ready.</summary>
    /// <returns>The roots, such as <c>C:\</c> and <c>D:\</c>.</returns>
    IReadOnlyList<string> GetFixedDriveRoots();
    #endregion Public Methods
}
