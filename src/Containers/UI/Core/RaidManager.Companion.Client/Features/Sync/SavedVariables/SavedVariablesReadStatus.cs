namespace RaidManager.Companion.Client.Features.Sync.SavedVariables;

/// <summary>Defines what reading <c>RaidManager.lua</c> found.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-07<br/>
/// Purpose: Lets the sync tell a complete file from one WoW is still writing, one cut by a crash, one it can't read and
/// one written by an addon version it doesn't support (#550, boards 2 and 5 of <c>companion-sync</c>).
/// </remarks>
internal enum SavedVariablesReadStatus
{
    /// <summary>The file is complete; every character it holds was read.</summary>
    Complete,

    /// <summary>The file ends early; the characters before the cut were read.</summary>
    Incomplete,

    /// <summary>The file isn't SavedVariables Lua, or holds no RaidManager data.</summary>
    Unreadable,

    /// <summary>The file was written for another schema version than 1.</summary>
    UnsupportedSchema,
}
