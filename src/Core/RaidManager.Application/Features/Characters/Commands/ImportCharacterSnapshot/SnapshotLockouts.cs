namespace RaidManager.Application.Features.Characters.Commands.ImportCharacterSnapshot;

/// <summary>Represents the lockouts section of a snapshot.</summary>
/// <param name="Status"><c>observed</c> when the game answered, <c>unavailable</c> when it didn't.</param>
/// <param name="ObservedAt">The player's computer time of the answer, in seconds since 1970 (UTC), when observed.</param>
/// <param name="Complete">Whether every saved instance the game counted was read.</param>
/// <param name="Items">The saved instances, raids and dungeons.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-05<br/>
/// Purpose: Mirrors the lockouts section of the addon contract (#384).
/// </remarks>
public sealed record SnapshotLockouts(string? Status, long? ObservedAt, bool? Complete, IReadOnlyList<SnapshotLockout>? Items);
