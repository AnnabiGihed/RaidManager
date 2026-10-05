namespace RaidManager.ViewModels.Features.Characters;

/// <summary>Names a character's primary loadout in the My characters list.</summary>
/// <param name="Name">The loadout name.</param>
/// <param name="Role">The role name, for example <c>MeleeDamage</c>.</param>
/// <param name="GearScore">The calculated GearScore, or <see langword="null"/> before an item catalog gives one (#552).</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-05<br/>
/// Purpose: Carries the primary loadout column of board 1.
/// </remarks>
public sealed record CharacterLoadoutSummary(string Name, string Role, int? GearScore);
