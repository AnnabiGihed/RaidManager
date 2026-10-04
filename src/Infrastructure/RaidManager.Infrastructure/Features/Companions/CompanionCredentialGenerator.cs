using System.Security.Cryptography;
using RaidManager.Application.Features.Companions.Abstractions;
using RaidManager.Domain.Features.Companions.ValueObjects;

namespace RaidManager.Infrastructure.Features.Companions;

/// <summary>Generates companion secrets and pairing codes with the operating system's cryptographic generator.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: Implements ADR-0030: 32 random bytes for device codes and tokens, and codes drawn uniformly from the
/// 31-symbol alphabet.
/// </remarks>
internal sealed class CompanionCredentialGenerator : ICompanionCredentialGenerator
{
    #region Constants
    /// <summary>Defines the number of random bytes in a secret.</summary>
    private const int SecretBytes = 32;
    #endregion Constants

    #region Public Methods
    /// <inheritdoc />
    public string NewSecret() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(SecretBytes))
        .TrimEnd('=')
        .Replace('+', '-')
        .Replace('/', '_');

    /// <inheritdoc />
    public PairingCode NewPairingCode() =>
        PairingCode.Create(RandomNumberGenerator.GetString(PairingCode.Alphabet, PairingCode.Length));
    #endregion Public Methods
}
