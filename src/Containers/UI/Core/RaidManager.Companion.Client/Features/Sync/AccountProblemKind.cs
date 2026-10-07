namespace RaidManager.Companion.Client.Features.Sync;

/// <summary>Defines why an account's file needs the player's help.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-07<br/>
/// Purpose: The actionable failures of #17's third criterion that come from the addon's file (#550).
/// </remarks>
public enum AccountProblemKind
{
    /// <summary>WoW didn't finish writing the file, perhaps after a crash: log in and <c>/reload</c> (board 5).</summary>
    IncompleteSnapshot,

    /// <summary>The file isn't one the addon writes.</summary>
    UnreadableFile,

    /// <summary>The addon that wrote the file uses another schema version: update the addon or the companion.</summary>
    UnsupportedSchema,
}
