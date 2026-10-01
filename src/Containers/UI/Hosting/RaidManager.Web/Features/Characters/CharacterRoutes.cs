using RaidManager.Web.Features.Authentication;

namespace RaidManager.Web.Features.Characters;

/// <summary>Names the website's character routes.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Keeps the sign-in redirect, the review page and the tests on the same paths.
/// </remarks>
public static class CharacterRoutes
{
    #region Constants
    /// <summary>Defines the page where a player approves or rejects newly found characters.</summary>
    public const string Review = "/characters/review";
    #endregion Constants

    #region Public Methods
    /// <summary>Builds the review page URL that returns to a page once the player continues.</summary>
    /// <param name="returnUrl">The page the player asked for.</param>
    /// <returns>The relative review page URL.</returns>
    public static string ReviewFor(string? returnUrl) => $"{Review}?returnUrl={Uri.EscapeDataString(ContinueUrl(returnUrl))}";

    /// <summary>Chooses where the review page continues to: a local return URL other than the review page, or home.</summary>
    /// <param name="returnUrl">The requested return URL.</param>
    /// <returns>The URL to continue to.</returns>
    public static string ContinueUrl(string? returnUrl)
    {
        var local = AuthenticationRoutes.LocalReturnUrl(returnUrl);
        return local.StartsWith(Review, StringComparison.OrdinalIgnoreCase) ? "/" : local;
    }
    #endregion Public Methods
}
