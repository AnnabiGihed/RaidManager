using System.Security.Cryptography;
using System.Text;

namespace RaidManager.Domain.Features.Companions.ValueObjects;

/// <summary>Represents the SHA-256 hash of a companion secret: a device code or a device token.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: Keeps only hashes of the companion's secrets, as ADR-0030 decides, so a copy of the database can't be used
/// to pair or upload. The secrets are 32 random bytes, so an unsalted fast hash is enough.
/// </remarks>
public sealed class CredentialHash : IEquatable<CredentialHash>
{
    #region Constants
    /// <summary>Defines the length of a hash in lower-case hexadecimal characters.</summary>
    public const int Length = 64;
    #endregion Constants

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="CredentialHash"/> class.</summary>
    /// <param name="value">The lower-case hexadecimal hash.</param>
    private CredentialHash(string value)
    {
        Value = value;
    }
    #endregion Constructors

    #region Properties
    /// <summary>Gets the hash as 64 lower-case hexadecimal characters.</summary>
    public string Value { get; }
    #endregion Properties

    #region Operators
    /// <summary>Compares two values by content.</summary>
    /// <param name="left">The first value.</param>
    /// <param name="right">The second value.</param>
    /// <returns><see langword="true"/> when both are absent or hold the same value.</returns>
    public static bool operator ==(CredentialHash? left, CredentialHash? right) => left is null ? right is null : left.Equals(right);

    /// <summary>Compares two values by content.</summary>
    /// <param name="left">The first value.</param>
    /// <param name="right">The second value.</param>
    /// <returns><see langword="true"/> when they hold different values.</returns>
    public static bool operator !=(CredentialHash? left, CredentialHash? right) => !(left == right);
    #endregion Operators

    #region Factory Methods
    /// <summary>Hashes a secret.</summary>
    /// <param name="secret">The device code or device token.</param>
    /// <returns>The hash of the secret.</returns>
    /// <exception cref="DomainException">Thrown when the secret is empty.</exception>
    public static CredentialHash Of(string secret)
    {
        if (string.IsNullOrEmpty(secret))
        {
            throw new UnknownDomainException("A companion secret is required.");
        }

        return new CredentialHash(Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(secret))));
    }

    /// <summary>Restores a stored hash.</summary>
    /// <param name="value">The stored lower-case hexadecimal hash.</param>
    /// <returns>The hash.</returns>
    /// <exception cref="DomainException">Thrown when the value isn't a SHA-256 hash in lower-case hexadecimal.</exception>
    public static CredentialHash FromStored(string value)
    {
        if (value is null || value.Length != Length || !value.All(symbol => char.IsAsciiHexDigitLower(symbol)))
        {
            throw new UnknownDomainException("A stored companion secret hash is 64 lower-case hexadecimal characters.");
        }

        return new CredentialHash(value);
    }
    #endregion Factory Methods

    #region Public Methods
    /// <inheritdoc />
    public bool Equals(CredentialHash? other) => other is not null && Value == other.Value;
    #endregion Public Methods

    #region Overrides
    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as CredentialHash);

    /// <inheritdoc />
    public override int GetHashCode() => Value.GetHashCode(StringComparison.Ordinal);

    /// <summary>Returns the hash.</summary>
    /// <returns>The lower-case hexadecimal hash.</returns>
    public override string ToString() => Value;
    #endregion Overrides
}
