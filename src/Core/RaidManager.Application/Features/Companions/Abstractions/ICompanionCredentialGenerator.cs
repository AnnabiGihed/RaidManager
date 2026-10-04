using RaidManager.Domain.Features.Companions.ValueObjects;

namespace RaidManager.Application.Features.Companions.Abstractions;

/// <summary>Generates the random secrets and codes of companion pairing.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: Keeps the cryptographic generator out of the handlers, so tests can fix the values (ADR-0030).
/// </remarks>
public interface ICompanionCredentialGenerator
{
    #region Methods
    /// <summary>Generates a secret of 32 random bytes, for a device code or a device token.</summary>
    /// <returns>The secret, URL-safe Base64 without padding.</returns>
    string NewSecret();

    /// <summary>Generates a random pairing code.</summary>
    /// <returns>The code.</returns>
    PairingCode NewPairingCode();
    #endregion Methods
}
