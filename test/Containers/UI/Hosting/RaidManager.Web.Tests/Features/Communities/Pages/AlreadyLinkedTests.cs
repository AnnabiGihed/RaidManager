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

/// <summary>Verifies the already-linked page of board 3.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: The page names the server and its Administrator, offers the Overview, and handles a missing community and an unreachable API.
/// </remarks>
public sealed class AlreadyLinkedTests : BunitContext
{
    #region Fields
    /// <summary>Stores the signed-in user.</summary>
    private readonly Guid _userId = Guid.NewGuid();

    /// <summary>Stores the fake API's community endpoints.</summary>
    private readonly FakeCommunitiesApiClient _communities = new();
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="AlreadyLinkedTests"/> class.</summary>
    public AlreadyLinkedTests()
    {
        Services.AddRadzenComponents();
        Services.AddSingleton<ICommunitiesApiClient>(_communities);
        Services.AddTransient<CommunityViewModel>();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }
    #endregion Constructors

    #region Tests
    /// <summary>Names the server and its Administrator and offers the Overview.</summary>
    [Fact]
    public void AlreadyLinkedServerIsExplained()
    {
        var community = FakeCommunitiesApiClient.Community(Guid.NewGuid());
        _communities.Communities.Add(community);
        SignIn();
        var navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo(CommunityRoutes.AlreadyLinkedFor(community.CommunityId));

        var page = Render<AlreadyLinked>();

        page.WaitForAssertion(() => page.Find("h1").TextContent.ShouldBe("Dark Templars is already linked"));
        page.Find(".page-heading-subtitle").TextContent.ShouldBe("Each Discord server links to one RaidManager community.");
        page.Find("[data-testid=already-linked] .notice-title").TextContent.ShouldBe("You're a member of this community");
        page.Find("[data-testid=already-linked] .notice-message").TextContent.ShouldBe("Its Administrator, Gihed Annabi, manages the realm and the officer roles.");
        page.Find("button").Click();
        new Uri(navigation.Uri).AbsolutePath.ShouldBe("/");
    }

    /// <summary>Goes to the Overview when the address names no community.</summary>
    [Fact]
    public void MissingCommunityGoesToTheOverview()
    {
        SignIn();
        var navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo(CommunityRoutes.AlreadyLinkedFor(Guid.NewGuid()));

        Render<AlreadyLinked>();

        new Uri(navigation.Uri).AbsolutePath.ShouldBe("/");
    }

    /// <summary>Offers a retry when the API can't answer.</summary>
    [Fact]
    public void UnreachableApiOffersARetry()
    {
        var community = FakeCommunitiesApiClient.Community(Guid.NewGuid());
        _communities.Communities.Add(community);
        _communities.Fails = true;
        SignIn();
        Services.GetRequiredService<NavigationManager>().NavigateTo(CommunityRoutes.AlreadyLinkedFor(community.CommunityId));
        var page = Render<AlreadyLinked>();
        page.WaitForAssertion(() => page.Find("[data-testid=load-failed]").ShouldNotBeNull());

        _communities.Fails = false;
        page.Find("button").Click();

        page.WaitForAssertion(() => page.Find("h1").TextContent.ShouldBe("Dark Templars is already linked"));
    }
    #endregion Tests

    #region Private Helpers
    /// <summary>Signs the user in with their RaidManager user id.</summary>
    private void SignIn() =>
        AddAuthorization().SetAuthorized("Gihed Annabi").SetClaims(new Claim(RaidManagerClaimTypes.UserId, _userId.ToString()));
    #endregion Private Helpers
}
