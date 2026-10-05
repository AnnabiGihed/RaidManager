namespace RaidManager.Application.Features.Characters.Commands.ImportCharacterSnapshot;

/// <summary>Represents the talents section of a snapshot.</summary>
/// <param name="Status"><c>observed</c> when the game answered, <c>unavailable</c> when it didn't.</param>
/// <param name="ObservedAt">The player's computer time of the answer, in seconds since 1970 (UTC), when observed.</param>
/// <param name="ActiveGroup">The active talent group, 1 or 2.</param>
/// <param name="Groups">The talent groups.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-05<br/>
/// Purpose: Mirrors the talents section of the addon contract (#384).
/// </remarks>
public sealed record SnapshotTalents(string? Status, long? ObservedAt, int? ActiveGroup, IReadOnlyList<SnapshotTalentGroup>? Groups);
