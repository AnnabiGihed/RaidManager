namespace RaidManager.Domain.Features.Characters.ValueObjects;

/// <summary>Captures the raid-relevant character statistics observed for a concrete loadout.</summary>
/// <param name="Strength">The effective Strength value.</param>
/// <param name="Agility">The effective Agility value.</param>
/// <param name="Stamina">The effective Stamina value.</param>
/// <param name="Intellect">The effective Intellect value.</param>
/// <param name="Spirit">The effective Spirit value.</param>
/// <param name="Health">The maximum health value.</param>
/// <param name="Armor">The effective armor value.</param>
/// <param name="AttackPower">The effective attack power.</param>
/// <param name="SpellPower">The effective spell power.</param>
/// <param name="HitRating">The relevant hit rating.</param>
/// <param name="HitPercent">The relevant hit percentage.</param>
/// <param name="CritRating">The relevant critical-strike rating.</param>
/// <param name="CritPercent">The relevant critical-strike percentage.</param>
/// <param name="HasteRating">The relevant haste rating.</param>
/// <param name="HastePercent">The relevant haste percentage.</param>
/// <param name="ExpertiseRating">The expertise rating.</param>
/// <param name="ExpertiseMainHand">The main-hand expertise value.</param>
/// <param name="ExpertiseOffHand">The off-hand expertise value.</param>
/// <param name="ArmorPenetrationRating">The armor-penetration rating.</param>
/// <param name="ArmorPenetrationPercent">The armor-penetration percentage.</param>
/// <param name="DefenseSkill">The defense skill value.</param>
/// <param name="DodgePercent">The dodge percentage.</param>
/// <param name="ParryPercent">The parry percentage.</param>
/// <param name="BlockPercent">The block percentage.</param>
/// <param name="ResilienceRating">The resilience rating.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-09-29<br/>
/// Purpose: Preserves the actual game-calculated stat snapshot associated with a synchronized equipment loadout.
/// </remarks>
public sealed record CombatStats(
    int Strength,
    int Agility,
    int Stamina,
    int Intellect,
    int Spirit,
    int Health,
    int Armor,
    int AttackPower,
    int SpellPower,
    int HitRating,
    decimal HitPercent,
    int CritRating,
    decimal CritPercent,
    int HasteRating,
    decimal HastePercent,
    int ExpertiseRating,
    decimal ExpertiseMainHand,
    decimal ExpertiseOffHand,
    int ArmorPenetrationRating,
    decimal ArmorPenetrationPercent,
    int DefenseSkill,
    decimal DodgePercent,
    decimal ParryPercent,
    decimal BlockPercent,
    int ResilienceRating);
