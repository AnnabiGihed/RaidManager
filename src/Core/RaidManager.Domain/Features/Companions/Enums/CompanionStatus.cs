namespace RaidManager.Domain.Features.Companions.Enums;

/// <summary>Identifies whether a paired companion may still call the API.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: Names the three outcomes of the token check of ADR-0030, which the website lists and the companion shows.
/// </remarks>
public enum CompanionStatus
{
    /// <summary>Identifies a companion that may upload.</summary>
    Active = 1,

    /// <summary>Identifies a companion its player revoked on the website; final.</summary>
    Revoked = 2,

    /// <summary>Identifies a companion unused for longer than <see cref="Aggregates.Companion.UnusedLifetime"/>; final.</summary>
    Expired = 3,
}
