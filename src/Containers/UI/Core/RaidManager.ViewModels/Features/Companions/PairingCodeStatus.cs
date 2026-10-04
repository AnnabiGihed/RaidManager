namespace RaidManager.ViewModels.Features.Companions;

/// <summary>Identifies what the API says about a pairing code.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: Separates the answers the confirm page shows differently (companion pairing boards 1 and 9 to 11).
/// </remarks>
public enum PairingCodeStatus
{
    /// <summary>A companion waits for this code to be confirmed.</summary>
    Waiting,

    /// <summary>The code was confirmed now: the companion is paired.</summary>
    Paired,

    /// <summary>The code is older than ten minutes.</summary>
    Expired,

    /// <summary>The code was confirmed before.</summary>
    AlreadyConfirmed,

    /// <summary>No companion shows this code.</summary>
    Unknown,
}
