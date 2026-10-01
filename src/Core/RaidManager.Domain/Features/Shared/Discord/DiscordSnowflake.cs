namespace RaidManager.Domain.Features.Shared.Discord;

/// <summary>Validates Discord snowflakes, the numeric identifiers Discord gives servers, roles and users.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Keeps one rule for every Discord identifier the domain stores, so a server id and a role id are checked alike.
/// </remarks>
internal static class DiscordSnowflake
{
    #region Constants
    /// <summary>Defines the maximum decimal length of a Discord snowflake (a 64-bit unsigned integer).</summary>
    private const int MaximumLength = 20;
    #endregion Constants

    #region Public Methods
    /// <summary>Ensures a value is a Discord snowflake.</summary>
    /// <param name="value">The requested identifier.</param>
    /// <param name="subject">What the identifier names, used in the error message, such as "Discord role".</param>
    /// <returns>The trimmed identifier.</returns>
    /// <exception cref="DomainException">Thrown when the value is empty, too long or not numeric.</exception>
    public static string Ensure(string value, string subject)
    {
        var trimmed = value?.Trim() ?? string.Empty;
        if (trimmed.Length == 0 || trimmed.Length > MaximumLength || !trimmed.All(char.IsAsciiDigit))
        {
            throw new UnknownDomainException($"{subject} identifier must be a numeric snowflake of at most {MaximumLength} digits.");
        }

        return trimmed;
    }
    #endregion Public Methods
}
