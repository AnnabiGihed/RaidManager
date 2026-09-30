using AspNet.Security.OAuth.Discord;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace RaidManager.Web.Features.Authentication;

/// <summary>Maps the sign-in and sign-out endpoints.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-09-30<br/>
/// Purpose: Sign-in and sign-out change cookies, which only plain HTTP requests can do, so they are endpoints, not components.
/// </remarks>
public static class AuthenticationEndpoints
{
    #region Public Methods
    /// <summary>Maps <c>GET /sign-in</c> and the antiforgery-protected <c>POST /sign-out</c>.</summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <returns>The same endpoint route builder.</returns>
    public static IEndpointRouteBuilder MapAuthenticationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet(AuthenticationRoutes.SignIn, (string? returnUrl) => Results.Challenge(
            new AuthenticationProperties { RedirectUri = AuthenticationRoutes.LocalReturnUrl(returnUrl) },
            [DiscordAuthenticationDefaults.AuthenticationScheme]));

        endpoints.MapPost(AuthenticationRoutes.SignOut, SignOutAsync);
        return endpoints;
    }
    #endregion Public Methods

    #region Private Helpers
    /// <summary>Ends the session, but only for a form posted by this website.</summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="antiforgery">The antiforgery service.</param>
    /// <returns>A redirect home after sign-out, or 400 when the antiforgery token is missing or wrong.</returns>
    /// <remarks>
    /// The antiforgery middleware only records its check, and minimal APIs enforce it only when binding form fields, which
    /// this endpoint does not, so the token is validated here.
    /// </remarks>
    private static async Task<IResult> SignOutAsync(HttpContext context, IAntiforgery antiforgery)
    {
        if (!await antiforgery.IsRequestValidAsync(context))
        {
            return Results.BadRequest();
        }

        return Results.SignOut(new AuthenticationProperties { RedirectUri = "/" }, [CookieAuthenticationDefaults.AuthenticationScheme]);
    }
    #endregion Private Helpers
}
