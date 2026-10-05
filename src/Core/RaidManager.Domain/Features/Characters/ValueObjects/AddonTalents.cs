namespace RaidManager.Domain.Features.Characters.ValueObjects;

/// <summary>Represents the character's talent groups and which one is active.</summary>
/// <param name="ActiveGroup">The active talent group, 1 or 2.</param>
/// <param name="Groups">Every talent group of the character.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-05<br/>
/// Purpose: Holds the talents section of an addon snapshot: the active group receives the gear worn at the capture (owner decision on #384).
/// </remarks>
public sealed record AddonTalents(int ActiveGroup, IReadOnlyList<AddonTalentGroup> Groups);
