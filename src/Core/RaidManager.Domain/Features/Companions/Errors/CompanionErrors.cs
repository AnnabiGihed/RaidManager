using Pivot.Framework.Domain.Shared;

namespace RaidManager.Domain.Features.Companions.Errors;

/// <summary>Defines the anticipated failures of companion pairing, the device token and revocation.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: Gives the companion and the website stable, machine-readable codes for each outcome of ADR-0030, which
/// they show as the mockup's states.
/// </remarks>
public static class CompanionErrors
{
    #region Static Instances
    /// <summary>Gets the error returned when no pairing awaits confirmation with the code the player entered.</summary>
    public static readonly Error PairingNotFound = new("CompanionPairing.NotFound", "No pairing is waiting for this code.");

    /// <summary>Gets the error returned when a pairing code is used after its ten minutes.</summary>
    public static readonly Error PairingExpired = new("CompanionPairing.Expired", "This pairing code expired. Get a new code in the companion.");

    /// <summary>Gets the error returned when a pairing was already confirmed.</summary>
    public static readonly Error PairingAlreadyConfirmed = new("CompanionPairing.AlreadyConfirmed", "This pairing code was already confirmed.");

    /// <summary>Gets the error returned to a polling companion whose pairing the player hasn't confirmed yet.</summary>
    public static readonly Error PairingPending = new("CompanionPairing.Pending", "The pairing is waiting for confirmation on the website.");

    /// <summary>Gets the error returned for an unknown device code or a pairing whose token was already collected.</summary>
    public static readonly Error PairingInvalid = new("CompanionPairing.Invalid", "This pairing can't be used. Start pairing again.");

    /// <summary>Gets the error returned when a request carries no device token, or one that matches no companion.</summary>
    public static readonly Error TokenUnknown = new("Companion.TokenUnknown", "This companion isn't paired. Pair it again.");

    /// <summary>Gets the error returned when the companion was revoked on the website.</summary>
    public static readonly Error Revoked = new("Companion.Revoked", "This companion was revoked on the website. Pair it again.");

    /// <summary>Gets the error returned when the companion went unused for too long.</summary>
    public static readonly Error Expired = new("Companion.Expired", "This companion went unused for too long. Pair it again.");

    /// <summary>Gets the error returned when the player has no companion with the requested identifier.</summary>
    public static readonly Error NotFound = new("Companion.NotFound", "The companion was not found.");

    /// <summary>Gets the error returned when a companion that was already revoked is revoked again.</summary>
    public static readonly Error AlreadyRevoked = new("Companion.AlreadyRevoked", "This companion was already revoked.");
    #endregion Static Instances
}
