using System.Security.Claims;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Radzen;
using Shouldly;
using Xunit;
using RaidManager.ViewModels.Features.Communities;
using RaidManager.Web.Features.Authentication;
using RaidManager.Web.Features.Communities;
using RaidManager.Web.Features.Communities.Pages;
using RaidManager.Web.Tests.Support;

namespace RaidManager.Web.Tests.Features.Communities.Pages;

/// <summary>Verifies the community page of board 4: the community card and the confirmation after linking.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: The page shows the user's community and, right after linking, the confirmation; a user without one goes to the Overview.
/// </remarks>
public sealed class CommunitySettingsTests : BunitContext
{
    #region Fields
    /// <summary>Stores the signed-in user.</summary>
    private readonly Guid _userId = Guid.NewGuid();

    /// <summary>Stores the fake API's community endpoints.</summary>
    private readonly FakeCommunitiesApiClient _communities = new();
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="CommunitySettingsTests"/> class.</summary>
    public CommunitySettingsTests()
    {
        Services.AddRadzenComponents();
        Services.AddSingleton<ICommunitiesApiClient>(_communities);
        Services.AddTransient<CommunityViewModel>();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }
    #endregion Constructors

    #region Tests
    /// <summary>Shows the server, realm and Administrator, and the confirmation right after linking.</summary>
    [Fact]
    public void JustLinkedCommunityShowsItsCardAndTheConfirmation()
    {
        _communities.Communities.Add(FakeCommunitiesApiClient.Community(_userId, "Dark Templars", "Icecrown"));
        SignIn();
        Services.GetRequiredService<NavigationManager>().NavigateTo(CommunityRoutes.SettingsAfterLinking());

        var page = Render<CommunitySettings>();

        page.WaitForAssertion(() => page.Find("h1").TextContent.ShouldBe("Dark Templars"));
        page.Find(".page-heading-eyebrow").TextContent.ShouldBe("Community");
        page.Find(".page-heading-subtitle").TextContent.ShouldBe("Discord server linked to RaidManager on Icecrown.");
        page.FindAll("[data-testid=community-summary] .labeled-value-label").Select(label => label.TextContent)
            .ShouldBe(["Discord server", "Warmane realm", "Administrator"]);
        page.FindAll("[data-testid=community-summary] .labeled-value-value").Select(value => value.TextContent)
            .ShouldBe(["Dark Templars", "Icecrown", "Gihed Annabi"]);
        page.Find("[data-testid=linked-confirmation] .surface-card-title").TextContent.ShouldBe("Dark Templars is linked");
        page.Find("[data-testid=linked-confirmation]").TextContent.ShouldContain("Members see it at their next sign-in.");
    }

    /// <summary>Leaves the confirmation out on a later visit.</summary>
    [Fact]
    public void LaterVisitHasNoConfirmation()
    {
        _communities.Communities.Add(FakeCommunitiesApiClient.Community(_userId));
        SignIn();
        Services.GetRequiredService<NavigationManager>().NavigateTo(CommunityRoutes.Settings);

        var page = Render<CommunitySettings>();

        page.WaitForAssertion(() => page.Find("h1").TextContent.ShouldBe("Dark Templars"));
        page.FindAll("[data-testid=linked-confirmation]").ShouldBeEmpty();
    }

    /// <summary>Sends a user without a community to the Overview.</summary>
    [Fact]
    public void UserWithoutACommunityGoesToTheOverview()
    {
        SignIn();
        var navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo(CommunityRoutes.Settings);

        Render<CommunitySettings>();

        new Uri(navigation.Uri).AbsolutePath.ShouldBe("/");
    }

    /// <summary>Offers a retry when the API can't answer.</summary>
    [Fact]
    public void UnreachableApiOffersARetry()
    {
        _communities.Communities.Add(FakeCommunitiesApiClient.Community(_userId));
        _communities.Fails = true;
        SignIn();
        var page = Render<CommunitySettings>();
        page.WaitForAssertion(() => page.Find("[data-testid=load-failed]").ShouldNotBeNull());

        _communities.Fails = false;
        page.Find("button").Click();

        page.WaitForAssertion(() => page.Find("h1").TextContent.ShouldBe("Dark Templars"));
    }
    #endregion Tests

    #region Private Helpers
    /// <summary>Signs the user in with their RaidManager user id.</summary>
    private void SignIn() =>
        AddAuthorization().SetAuthorized("Gihed Annabi").SetClaims(new Claim(RaidManagerClaimTypes.UserId, _userId.ToString()));
    #endregion Private Helpers
}
