namespace RaidManager.Domain.Features.Companions.Enums;

/// <summary>Identifies where a companion pairing request is in the flow of ADR-0030.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: Makes the single use of a pairing explicit: confirmed once by the player, then completed once by the companion.
/// </remarks>
public enum CompanionPairingState
{
    /// <summary>Identifies a request the companion made and the player hasn't confirmed yet.</summary>
    Pending = 1,

    /// <summary>Identifies a request the player confirmed; the companion hasn't collected its token yet.</summary>
    Confirmed = 2,

    /// <summary>Identifies a request whose token the companion collected; it can't be used again.</summary>
    Completed = 3,
}
