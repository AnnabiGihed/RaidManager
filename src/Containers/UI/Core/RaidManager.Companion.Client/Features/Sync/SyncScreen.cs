namespace RaidManager.Companion.Client.Features.Sync;

/// <summary>Lists the sync window's screens, by their board in the <c>companion-sync</c> mockup.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-08<br/>
/// Purpose: Gives the sync view one value to switch on (#551); board 9 is <see cref="WatchedFolders"/> with its
/// warning notice.
/// </remarks>
public enum SyncScreen
{
    /// <summary>Boards 1 and 9: the watched folders and their accounts.</summary>
    WatchedFolders,

    /// <summary>Board 2: snapshots upload as WoW writes them.</summary>
    Syncing,

    /// <summary>Board 3: the player paused uploads.</summary>
    Paused,

    /// <summary>Board 4: uploads can't reach RaidManager and wait in the queue.</summary>
    Offline,

    /// <summary>Board 5: WoW didn't finish writing a snapshot.</summary>
    IncompleteSnapshot,

    /// <summary>Board 6: RaidManager refused a snapshot.</summary>
    RefusedSnapshot,

    /// <summary>Board 7: an addon version the companion doesn't read wrote an account's file.</summary>
    UnsupportedAddon,

    /// <summary>Board 8: an account's file isn't the addon's.</summary>
    UnreadableFile,
}
