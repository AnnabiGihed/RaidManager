namespace RaidManager.Companion.Client.Features.Sync;

/// <summary>Defines what the sync is doing with a character, as board 2's recent activity shows it.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-07<br/>
/// Purpose: The three row states of board 2 of <c>companion-sync</c> (#550, #551).
/// </remarks>
public enum CharacterActivityState
{
    /// <summary>Its snapshot is being sent to RaidManager.</summary>
    Uploading,

    /// <summary>WoW hasn't finished writing its snapshot yet.</summary>
    WaitingForWow,

    /// <summary>RaidManager accepted its latest snapshot.</summary>
    Uploaded,
}
