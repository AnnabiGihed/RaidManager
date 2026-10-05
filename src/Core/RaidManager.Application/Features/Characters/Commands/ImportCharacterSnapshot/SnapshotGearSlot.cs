namespace RaidManager.Application.Features.Characters.Commands.ImportCharacterSnapshot;

/// <summary>Represents one inventory slot.</summary>
/// <param name="Slot">The inventory slot, 1 (head) to 19 (tabard).</param>
/// <param name="Empty">Whether nothing is equipped there.</param>
/// <param name="Status"><c>unavailable</c> when the slot holds an item the game didn't describe.</param>
/// <param name="ItemString">The item string, <c>item:itemId:enchantId:...</c>.</param>
/// <param name="ItemId">The item id.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-05<br/>
/// Purpose: Mirrors one gear slot of the addon contract (#384).
/// </remarks>
public sealed record SnapshotGearSlot(int Slot, bool? Empty, string? Status, string? ItemString, int? ItemId);
