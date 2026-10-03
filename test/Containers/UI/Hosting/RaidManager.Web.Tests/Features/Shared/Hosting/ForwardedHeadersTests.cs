using System.Net;
using Microsoft.AspNetCore.WebUtilities;
using Shouldly;
using Xunit;
using RaidManager.Web.Features.Authentication;
using RaidManager.Web.Tests.Support;

namespace RaidManager.Web.Tests.Features.Shared.Hosting;

/// <summary>Verifies the Discord redirect a website builds behind the shared Caddy.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-03<br/>
/// Purpose: Caddy ends HTTPS and forwards plain HTTP with <c>X-Forwarded-Proto</c>. Deployed environments set
/// <c>ASPNETCORE_FORWARDEDHEADERS_ENABLED</c>, so the website trusts it and asks Discord to return over HTTPS (ADR-0027).
/// </remarks>
public sealed class ForwardedHeadersTests
{
    #region Constants
    /// <summary>Defines the setting the <c>ASPNETCORE_FORWARDEDHEADERS_ENABLED</c> variable becomes.</summary>
    private const string ForwardedHeadersEnabledKey = "ForwardedHeaders_Enabled";
    #endregion Constants

    #region Tests
    /// <summary>Starts sign-in over the proxy's plain HTTP with forwarded headers turned on.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task BehindTheProxyDiscordReturnsOverHttps()
    {
        await using var site = new WebsiteFactory();
        site.Settings[ForwardedHeadersEnabledKey] = "true";

        var redirectUri = await SignInRedirectUriAsync(site);

        redirectUri.ShouldBe("https://localhost" + AuthenticationRoutes.DiscordCallback);
    }

    /// <summary>Starts the same sign-in without forwarded headers, as the local run does.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task WithoutForwardedHeadersTheProxyHeaderIsIgnored()
    {
        await using var site = new WebsiteFactory();

        var redirectUri = await SignInRedirectUriAsync(site);

        redirectUri.ShouldBe("http://localhost" + AuthenticationRoutes.DiscordCallback);
    }
    #endregion Tests

    #region Private Helpers
    /// <summary>Requests the sign-in page as Caddy forwards it, and reads the return address sent to Discord.</summary>
    /// <param name="site">The website.</param>
    /// <returns>The <c>redirect_uri</c> of the Discord authorization request.</returns>
    private static async Task<string> SignInRedirectUriAsync(WebsiteFactory site)
    {
        using var proxy = site.CreateClient(new() { BaseAddress = new Uri("http://localhost"), AllowAutoRedirect = false });
        using var request = new HttpRequestMessage(HttpMethod.Get, AuthenticationRoutes.SignIn);
        request.Headers.Add("X-Forwarded-Proto", "https");

        var response = await proxy.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        var location = response.Headers.Location.ShouldNotBeNull();
        return QueryHelpers.ParseQuery(location.Query)["redirect_uri"].ToString();
    }
    #endregion Private Helpers
}
