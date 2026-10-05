using RaidManager.Domain.Features.Characters.Enums;

namespace RaidManager.Application.Features.Characters.Queries.GetCharacterProfile;

/// <summary>Describes one equipped item of a loadout.</summary>
/// <param name="Slot">The equipment slot.</param>
/// <param name="ItemId">The WotLK item identifier.</param>
/// <param name="ItemLink">The in-game item link, which carries the item name.</param>
/// <param name="ItemLevel">The item level, or <see langword="null"/> before an item catalog gives one (#552).</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-05<br/>
/// Purpose: Feeds the profile's Equipment card.
/// </remarks>
public sealed record ProfileGearItemResponse(EquipmentSlot Slot, int ItemId, string ItemLink, int? ItemLevel);
