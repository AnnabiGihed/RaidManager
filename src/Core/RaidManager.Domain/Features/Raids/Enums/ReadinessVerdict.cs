namespace RaidManager.Domain.Features.Raids.Enums;

/// <summary>Identifies whether a character can join one raid target at the scheduled raid start.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-09-29<br/>
/// Purpose: Orders the raid-start readiness verdicts from least to most restrictive, so a combined raid takes the highest value.
/// </remarks>
public enum ReadinessVerdict
{
    /// <summary>Identifies fresh evidence confirming no matching save at raid start.</summary>
    Available = 1,
    /// <summary>Identifies a matching save that expires before raid start.</summary>
    ResetsBeforeRaid = 2,
    /// <summary>Identifies missing, stale or uncertain evidence that must be synchronized again before eligibility is known.</summary>
    NeedsFreshSync = 3,
    /// <summary>Identifies a matching save confirmed to remain active at raid start; it blocks signup and roster assignment.</summary>
    LockedThroughRaid = 4,
}
