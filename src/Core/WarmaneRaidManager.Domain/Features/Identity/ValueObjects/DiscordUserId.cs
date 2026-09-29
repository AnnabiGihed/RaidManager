namespace WarmaneRaidManager.Domain.Features.Identity.ValueObjects;

/// <summary>Represents the immutable Discord snowflake identifying an authenticated user.</summary>
/// <remarks>
/// Author: Rodolphe Balay<br/>
/// Date: 2026-09-29<br/>
/// Purpose: Keeps the external Discord identity explicit and validated inside the domain model.
/// </remarks>
public sealed class DiscordUserId
{
    #region Constants
    /// <summary>Defines the maximum supported decimal length of a Discord snowflake.</summary>
    private const int MaxSnowflakeLength = 20;
    #endregion Constants

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="DiscordUserId"/> class.</summary>
    /// <param name="value">The decimal Discord snowflake.</param>
    private DiscordUserId(string value)
    {
        Value = value;
    }
    #endregion Constructors

    #region Properties
    /// <summary>Gets the decimal Discord snowflake.</summary>
    public string Value { get; }
    #endregion Properties

    #region Factory Methods
    /// <summary>Creates a validated Discord user identifier.</summary>
    /// <param name="value">The decimal Discord snowflake.</param>
    /// <returns>The validated Discord user identifier.</returns>
    /// <exception cref="DomainException">Thrown when the identifier is empty, too long or non-numeric.</exception>
    public static DiscordUserId Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > MaxSnowflakeLength || !value.All(char.IsDigit))
        {
            throw new DomainException("Discord user identifier must be a numeric snowflake of at most 20 digits.");
        }

        return new DiscordUserId(value);
    }
    #endregion Factory Methods

    #region Overrides
    /// <summary>Returns the Discord snowflake.</summary>
    /// <returns>The decimal Discord snowflake.</returns>
    public override string ToString() => Value;
    #endregion Overrides
}
