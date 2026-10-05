namespace RaidManager.ViewModels.Features.Characters;

/// <summary>Describes one equipped item of a loadout.</summary>
/// <param name="Slot">The slot name, for example <c>Head</c>.</param>
/// <param name="ItemId">The WotLK item identifier.</param>
/// <param name="ItemLink">The in-game item link, which carries the item name.</param>
/// <param name="ItemLevel">The item level.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-05<br/>
/// Purpose: Carries one row of the profile's Equipment card.
/// </remarks>
public sealed record CharacterGearItem(string Slot, int ItemId, string ItemLink, int ItemLevel);
