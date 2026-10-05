namespace RaidManager.ViewModels.Features.Characters;

/// <summary>Describes a loadout on a profile, with its equipment.</summary>
/// <param name="Name">The loadout name.</param>
/// <param name="Role">The role name, for example <c>Tank</c>.</param>
/// <param name="IsPrimary">Whether it is the primary loadout.</param>
/// <param name="GearScore">The calculated GearScore, or <see langword="null"/> before an item catalog gives one (#552).</param>
/// <param name="Talents">The talent points per tree, for example <c>0/53/18</c>.</param>
/// <param name="Source">The source name, for example <c>WowAddon</c>.</param>
/// <param name="Gear">The equipped items, in slot order.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-05<br/>
/// Purpose: Carries one entry of the profile's Loadouts card and, for the primary loadout, its Equipment card.
/// </remarks>
public sealed record CharacterLoadout(
    string Name,
    string Role,
    bool IsPrimary,
    int? GearScore,
    string Talents,
    string Source,
    IReadOnlyList<CharacterGearItem> Gear);
