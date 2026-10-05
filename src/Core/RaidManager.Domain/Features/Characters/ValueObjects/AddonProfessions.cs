namespace RaidManager.Domain.Features.Characters.ValueObjects;

/// <summary>Represents a complete read of the character's skill list.</summary>
/// <param name="Items">Every profession and secondary skill read, in the game's order; empty when the character has none.</param>
/// <param name="ObservedAtUtc">The UTC instant the skill list was read.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-05<br/>
/// Purpose: Holds the professions section of an addon snapshot with its own observation time (#384).
/// </remarks>
public sealed record AddonProfessions(IReadOnlyList<Profession> Items, DateTimeOffset ObservedAtUtc);
