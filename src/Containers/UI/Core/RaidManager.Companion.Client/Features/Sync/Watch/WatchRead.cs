using RaidManager.Companion.Client.Features.Sync.SavedVariables;

namespace RaidManager.Companion.Client.Features.Sync.Watch;

/// <summary>Represents one read of a watched <c>RaidManager.lua</c>.</summary>
/// <param name="File">What was read.</param>
/// <param name="Final">Whether this version of the file won't be read again: complete, or cut for too long.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-07<br/>
/// Purpose: Lets the sync queue the complete characters at once, and show a cut one as waiting for WoW until the read
/// is final, then as incomplete (#550).
/// </remarks>
internal sealed record WatchRead(SavedVariablesFile File, bool Final);
