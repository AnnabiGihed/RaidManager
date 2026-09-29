namespace WarmaneRaidManager.Domain.Features.Characters.Enums;

/// <summary>Identifies the raid role supplied by a loadout.</summary>
/// <remarks>
/// Author: Rodolphe Balay<br/>
/// Date: 2026-09-29<br/>
/// Purpose: Provides a closed domain vocabulary for identifies the raid role supplied by a loadout.
/// </remarks>
public enum CharacterRole
{
    /// <summary>Identifies a tank loadout.</summary>
    Tank = 1,
    /// <summary>Identifies a healer loadout.</summary>
    Healer = 2,
    /// <summary>Identifies a melee damage loadout.</summary>
    MeleeDamage = 3,
    /// <summary>Identifies a ranged damage loadout.</summary>
    RangedDamage = 4,
}
