using RaidManager.Domain.Features.Characters.Enums;

namespace RaidManager.Domain.Features.Characters.ValueObjects;

/// <summary>Represents the gear the character wore at the capture.</summary>
/// <param name="Items">Every item the addon could read; a slot with no item and no unread mark is empty.</param>
/// <param name="UnreadSlots">The slots holding an item the game didn't describe, which keep the item known before.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-05<br/>
/// Purpose: Keeps an unread slot apart from an empty one, so a partial read never removes a known item (#384).
/// </remarks>
public sealed record AddonGear(IReadOnlyList<GearItem> Items, IReadOnlyCollection<EquipmentSlot> UnreadSlots);
