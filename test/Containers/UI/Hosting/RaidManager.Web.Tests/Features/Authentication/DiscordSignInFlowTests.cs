using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.WebUtilities;
using Shouldly;
using Xunit;
using RaidManager.Web.Features.Authentication;
using RaidManager.Web.Features.Characters;
using RaidManager.Web.Tests.Support;

namespace RaidManager.Web.Tests.Features.Authentication;

/// <summary>Drives the Discord sign-in flow through HTTP, as a browser does, with Discord and the API stubbed.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-09-30<br/>
/// Purpose: Proves the challenge, the callback that issues the session, the failure paths, open-redirect protection and sign-out.
/// </remarks>
public sealed partial class DiscordSignInFlowTests
{
    #region Tests
    /// <summary>Starts sign-in and checks the redirect to Discord.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task SignInRedirectsToDiscordWithOnlyTheIdentifyScope()
    {
        await using var site = new WebsiteFactory();
        using var browser = site.CreateBrowser();

        var response = await browser.GetAsync(AuthenticationRoutes.SignIn);

        response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        var location = response.Headers.Location.ShouldNotBeNull();
        location.GetLeftPart(UriPartial.Path).ShouldBe("https://discord.com/api/oauth2/authorize");
        var query = QueryHelpers.ParseQuery(location.Query);
        query["client_id"].ToString().ShouldBe(WebsiteFactory.ClientId);
        query["scope"].ToString().ShouldBe("identify");
        query["redirect_uri"].ToString().ShouldBe("https://localhost" + AuthenticationRoutes.DiscordCallback);
    }

    /// <summary>Completes a callback and checks the session and the call to the API.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task SuccessfulCallbackSignsThePlayerInAndReturnsToTheRequestedPage()
    {
        await using var site = new WebsiteFactory();
        using var browser = site.CreateBrowser();
        var state = await StartSignInAsync(browser, "/raids?week=40");

        var callback = await browser.GetAsync($"{AuthenticationRoutes.DiscordCallback}?code=test-code&state={Uri.EscapeDataString(state)}");

        callback.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        callback.Headers.Location.ShouldNotBeNull().OriginalString.ShouldBe("/raids?week=40");
        callback.Headers.GetValues("Set-Cookie").ShouldContain(cookie =>
            cookie.StartsWith(RaidManagerAuthenticationExtensions.SessionCookieName, StringComparison.Ordinal)
            && cookie.Contains("httponly", StringComparison.OrdinalIgnoreCase)
            && cookie.Contains("secure", StringComparison.OrdinalIgnoreCase)
            && cookie.Contains("samesite=lax", StringComparison.OrdinalIgnoreCase));
        site.IdentityApi.LastDiscordUserId.ShouldBe(DiscordBackchannelStub.DiscordUserId);
        site.IdentityApi.LastDisplayName.ShouldBe(DiscordBackchannelStub.GlobalName);
        site.IdentityApi.LastAvatarUrl.ShouldBe(
            $"https://cdn.discordapp.com/avatars/{DiscordBackchannelStub.DiscordUserId}/{DiscordBackchannelStub.AvatarHash}.png");

        var home = await browser.GetStringAsync("/");
        home.ShouldContain(DiscordBackchannelStub.GlobalName);
        home.ShouldContain("Sign out");
    }

    /// <summary>Signs in with a character awaiting the player's decision.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task PendingClaimLeadsToTheReviewPageKeepingTheRequestedPage()
    {
        await using var site = new WebsiteFactory();
        site.ClaimsApi.Claims = [FakeCharacterClaimsApiClient.Claim("Arthasdk"), FakeCharacterClaimsApiClient.Claim("Sylvanash", "Conflict")];
        using var browser = site.CreateBrowser();
        var state = await StartSignInAsync(browser, "/raids?week=40");

        var callback = await browser.GetAsync($"{AuthenticationRoutes.DiscordCallback}?code=test-code&state={Uri.EscapeDataString(state)}");

        callback.Headers.Location.ShouldNotBeNull().OriginalString.ShouldBe(CharacterRoutes.ReviewFor("/raids?week=40"));
        SessionCookieWasIssued(callback).ShouldBeTrue();
        var review = await browser.GetAsync(CharacterRoutes.ReviewFor("/raids?week=40"));
        review.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await review.Content.ReadAsStringAsync()).ShouldContain("Review your new characters");
    }

    /// <summary>Signs in while only a conflict waits for an officer.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task OnlyAConflictStillLeadsToTheReviewPage()
    {
        await using var site = new WebsiteFactory();
        site.ClaimsApi.Claims = [FakeCharacterClaimsApiClient.Claim("Sylvanash", "Conflict")];
        using var browser = site.CreateBrowser();
        var state = await StartSignInAsync(browser, "/raids");

        var callback = await browser.GetAsync($"{AuthenticationRoutes.DiscordCallback}?code=test-code&state={Uri.EscapeDataString(state)}");

        callback.Headers.Location.ShouldNotBeNull().OriginalString.ShouldBe(CharacterRoutes.ReviewFor("/raids"));
    }

    /// <summary>Signs in with no claim waiting.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task NoWaitingClaimLeadsToTheRequestedPage()
    {
        await using var site = new WebsiteFactory();
        using var browser = site.CreateBrowser();
        var state = await StartSignInAsync(browser, "/raids");

        var callback = await browser.GetAsync($"{AuthenticationRoutes.DiscordCallback}?code=test-code&state={Uri.EscapeDataString(state)}");

        callback.Headers.Location.ShouldNotBeNull().OriginalString.ShouldBe("/raids");
    }

    /// <summary>Signs in while the claims can't be checked.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task FailedPendingCheckStillSignsInAndOpensTheReviewPage()
    {
        await using var site = new WebsiteFactory();
        site.ClaimsApi.Fails = true;
        using var browser = site.CreateBrowser();
        var state = await StartSignInAsync(browser, "/");

        var callback = await browser.GetAsync($"{AuthenticationRoutes.DiscordCallback}?code=test-code&state={Uri.EscapeDataString(state)}");

        callback.Headers.Location.ShouldNotBeNull().OriginalString.ShouldBe(CharacterRoutes.ReviewFor("/"));
        SessionCookieWasIssued(callback).ShouldBeTrue();
    }

    /// <summary>Opens the review page without a session.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task ReviewPageRequiresSignIn()
    {
        await using var site = new WebsiteFactory();
        using var browser = site.CreateBrowser();

        var review = await browser.GetAsync(CharacterRoutes.Review);

        review.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        review.Headers.Location.ShouldNotBeNull().GetLeftPart(UriPartial.Path).ShouldBe("https://discord.com/api/oauth2/authorize");
    }

    /// <summary>Declines consent on Discord.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task DeclinedConsentLeadsToTheRetryPageWithoutASession()
    {
        await using var site = new WebsiteFactory();
        using var browser = site.CreateBrowser();
        var state = await StartSignInAsync(browser, "/");

        var callback = await browser.GetAsync($"{AuthenticationRoutes.DiscordCallback}?error=access_denied&state={Uri.EscapeDataString(state)}");

        callback.Headers.Location.ShouldNotBeNull().OriginalString.ShouldBe(AuthenticationRoutes.SignInFailedFor("denied"));
        SessionCookieWasIssued(callback).ShouldBeFalse();
        (await browser.GetStringAsync(AuthenticationRoutes.SignInFailedFor("denied"))).ShouldContain("Sign-in cancelled");
    }

    /// <summary>Completes a callback while the API is unavailable.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task UnavailableApiLeadsToTheRetryPageWithoutASession()
    {
        await using var site = new WebsiteFactory();
        site.IdentityApi.Fails = true;
        using var browser = site.CreateBrowser();
        var state = await StartSignInAsync(browser, "/");

        var callback = await browser.GetAsync($"{AuthenticationRoutes.DiscordCallback}?code=test-code&state={Uri.EscapeDataString(state)}");

        callback.Headers.Location.ShouldNotBeNull().OriginalString.ShouldBe(AuthenticationRoutes.SignInFailedFor("failed"));
        SessionCookieWasIssued(callback).ShouldBeFalse();
    }

    /// <summary>Tampers with the state parameter, as an expired or forged callback would.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task InvalidStateLeadsToTheRetryPage()
    {
        await using var site = new WebsiteFactory();
        using var browser = site.CreateBrowser();
        await StartSignInAsync(browser, "/");

        var callback = await browser.GetAsync($"{AuthenticationRoutes.DiscordCallback}?code=test-code&state=forged");

        callback.Headers.Location.ShouldNotBeNull().OriginalString.ShouldBe(AuthenticationRoutes.SignInFailedFor("failed"));
        SessionCookieWasIssued(callback).ShouldBeFalse();
    }

    /// <summary>Asks to return to another site after sign-in.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task ForeignReturnUrlIsReplacedByTheHomePage()
    {
        await using var site = new WebsiteFactory();
        using var browser = site.CreateBrowser();
        var state = await StartSignInAsync(browser, "https://evil.example/steal");

        var callback = await browser.GetAsync($"{AuthenticationRoutes.DiscordCallback}?code=test-code&state={Uri.EscapeDataString(state)}");

        callback.Headers.Location.ShouldNotBeNull().OriginalString.ShouldBe("/");
    }

    /// <summary>Signs in, then signs out through the page's form.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task SignOutEndsTheSession()
    {
        await using var site = new WebsiteFactory();
        using var browser = site.CreateBrowser();
        var state = await StartSignInAsync(browser, "/");
        await browser.GetAsync($"{AuthenticationRoutes.DiscordCallback}?code=test-code&state={Uri.EscapeDataString(state)}");
        var token = AntiforgeryTokenPattern().Match(await browser.GetStringAsync("/")).Groups["token"].Value;
        token.ShouldNotBeNullOrEmpty();

        using var form = new FormUrlEncodedContent([new KeyValuePair<string, string>("__RequestVerificationToken", token)]);
        var signOut = await browser.PostAsync(AuthenticationRoutes.SignOut, form);

        signOut.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        (await browser.GetStringAsync("/")).ShouldContain("Sign in with Discord");
    }

    /// <summary>Posts to sign-out without the antiforgery token.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task SignOutWithoutAnAntiforgeryTokenIsRefused()
    {
        await using var site = new WebsiteFactory();
        using var browser = site.CreateBrowser();

        using var form = new FormUrlEncodedContent([]);
        var signOut = await browser.PostAsync(AuthenticationRoutes.SignOut, form);

        signOut.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    /// <summary>Checks that pages render as interactive server components, as ADR-0012 decides.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task PagesRenderInInteractiveServerMode()
    {
        await using var site = new WebsiteFactory();
        using var browser = site.CreateBrowser();

        var home = await browser.GetStringAsync("/");

        home.ShouldContain("<!--Blazor:{\"type\":\"server\"");
    }
    #endregion Tests

    #region Private Helpers
    /// <summary>Starts sign-in and returns the OAuth state Discord would send back.</summary>
    /// <param name="browser">The browser-like client.</param>
    /// <param name="returnUrl">The page to return to.</param>
    /// <returns>The OAuth state value.</returns>
    private static async Task<string> StartSignInAsync(HttpClient browser, string returnUrl)
    {
        var challenge = await browser.GetAsync($"{AuthenticationRoutes.SignIn}?returnUrl={Uri.EscapeDataString(returnUrl)}");
        var location = challenge.Headers.Location.ShouldNotBeNull();
        return QueryHelpers.ParseQuery(location.Query)["state"].ToString();
    }

    /// <summary>Determines whether a response issued the session cookie.</summary>
    /// <param name="response">The response.</param>
    /// <returns><see langword="true"/> when the session cookie was set.</returns>
    private static bool SessionCookieWasIssued(HttpResponseMessage response) =>
        response.Headers.TryGetValues("Set-Cookie", out var cookies)
        && cookies.Any(cookie => cookie.StartsWith(RaidManagerAuthenticationExtensions.SessionCookieName + "=", StringComparison.Ordinal)
            && !cookie.Contains("expires=Thu, 01 Jan 1970", StringComparison.OrdinalIgnoreCase));

    /// <summary>Finds the antiforgery token rendered in the sign-out form.</summary>
    /// <returns>The pattern.</returns>
    [GeneratedRegex("name=\"__RequestVerificationToken\"[^>]*value=\"(?<token>[^\"]+)\"")]
    private static partial Regex AntiforgeryTokenPattern();
    #endregion Private Helpers
}
