using RaidManager.Domain.Features.Characters.Enums;

namespace RaidManager.Application.Features.Characters.Queries.GetCharacterProfile;

/// <summary>Describes a loadout on a profile.</summary>
/// <param name="Name">The loadout name, from the equipment set.</param>
/// <param name="Role">The raid role.</param>
/// <param name="IsPrimary">Whether it is the primary loadout.</param>
/// <param name="GearScore">The calculated GearScore.</param>
/// <param name="Talents">The talent points per tree, for example <c>0/53/18</c>.</param>
/// <param name="Source">Where the loadout's data came from.</param>
/// <param name="Gear">The equipped items, in slot order.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-05<br/>
/// Purpose: Feeds the profile's Loadouts card, and its Equipment card for the primary loadout.
/// </remarks>
public sealed record ProfileLoadoutResponse(
    string Name,
    CharacterRole Role,
    bool IsPrimary,
    int GearScore,
    string Talents,
    CharacterDataSource Source,
    IReadOnlyList<ProfileGearItemResponse> Gear);
