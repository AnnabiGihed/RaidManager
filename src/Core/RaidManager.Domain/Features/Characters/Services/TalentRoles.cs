using RaidManager.Domain.Features.Characters.Enums;
using RaidManager.Domain.Features.Characters.ValueObjects;

namespace RaidManager.Domain.Features.Characters.Services;

/// <summary>Derives the raid role of a talent configuration from its class and its tree with the most points spent.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-05<br/>
/// Purpose: Gives each addon loadout a role until the player corrects it (owner decision on #384, editing in #545). A
/// tree that serves two roles takes its usual raid role: Blood and Feral are read as tank and melee damage.
/// </remarks>
public static class TalentRoles
{
    #region Static Instances
    /// <summary>Stores the raid role of each class's three talent trees, in the game's tree order.</summary>
    private static readonly Dictionary<WowClass, CharacterRole[]> RolesByTree = new()
    {
        [WowClass.DeathKnight] = [CharacterRole.Tank, CharacterRole.MeleeDamage, CharacterRole.MeleeDamage],
        [WowClass.Druid] = [CharacterRole.RangedDamage, CharacterRole.MeleeDamage, CharacterRole.Healer],
        [WowClass.Hunter] = [CharacterRole.RangedDamage, CharacterRole.RangedDamage, CharacterRole.RangedDamage],
        [WowClass.Mage] = [CharacterRole.RangedDamage, CharacterRole.RangedDamage, CharacterRole.RangedDamage],
        [WowClass.Paladin] = [CharacterRole.Healer, CharacterRole.Tank, CharacterRole.MeleeDamage],
        [WowClass.Priest] = [CharacterRole.Healer, CharacterRole.Healer, CharacterRole.RangedDamage],
        [WowClass.Rogue] = [CharacterRole.MeleeDamage, CharacterRole.MeleeDamage, CharacterRole.MeleeDamage],
        [WowClass.Shaman] = [CharacterRole.RangedDamage, CharacterRole.MeleeDamage, CharacterRole.Healer],
        [WowClass.Warlock] = [CharacterRole.RangedDamage, CharacterRole.RangedDamage, CharacterRole.RangedDamage],
        [WowClass.Warrior] = [CharacterRole.MeleeDamage, CharacterRole.MeleeDamage, CharacterRole.Tank],
    };
    #endregion Static Instances

    #region Public Methods
    /// <summary>Gets the raid role of a talent configuration.</summary>
    /// <param name="wowClass">The character's class.</param>
    /// <param name="talents">The talent configuration.</param>
    /// <returns>The role of the tree with the most points spent; the first such tree on a tie or with no points.</returns>
    public static CharacterRole RoleOf(WowClass wowClass, TalentConfiguration talents)
    {
        ArgumentNullException.ThrowIfNull(talents);
        return RolesByTree[wowClass][MainTreeIndex(talents)];
    }

    /// <summary>Gets the zero-based index of the tree with the most points spent.</summary>
    /// <param name="talents">The talent configuration.</param>
    /// <returns>0, 1 or 2; the first such tree on a tie.</returns>
    public static int MainTreeIndex(TalentConfiguration talents)
    {
        ArgumentNullException.ThrowIfNull(talents);
        int[] points = [talents.FirstTreePoints, talents.SecondTreePoints, talents.ThirdTreePoints];
        return Array.IndexOf(points, points.Max());
    }
    #endregion Public Methods
}
