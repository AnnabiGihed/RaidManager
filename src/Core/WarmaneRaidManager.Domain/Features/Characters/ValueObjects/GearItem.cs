using WarmaneRaidManager.Domain.Features.Characters.Enums;

namespace WarmaneRaidManager.Domain.Features.Characters.ValueObjects;

/// <summary>Represents one equipped item belonging to a synchronized character loadout.</summary>
/// <param name="Slot">The equipment slot occupied by the item.</param>
/// <param name="ItemId">The WotLK item identifier.</param>
/// <param name="DisplayId">The optional item display identifier used by the future 3D character renderer.</param>
/// <param name="ItemLink">The complete in-game item link containing enchant, gem and variant information.</param>
/// <param name="ItemLevel">The item level observed during synchronization.</param>
/// <remarks>
/// Author: Rodolphe Balay<br/>
/// Date: 2026-09-29<br/>
/// Purpose: Keeps exact loadout equipment independent from whichever set the character is currently wearing on the Armory.
/// </remarks>
public sealed record GearItem(EquipmentSlot Slot, int ItemId, int? DisplayId, string ItemLink, int ItemLevel);
