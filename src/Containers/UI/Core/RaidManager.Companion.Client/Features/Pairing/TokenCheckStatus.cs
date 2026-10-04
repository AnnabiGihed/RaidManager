namespace RaidManager.Companion.Client.Features.Pairing;

/// <summary>Lists what a check of the stored device token can answer.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: Separates a token RaidManager refused, which must be deleted, from a check that couldn't be made, which
/// must keep it (ADR-0030).
/// </remarks>
public enum TokenCheckStatus
{
    /// <summary>The token is valid.</summary>
    Valid,

    /// <summary>RaidManager refused the token: it was revoked, went unused for 180 days, or is unknown.</summary>
    Refused,

    /// <summary>RaidManager didn't answer, or answered with anything else.</summary>
    Unavailable,
}
