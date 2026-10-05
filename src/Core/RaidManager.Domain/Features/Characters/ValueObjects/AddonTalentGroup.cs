namespace RaidManager.Domain.Features.Characters.ValueObjects;

/// <summary>Represents one of the character's talent groups (dual specialization).</summary>
/// <param name="Group">The talent group, 1 or 2.</param>
/// <param name="Talents">The group's talents and glyphs; its specialization name is the tree with the most points spent.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-05<br/>
/// Purpose: Gives each talent group the loadout it becomes (owner decision on #384).
/// </remarks>
public sealed record AddonTalentGroup(int Group, TalentConfiguration Talents);
