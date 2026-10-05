namespace RaidManager.ApiService.Features.Characters;

/// <summary>Names a character's primary loadout in the My characters list.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-05<br/>
/// Purpose: Gives the website the loadout name, role and GearScore of board 1.
/// </remarks>
/// <param name="Name">The loadout name.</param>
/// <param name="Role">The role name, for example <c>MeleeDamage</c>.</param>
/// <param name="GearScore">The calculated GearScore.</param>
public sealed record CharacterLoadoutSummary(string Name, string Role, int GearScore);
