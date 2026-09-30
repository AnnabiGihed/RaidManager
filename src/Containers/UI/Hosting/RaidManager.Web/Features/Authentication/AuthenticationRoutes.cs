namespace RaidManager.Web.Features.Authentication;

/// <summary>Names the website's sign-in routes.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-09-30<br/>
/// Purpose: Keeps the endpoints, the Discord events, the layout and the tests on the same paths.
/// </remarks>
public static class AuthenticationRoutes
{
    #region Constants
    /// <summary>Defines the route that starts Discord sign-in.</summary>
    public const string SignIn = "/sign-in";

    /// <summary>Defines the route that ends the session.</summary>
    public const string SignOut = "/sign-out";

    /// <summary>Defines the page that explains a failed sign-in and offers a retry.</summary>
    public const string SignInFailed = "/sign-in/failed";

    /// <summary>Defines the Discord callback path, which must match the redirect URL registered with Discord.</summary>
    public const string DiscordCallback = "/signin-discord";
    #endregion Constants

    #region Public Methods
    /// <summary>Builds the failure page URL for a reason.</summary>
    /// <param name="reason">The failure reason, such as <c>denied</c> or <c>failed</c>.</param>
    /// <returns>The relative failure page URL.</returns>
    public static string SignInFailedFor(string reason) => $"{SignInFailed}?reason={Uri.EscapeDataString(reason)}";

    /// <summary>Keeps a return URL only when it points inside the website, preventing open redirects.</summary>
    /// <param name="returnUrl">The requested return URL.</param>
    /// <returns>The URL when it is a local path; otherwise <c>/</c>.</returns>
    public static string LocalReturnUrl(string? returnUrl)
    {
        var isLocal = !string.IsNullOrEmpty(returnUrl)
            && returnUrl[0] == '/'
            && (returnUrl.Length == 1 || (returnUrl[1] != '/' && returnUrl[1] != '\\'));
        return isLocal ? returnUrl! : "/";
    }
    #endregion Public Methods
}
