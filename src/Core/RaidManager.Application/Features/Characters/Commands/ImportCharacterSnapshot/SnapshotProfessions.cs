namespace RaidManager.Application.Features.Characters.Commands.ImportCharacterSnapshot;

/// <summary>Represents the professions section of a snapshot.</summary>
/// <param name="Status"><c>observed</c> when the game answered, <c>unavailable</c> when it didn't.</param>
/// <param name="ObservedAt">The player's computer time of the answer, in seconds since 1970 (UTC), when observed.</param>
/// <param name="Items">The skill lines, in the game's order.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-05<br/>
/// Purpose: Mirrors the professions section of the addon contract (#384).
/// </remarks>
public sealed record SnapshotProfessions(string? Status, long? ObservedAt, IReadOnlyList<SnapshotProfession>? Items);
