namespace RaidManager.Domain.Features.Companions.ValueObjects;

/// <summary>Represents the six-character code a companion shows and the player confirms on the website.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: Applies the code format of ADR-0030: six characters from a 31-symbol alphabet without look-alike characters,
/// stored without separator and shown as <c>XXX-XXX</c>, so a code typed or pasted with a dash, spaces or lower case still matches.
/// </remarks>
public sealed class PairingCode : IEquatable<PairingCode>
{
    #region Constants
    /// <summary>Defines the symbols a code is made of: digits and capital letters without 0, O, 1, I and L.</summary>
    public const string Alphabet = "23456789ABCDEFGHJKMNPQRSTUVWXYZ";

    /// <summary>Defines the number of symbols in a code.</summary>
    public const int Length = 6;

    /// <summary>Defines the separator shown between the two halves of a code.</summary>
    private const char Separator = '-';
    #endregion Constants

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="PairingCode"/> class.</summary>
    /// <param name="value">The normalized code, without separator.</param>
    private PairingCode(string value)
    {
        Value = value;
    }
    #endregion Constructors

    #region Properties
    /// <summary>Gets the normalized code, six symbols without separator.</summary>
    public string Value { get; }
    #endregion Properties

    #region Operators
    /// <summary>Compares two values by content.</summary>
    /// <param name="left">The first value.</param>
    /// <param name="right">The second value.</param>
    /// <returns><see langword="true"/> when both are absent or hold the same value.</returns>
    public static bool operator ==(PairingCode? left, PairingCode? right) => left is null ? right is null : left.Equals(right);

    /// <summary>Compares two values by content.</summary>
    /// <param name="left">The first value.</param>
    /// <param name="right">The second value.</param>
    /// <returns><see langword="true"/> when they hold different values.</returns>
    public static bool operator !=(PairingCode? left, PairingCode? right) => !(left == right);
    #endregion Operators

    #region Factory Methods
    /// <summary>Creates a code from what a player typed or a companion sent.</summary>
    /// <param name="value">The code, with or without its dash, in any case, with surrounding spaces.</param>
    /// <returns>The normalized code.</returns>
    /// <exception cref="DomainException">Thrown when the code isn't six symbols of <see cref="Alphabet"/>.</exception>
    public static PairingCode Create(string value)
    {
        if (!TryCreate(value, out var code))
        {
            throw new UnknownDomainException($"A pairing code is {Length} characters from {Alphabet}.");
        }

        return code;
    }

    /// <summary>Creates a code from what a player typed or a companion sent, without throwing.</summary>
    /// <param name="value">The code, with or without its dash, in any case, with surrounding spaces.</param>
    /// <param name="code">The normalized code, when the value is one.</param>
    /// <returns><see langword="true"/> when the value is a pairing code.</returns>
    public static bool TryCreate(string? value, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out PairingCode? code)
    {
        var normalized = new string((value ?? string.Empty).Where(symbol => symbol != Separator && !char.IsWhiteSpace(symbol)).ToArray())
            .ToUpperInvariant();
        code = normalized.Length == Length && normalized.All(symbol => Alphabet.Contains(symbol, StringComparison.Ordinal))
            ? new PairingCode(normalized)
            : null;
        return code is not null;
    }
    #endregion Factory Methods

    #region Public Methods
    /// <inheritdoc />
    public bool Equals(PairingCode? other) => other is not null && Value == other.Value;
    #endregion Public Methods

    #region Overrides
    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as PairingCode);

    /// <inheritdoc />
    public override int GetHashCode() => Value.GetHashCode(StringComparison.Ordinal);

    /// <summary>Returns the code as the companion and the website show it, such as <c>K7M-4QX</c>.</summary>
    /// <returns>The code with its dash.</returns>
    public override string ToString() => $"{Value[..(Length / 2)]}{Separator}{Value[(Length / 2)..]}";
    #endregion Overrides
}
