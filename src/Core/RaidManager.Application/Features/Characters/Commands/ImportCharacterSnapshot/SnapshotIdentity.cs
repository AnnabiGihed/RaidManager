namespace RaidManager.Application.Features.Characters.Commands.ImportCharacterSnapshot;

/// <summary>Represents the identity section of a snapshot.</summary>
/// <param name="Status"><c>observed</c> when the game answered, <c>unavailable</c> when it didn't.</param>
/// <param name="ObservedAt">The player's computer time of the answer, in seconds since 1970 (UTC), when observed.</param>
/// <param name="Level">The character level.</param>
/// <param name="Class">The class token, such as <c>DEATHKNIGHT</c>.</param>
/// <param name="Race">The race token, such as <c>Scourge</c>.</param>
/// <param name="Faction"><c>Alliance</c> or <c>Horde</c>.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-05<br/>
/// Purpose: Mirrors the identity section of the addon contract (#384).
/// </remarks>
public sealed record SnapshotIdentity(string? Status, long? ObservedAt, int? Level, string? Class, string? Race, string? Faction);
