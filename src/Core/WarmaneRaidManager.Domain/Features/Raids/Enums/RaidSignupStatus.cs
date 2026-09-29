namespace WarmaneRaidManager.Domain.Features.Raids.Enums;

/// <summary>Identifies the lifecycle state of a raid signup.</summary>
/// <remarks>
/// Author: Rodolphe Balay<br/>
/// Date: 2026-09-29<br/>
/// Purpose: Provides a closed domain vocabulary for identifies the lifecycle state of a raid signup.
/// </remarks>
public enum RaidSignupStatus
{
    /// <summary>Identifies an available signup.</summary>
    Available = 1,
    /// <summary>Identifies a tentative signup.</summary>
    Tentative = 2,
    /// <summary>Identifies a participant who expects to be late.</summary>
    Late = 3,
    /// <summary>Identifies a declined invitation or signup.</summary>
    Declined = 4,
    /// <summary>Identifies a signup selected for the roster.</summary>
    Selected = 5,
    /// <summary>Identifies a signup placed on the bench.</summary>
    Bench = 6,
}
