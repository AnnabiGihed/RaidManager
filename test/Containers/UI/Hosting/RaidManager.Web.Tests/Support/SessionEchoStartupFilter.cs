using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using RaidManager.Web.Features.Authentication;

namespace RaidManager.Web.Tests.Support;

/// <summary>Adds a test-only address that answers the matched communities the session cookie holds.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-02<br/>
/// Purpose: Lets the sign-in tests read what the encrypted session cookie keeps, through the website's own cookie handler.
/// </remarks>
public sealed class SessionEchoStartupFilter : IStartupFilter
{
    #region Constants
    /// <summary>Defines the test-only address.</summary>
    public const string Path = "/test/session-communities";
    #endregion Constants

    #region Public Methods
    /// <inheritdoc />
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
    {
        app.Map(Path, echo => echo.Run(async context =>
        {
            var session = await context.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            var user = session.Principal ?? new ClaimsPrincipal();
            await context.Response.WriteAsync(string.Join(",", user.MemberCommunityIds()));
        }));
        next(app);
    };
    #endregion Public Methods
}
