namespace WarmaneRaidManager.Domain.Features.Characters.ValueObjects;

/// <summary>Represents a normalized Warmane character name.</summary>
/// <remarks>
/// Author: Rodolphe Balay<br/>
/// Date: 2026-09-29<br/>
/// Purpose: Applies one canonical representation for character identity and lookup.
/// </remarks>
public sealed class CharacterName
{
    #region Constants
    /// <summary>Defines the minimum supported character-name length.</summary>
    private const int MinLength = 2;

    /// <summary>Defines the maximum supported character-name length.</summary>
    private const int MaxLength = 12;
    #endregion Constants

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="CharacterName"/> class.</summary>
    /// <param name="value">The normalized character name.</param>
    private CharacterName(string value)
    {
        Value = value;
    }
    #endregion Constructors

    #region Properties
    /// <summary>Gets the normalized character name.</summary>
    public string Value { get; }
    #endregion Properties

    #region Factory Methods
    /// <summary>Creates and normalizes a character name.</summary>
    /// <param name="value">The character name supplied by a game-data source.</param>
    /// <returns>The validated character name.</returns>
    /// <exception cref="DomainException">Thrown when the name is outside supported WotLK naming constraints.</exception>
    public static CharacterName Create(string value)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalized) || normalized.Length < MinLength || normalized.Length > MaxLength || !normalized.All(char.IsLetter))
        {
            throw new DomainException("Character name must contain between 2 and 12 letters.");
        }

        var canonical = char.ToUpperInvariant(normalized[0]) + normalized[1..].ToLowerInvariant();
        return new CharacterName(canonical);
    }
    #endregion Factory Methods

    #region Overrides
    /// <summary>Returns the normalized character name.</summary>
    /// <returns>The normalized character name.</returns>
    public override string ToString() => Value;
    #endregion Overrides
}
