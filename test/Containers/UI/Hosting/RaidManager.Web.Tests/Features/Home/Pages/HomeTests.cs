using System.Security.Claims;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Radzen;
using Shouldly;
using Xunit;
using RaidManager.ViewModels.Features.Communities;
using RaidManager.Web.Features.Authentication;
using RaidManager.Web.Tests.Support;
using HomePage = RaidManager.Web.Features.Home.Pages.Home;

namespace RaidManager.Web.Tests.Features.Home.Pages;

/// <summary>Verifies the home page for visitors and signed-in players.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Visitors get the sign-in card of the sign-in mockup; signed-in players get a welcome.
/// </remarks>
public sealed class HomeTests : BunitContext
{
    #region Fields
    /// <summary>Stores the signed-in player.</summary>
    private readonly Guid _userId = Guid.NewGuid();

    /// <summary>Stores the fake API's community endpoints.</summary>
    private readonly FakeCommunitiesApiClient _communities = new();
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="HomeTests"/> class.</summary>
    public HomeTests()
    {
        Services.AddRadzenComponents();
        Services.AddSingleton<ICommunitiesApiClient>(_communities);
        Services.AddScoped<OverviewViewModel>();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }
    #endregion Constructors

    #region Tests
    /// <summary>Clicks "Sign in with Discord" as a visitor.</summary>
    [Fact]
    public void VisitorCanStartDiscordSignIn()
    {
        AddAuthorization();
        var navigation = Services.GetRequiredService<NavigationManager>();
        var page = Render<HomePage>();

        page.Find("h1").TextContent.ShouldBe("Plan raids with your characters");
        page.Find("[data-testid=sign-in-card] h2").TextContent.ShouldBe("Sign in to continue");
        page.Find("button.action-button-primary").TextContent.ShouldContain("Sign in with Discord");
        page.Find(".text-note").TextContent.ShouldBe("Your first sign-in creates your RaidManager account.");
        page.FindAll("button").First(button => button.TextContent.Contains("Sign in with Discord", StringComparison.Ordinal)).Click();

        navigation.Uri.ShouldEndWith("/sign-in");
    }

    /// <summary>Welcomes a player who has a community.</summary>
    [Fact]
    public void SignedInPlayerWithACommunityIsWelcomed()
    {
        _communities.Communities.Add(FakeCommunitiesApiClient.Community(_userId));
        SignIn();

        var page = Render<HomePage>();

        page.WaitForAssertion(() => page.Find("h1").TextContent.ShouldBe("Welcome, Arthas Menethil"));
        page.Markup.ShouldNotContain("Sign in with Discord");
        page.FindAll("[data-testid=link-steps]").ShouldBeEmpty();
    }

    /// <summary>Shows a player without a community the steps of board 1 and starts adding the bot.</summary>
    [Fact]
    public void PlayerWithoutACommunitySeesHowToLinkOne()
    {
        SignIn();
        var navigation = Services.GetRequiredService<NavigationManager>();

        var page = Render<HomePage>();

        page.WaitForAssertion(() => page.Find("h1").TextContent.ShouldBe("Link your Discord community"));
        page.Find(".page-heading-eyebrow").TextContent.ShouldBe("Get started");
        page.FindAll(".step-list-title").Select(step => step.TextContent).ShouldBe(
            ["Add RaidManager to your Discord server", "Choose your Warmane realm", "Map your officer roles"]);
        page.Find(".text-note").TextContent.ShouldBe(OverviewViewModel.MemberNote);
        page.FindAll("[data-testid=link-failed]").ShouldBeEmpty();
        page.Find("[data-testid=link-steps] button").Click();
        navigation.Uri.ShouldEndWith("/communities/link");
    }

    /// <summary>Explains why adding the bot stopped when the player comes back from it.</summary>
    /// <param name="reason">The reason the return address names.</param>
    /// <param name="title">The expected notice title.</param>
    [Theory]
    [InlineData("cancelled", "RaidManager wasn't added")]
    [InlineData("failed", "RaidManager couldn't finish adding the bot")]
    [InlineData("expired", "That took too long")]
    [InlineData("otheraccount", "Another Discord account added the bot")]
    public void ReturnFromAnUnfinishedLinkExplainsWhy(string reason, string title)
    {
        SignIn();
        Services.GetRequiredService<NavigationManager>().NavigateTo($"/?link={reason}");

        var page = Render<HomePage>();

        page.WaitForAssertion(() => page.Find("[data-testid=link-failed] .notice-title").TextContent.ShouldBe(title));
        page.Find("[data-testid=link-failed]").GetAttribute("role").ShouldBe("alert");
        new Uri(Services.GetRequiredService<NavigationManager>().Uri).PathAndQuery.ShouldBe("/");
    }

    /// <summary>Offers a retry when the API can't answer, and shows the Overview once it does.</summary>
    [Fact]
    public void UnavailableApiOffersARetry()
    {
        _communities.Fails = true;
        SignIn();
        var page = Render<HomePage>();
        page.WaitForAssertion(() => page.Find("[data-testid=load-failed]").TextContent.ShouldContain("The Overview couldn't load"));

        _communities.Fails = false;
        page.FindAll("button").First(button => button.TextContent.Contains("Try again", StringComparison.Ordinal)).Click();

        page.WaitForAssertion(() => page.Find("h1").TextContent.ShouldBe("Link your Discord community"));
    }
    #endregion Tests

    #region Private Helpers
    /// <summary>Signs the player in with their RaidManager user id.</summary>
    private void SignIn() =>
        AddAuthorization().SetAuthorized("Arthas Menethil").SetClaims(new Claim(RaidManagerClaimTypes.UserId, _userId.ToString()));
    #endregion Private Helpers
}
