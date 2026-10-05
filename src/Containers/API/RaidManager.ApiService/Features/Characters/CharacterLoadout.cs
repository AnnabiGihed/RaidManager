using RaidManager.Application.Features.Characters.Queries.GetCharacterProfile;

namespace RaidManager.ApiService.Features.Characters;

/// <summary>Describes a loadout on a profile, with its equipment.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-05<br/>
/// Purpose: Carries one entry of the profile's Loadouts card and, for the primary loadout, its Equipment card.
/// </remarks>
/// <param name="Name">The loadout name.</param>
/// <param name="Role">The role name, for example <c>Tank</c>.</param>
/// <param name="IsPrimary">Whether it is the primary loadout.</param>
/// <param name="GearScore">The calculated GearScore.</param>
/// <param name="Talents">The talent points per tree, for example <c>0/53/18</c>.</param>
/// <param name="Source">The source name, for example <c>WowAddon</c>.</param>
/// <param name="Gear">The equipped items, in slot order.</param>
public sealed record CharacterLoadout(
    string Name,
    string Role,
    bool IsPrimary,
    int GearScore,
    string Talents,
    string Source,
    IReadOnlyList<CharacterGearItem> Gear)
{
    #region Public Methods
    /// <summary>Maps the application response to the transport record.</summary>
    /// <param name="loadout">The application response.</param>
    /// <returns>The transport record.</returns>
    public static CharacterLoadout From(ProfileLoadoutResponse loadout)
    {
        ArgumentNullException.ThrowIfNull(loadout);
        return new CharacterLoadout(
            loadout.Name,
            loadout.Role.ToString(),
            loadout.IsPrimary,
            loadout.GearScore,
            loadout.Talents,
            loadout.Source.ToString(),
            [.. loadout.Gear.Select(item => new CharacterGearItem(item.Slot.ToString(), item.ItemId, item.ItemLink, item.ItemLevel))]);
    }
    #endregion Public Methods
}
