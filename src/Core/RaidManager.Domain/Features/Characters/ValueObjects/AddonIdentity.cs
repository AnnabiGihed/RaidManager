using RaidManager.Domain.Features.Characters.Enums;

namespace RaidManager.Domain.Features.Characters.ValueObjects;

/// <summary>Represents who the addon saw the character to be.</summary>
/// <param name="Class">The WotLK class.</param>
/// <param name="Race">The WotLK race.</param>
/// <param name="Faction">The faction.</param>
/// <param name="Level">The character level.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-05<br/>
/// Purpose: Holds the identity section of an addon snapshot, which imports a new character and refreshes a known one (#384).
/// </remarks>
public sealed record AddonIdentity(WowClass Class, WowRace Race, Faction Faction, int Level);
