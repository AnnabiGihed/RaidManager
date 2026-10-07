namespace RaidManager.Companion.Client.Features.Sync.Discovery;

/// <summary>Represents one account folder of a World of Warcraft installation.</summary>
/// <param name="Id">The account's hash, which the companion's files keep instead of its name.</param>
/// <param name="Name">The account folder's name, the player's WoW login, shown only on screen.</param>
/// <param name="Folder">The account folder's full path.</param>
/// <param name="CharacterCount">The number of character folders WoW keeps for it.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-07<br/>
/// Purpose: An account row of board 1 of <c>companion-sync</c>, and the folder whose
/// <c>SavedVariables\RaidManager.lua</c> the sync reads (#550).
/// </remarks>
public sealed record WowAccount(string Id, string Name, string Folder, int CharacterCount)
{
    #region Public Properties
    /// <summary>Gets the path of the addon's file for this account.</summary>
    public string SavedVariablesPath => Path.Combine(Folder, "SavedVariables", "RaidManager.lua");
    #endregion Public Properties
}
