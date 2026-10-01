using System.Security.Claims;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Radzen;
using Shouldly;
using Xunit;
using RaidManager.ViewModels.Features.Communities;
using RaidManager.Web.Features.Authentication;
using RaidManager.Web.Features.Communities.Pages;
using RaidManager.Web.Tests.Support;

namespace RaidManager.Web.Tests.Features.Communities.Pages;

/// <summary>Verifies the members page of board 5.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: The page lists each member with picture or initials, Discord roles and a role badge, says when Discord was checked, and offers a retry instead of a stale list.
/// </remarks>
public sealed class MembersAndRolesTests : BunitContext
{
    #region Fields
    /// <summary>Stores the signed-in user.</summary>
    private readonly Guid _userId = Guid.NewGuid();

    /// <summary>Stores the fake API's community endpoints.</summary>
    private readonly FakeCommunitiesApiClient _communities = new();
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="MembersAndRolesTests"/> class.</summary>
    public MembersAndRolesTests()
    {
        Services.AddRadzenComponents();
        Services.AddSingleton<ICommunitiesApiClient>(_communities);
        Services.AddSingleton(TimeProvider.System);
        Services.AddTransient<CommunityViewModel>();
        Services.AddTransient<CommunityMembersViewModel>();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }
    #endregion Constructors

    #region Tests
    /// <summary>Lists the members as board 5 shows them.</summary>
    [Fact]
    public void MembersAreListedWithTheirRoles()
    {
        _communities.Communities.Add(FakeCommunitiesApiClient.Community(_userId));
        SignIn();

        var page = Render<MembersAndRoles>();

        page.WaitForAssertion(() => page.FindAll("[data-testid=members-table] tbody tr").Count.ShouldBe(2));
        page.Find("h1").TextContent.ShouldBe("Members and roles");
        page.FindAll("[data-testid=members-table] th").Select(heading => heading.TextContent).ShouldBe(["Member", "Discord roles", "RaidManager role"]);
        var rows = page.FindAll("[data-testid=members-table] tbody tr");
        rows[0].TextContent.ShouldContain("Malarya");
        rows[0].QuerySelector("img.user-avatar")!.GetAttribute("src").ShouldBe("https://cdn.discordapp.com/avatars/1/a.png");
        rows[0].QuerySelector(".member-discord-roles")!.TextContent.ShouldBe("@Officier, @Veteran");
        rows[0].QuerySelector(".tag-chip")!.ClassList.ShouldContain("tag-chip-success");
        rows[1].QuerySelector(".user-avatar-initials")!.TextContent.ShouldBe("O");
        rows[1].QuerySelector(".member-discord-roles")!.TextContent.ShouldBe("No roles");
        rows[1].QuerySelector(".tag-chip")!.ClassList.ShouldContain("tag-chip-neutral");
        page.Find(".text-note").TextContent.ShouldStartWith("Last checked with Discord today at ");
    }

    /// <summary>Offers a retry when Discord can't answer, and lists the members once it does.</summary>
    [Fact]
    public void UnavailableDiscordOffersARetry()
    {
        _communities.Communities.Add(FakeCommunitiesApiClient.Community(_userId));
        _communities.RoleStatus = CommunityApiStatus.DiscordUnavailable;
        SignIn();
        var page = Render<MembersAndRoles>();
        page.WaitForAssertion(() => page.Find("[data-testid=members-problem] .notice-title").TextContent.ShouldBe("The members couldn't be shown"));
        page.FindAll("[data-testid=members-table]").ShouldBeEmpty();

        _communities.RoleStatus = CommunityApiStatus.Succeeded;
        page.Find("button").Click();

        page.WaitForAssertion(() => page.FindAll("[data-testid=members-table] tbody tr").Count.ShouldBe(2));
    }

    /// <summary>Offers a retry when the API can't read the community, and lists the members once it can.</summary>
    [Fact]
    public void UnreachableApiOffersARetry()
    {
        _communities.Communities.Add(FakeCommunitiesApiClient.Community(_userId));
        _communities.Fails = true;
        SignIn();
        var page = Render<MembersAndRoles>();
        page.WaitForAssertion(() => page.Find("[data-testid=members-problem] .notice-title").TextContent.ShouldBe("The members couldn't be shown"));

        _communities.Fails = false;
        page.Find("button").Click();

        page.WaitForAssertion(() => page.FindAll("[data-testid=members-table] tbody tr").Count.ShouldBe(2));
    }

    /// <summary>Sends a user without a community to the Overview.</summary>
    [Fact]
    public void UserWithoutACommunityGoesToTheOverview()
    {
        SignIn();
        var navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo("/community/members");

        Render<MembersAndRoles>();

        new Uri(navigation.Uri).AbsolutePath.ShouldBe("/");
    }
    #endregion Tests

    #region Private Helpers
    /// <summary>Signs the user in with their RaidManager user id.</summary>
    private void SignIn() =>
        AddAuthorization().SetAuthorized("Gihed Annabi").SetClaims(new Claim(RaidManagerClaimTypes.UserId, _userId.ToString()));
    #endregion Private Helpers
}
