namespace RaidManager.Application.Features.Companions.Abstractions;

/// <summary>Holds the limits of the companion pairing contract.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: Keeps the numbers the companion relies on in one place: the polling interval of ADR-0030 and the input bounds.
/// </remarks>
public static class CompanionPairingDefaults
{
    #region Constants
    /// <summary>Defines the shortest wait, in seconds, between two polls for the device token.</summary>
    public const int PollingIntervalSeconds = 5;

    /// <summary>Defines the longest computer label accepted; longer ones are refused rather than cut.</summary>
    public const int MaximumComputerLabelLength = 200;

    /// <summary>Defines the longest device code or device token accepted.</summary>
    public const int MaximumSecretLength = 128;

    /// <summary>Defines how many fresh codes are tried when a generated one is already in use.</summary>
    public const int CodeAttempts = 5;
    #endregion Constants
}
