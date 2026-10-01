using System.Net;
using Microsoft.AspNetCore.WebUtilities;
using Shouldly;
using Xunit;
using RaidManager.Web.Features.Authentication;
using RaidManager.Web.Features.Communities;
using RaidManager.Web.Tests.Support;

namespace RaidManager.Web.Tests.Features.Communities;

/// <summary>Verifies the way out to Discord and the way back when the bot is added, through the real website.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Each outcome of Discord's return, as Discord sent it on 2026-10-01 (#288): the realm choice, an already
/// linked server, a cancel, an expired or forged state, a failed exchange, another account, and an unreachable API.
/// </remarks>
public sealed class CommunityLinkEndpointsTests
{
    #region Tests
    /// <summary>Sends a signed-in user to Discord's page for adding the bot, with the parameters Discord was checked with.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task StartSendsTheUserToDiscordWithAStateCookie()
    {
        await using var site = new WebsiteFactory();
        using var browser = await SignedInBrowserAsync(site);

        var start = await browser.GetAsync(CommunityRoutes.AddBot);

        start.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        var location = start.Headers.Location.ShouldNotBeNull();
        location.GetLeftPart(UriPartial.Path).ShouldBe("https://discord.com/oauth2/authorize");
        var query = QueryHelpers.ParseQuery(location.Query);
        query["client_id"].ToString().ShouldBe(WebsiteFactory.ClientId);
        query["scope"].ToString().ShouldBe("bot identify");
        query["permissions"].ToString().ShouldBe("0");
        query["response_type"].ToString().ShouldBe("code");
        query["redirect_uri"].ToString().ShouldBe("https://localhost/communities/link/discord");
        query["integration_type"].ToString().ShouldBe("0");
        query["state"].ToString().ShouldNotBeNullOrWhiteSpace();
        start.Headers.GetValues("Set-Cookie").ShouldContain(cookie =>
            cookie.StartsWith(CommunityLinkEndpoints.StateCookieName + "=", StringComparison.Ordinal)
            && cookie.Contains("httponly", StringComparison.OrdinalIgnoreCase)
            && cookie.Contains("secure", StringComparison.OrdinalIgnoreCase)
            && cookie.Contains("samesite=lax", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Sends a visitor to Discord sign-in first, which then returns to adding the bot.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task VisitorIsAskedToSignInFirst()
    {
        await using var site = new WebsiteFactory();
        using var browser = site.CreateBrowser();

        var start = await browser.GetAsync(CommunityRoutes.AddBot);

        start.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        var signIn = Uri.UnescapeDataString(start.Headers.Location.ShouldNotBeNull().OriginalString);
        signIn.ShouldContain("redirect_uri=https://localhost/signin-discord");
        signIn.ShouldContain("scope=identify");
    }

    /// <summary>Continues a new server to the realm choice with what Discord confirmed.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task NewServerContinuesToTheRealmChoice()
    {
        await using var site = new WebsiteFactory();
        using var browser = await SignedInBrowserAsync(site);
        var state = await StartAsync(browser);

        var back = await browser.GetAsync(ReturnAddress(state, guildId: FakeDiscordInstallClient.GuildId));

        var location = back.Headers.Location.ShouldNotBeNull().OriginalString;
        location.ShouldStartWith($"{CommunityRoutes.ChooseRealm}?link=");
        site.DiscordInstall.Exchanges.ShouldHaveSingleItem().ShouldBe(("discord-code", new Uri("https://localhost/communities/link/discord")));
        back.Headers.GetValues("Set-Cookie").ShouldContain(cookie =>
            cookie.StartsWith(CommunityLinkEndpoints.StateCookieName + "=;", StringComparison.Ordinal));
    }

    /// <summary>Shows an already linked server's community instead of linking it again.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task AlreadyLinkedServerOpensItsCommunity()
    {
        await using var site = new WebsiteFactory();
        var existing = FakeCommunitiesApiClient.Community(Guid.NewGuid()) with { DiscordGuildId = FakeDiscordInstallClient.GuildId };
        site.CommunitiesApi.Communities.Add(existing);
        using var browser = await SignedInBrowserAsync(site);
        var state = await StartAsync(browser);

        var back = await browser.GetAsync(ReturnAddress(state));

        back.Headers.Location.ShouldNotBeNull().OriginalString.ShouldBe(CommunityRoutes.AlreadyLinkedFor(existing.CommunityId));
    }

    /// <summary>Explains each way the return stops, and links nothing.</summary>
    /// <param name="variant">What goes wrong.</param>
    /// <param name="reason">The reason the Overview is given.</param>
    /// <returns>A task that completes when the test has run.</returns>
    [Theory]
    [InlineData("cancelled on Discord", "cancelled")]
    [InlineData("another Discord error", "failed")]
    [InlineData("no code", "failed")]
    [InlineData("forged state", "expired")]
    [InlineData("exchange refused", "failed")]
    [InlineData("other server in the address", "failed")]
    [InlineData("other Discord account", "otheraccount")]
    [InlineData("API unreachable", "failed")]
    public async Task UnfinishedReturnExplainsWhy(string variant, string reason)
    {
        await using var site = new WebsiteFactory();
        using var browser = await SignedInBrowserAsync(site);
        var state = await StartAsync(browser);
        var address = variant switch
        {
            "cancelled on Discord" => $"{CommunityRoutes.DiscordReturn}?error=access_denied&error_description=cancelled&state={Uri.EscapeDataString(state)}",
            "another Discord error" => $"{CommunityRoutes.DiscordReturn}?error=server_error&state={Uri.EscapeDataString(state)}",
            "no code" => $"{CommunityRoutes.DiscordReturn}?state={Uri.EscapeDataString(state)}",
            "forged state" => ReturnAddress("forged-state"),
            "other server in the address" => ReturnAddress(state, guildId: "111"),
            _ => ReturnAddress(state),
        };
        site.DiscordInstall.Install = variant switch
        {
            "exchange refused" => null,
            "other Discord account" => site.DiscordInstall.Install! with { InstallerDiscordUserId = "222" },
            _ => site.DiscordInstall.Install,
        };
        site.CommunitiesApi.Fails = variant == "API unreachable";

        var back = await browser.GetAsync(address);

        back.Headers.Location.ShouldNotBeNull().OriginalString.ShouldBe($"/?link={reason}");
        site.CommunitiesApi.Links.ShouldBeEmpty();
    }

    /// <summary>Refuses a return that comes without the state cookie, such as one opened in another browser.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task ReturnWithoutTheStateCookieHasExpired()
    {
        await using var site = new WebsiteFactory();
        using var browser = await SignedInBrowserAsync(site);

        var back = await browser.GetAsync(ReturnAddress("state-from-elsewhere"));

        back.Headers.Location.ShouldNotBeNull().OriginalString.ShouldBe("/?link=expired");
        site.DiscordInstall.Exchanges.ShouldBeEmpty();
    }
    #endregion Tests

    #region Private Helpers
    /// <summary>Builds Discord's return address with a code.</summary>
    /// <param name="state">The returned state.</param>
    /// <param name="guildId">The server in the address, if any.</param>
    /// <returns>The address.</returns>
    private static string ReturnAddress(string state, string? guildId = null) =>
        $"{CommunityRoutes.DiscordReturn}?code=discord-code&state={Uri.EscapeDataString(state)}&permissions=0"
        + (guildId is null ? string.Empty : $"&guild_id={guildId}");

    /// <summary>Starts adding the bot and returns the state sent to Discord.</summary>
    /// <param name="browser">The signed-in browser.</param>
    /// <returns>The state.</returns>
    private static async Task<string> StartAsync(HttpClient browser)
    {
        var start = await browser.GetAsync(CommunityRoutes.AddBot);
        return QueryHelpers.ParseQuery(start.Headers.Location.ShouldNotBeNull().Query)["state"].ToString();
    }

    /// <summary>Signs a player in through the real Discord sign-in flow, with Discord stubbed.</summary>
    /// <param name="site">The website.</param>
    /// <returns>A browser holding the session cookie.</returns>
    private static async Task<HttpClient> SignedInBrowserAsync(WebsiteFactory site)
    {
        var browser = site.CreateBrowser();
        var challenge = await browser.GetAsync($"{AuthenticationRoutes.SignIn}?returnUrl=%2F");
        var state = QueryHelpers.ParseQuery(challenge.Headers.Location.ShouldNotBeNull().Query)["state"].ToString();
        var callback = await browser.GetAsync($"{AuthenticationRoutes.DiscordCallback}?code=test-code&state={Uri.EscapeDataString(state)}");
        callback.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        return browser;
    }
    #endregion Private Helpers
}
