namespace WarmaneRaidManager.Domain.Features.Raids.Enums;

/// <summary>Identifies the lifecycle state of a raid.</summary>
/// <remarks>
/// Author: Rodolphe Balay<br/>
/// Date: 2026-09-29<br/>
/// Purpose: Provides a closed domain vocabulary for identifies the lifecycle state of a raid.
/// </remarks>
public enum RaidStatus
{
    /// <summary>Identifies a raid that is being prepared.</summary>
    Draft = 1,
    /// <summary>Identifies a raid accepting signups.</summary>
    OpenForSignups = 2,
    /// <summary>Identifies a raid with a published roster.</summary>
    RosterPublished = 3,
    /// <summary>Identifies a raid currently in progress.</summary>
    InProgress = 4,
    /// <summary>Identifies a finished raid.</summary>
    Completed = 5,
    /// <summary>Identifies a cancelled raid.</summary>
    Cancelled = 6,
}
