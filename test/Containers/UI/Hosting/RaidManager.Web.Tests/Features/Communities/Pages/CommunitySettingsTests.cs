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
        Services.AddTransient<CommunityRolesViewModel>();
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
        page.Find("[data-testid=linked-confirmation] .toast-title").TextContent.ShouldBe("Dark Templars is linked");
        page.Find("[data-testid=linked-confirmation] .toast-message").TextContent.ShouldBe("Members see it at their next sign-in.");
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

    /// <summary>Shows the Administrator each role with its chips and counts, and the controls of board 8.</summary>
    [Fact]
    public void AdministratorSeesTheRolesCardWithItsControls()
    {
        var page = OpenAdministratorPage();

        page.FindAll("[data-testid^=role-row-] .role-row-label").Select(label => label.TextContent).ShouldBe(["Administrator", "Officer", "Raid leader", "Member"]);
        page.FindAll("[data-testid^=role-row-] .role-row-members").Select(count => count.TextContent).ShouldBe(["1 member", "1 member", "0 members", "2 members"]);
        page.Find("[data-testid=role-row-Administrator] .role-row-source").TextContent.ShouldBe("Added RaidManager to the server");
        page.Find("[data-testid=role-row-Officer] .tag-chip-text").TextContent.ShouldBe("@Officier");
        page.Find("[data-testid=role-row-Officer] .tag-chip-remove").GetAttribute("aria-label").ShouldBe("Remove @Officier");
        page.Find("[data-testid=role-row-RaidLeader] .tag-chip").ClassList.ShouldContain("tag-chip-danger");
        page.Find("[data-testid=role-row-RaidLeader] .tag-chip-text").TextContent.ShouldBe("Deleted role");
        page.FindAll("button").Count(button => button.TextContent.Contains("+ Add Discord role", StringComparison.Ordinal)).ShouldBe(2);
    }

    /// <summary>Opens the members page from the roles card.</summary>
    [Fact]
    public void ViewMembersOpensTheMembersPage()
    {
        var page = OpenAdministratorPage();
        var navigation = Services.GetRequiredService<NavigationManager>();

        page.FindAll("button").First(button => button.TextContent.Contains("View members", StringComparison.Ordinal)).Click();

        navigation.Uri.ShouldEndWith("/community/members");
    }

    /// <summary>Picks a Discord role for Officer, adds it, and confirms the change.</summary>
    [Fact]
    public void AddingARoleSavesItAndConfirms()
    {
        var page = OpenAdministratorPage();
        page.FindAll("[data-testid=role-row-Officer] button").First(button => button.TextContent.Contains("+ Add Discord role", StringComparison.Ordinal)).Click();

        var picker = page.Find("[data-testid=role-picker] select");
        picker.GetAttribute("aria-label").ShouldBe("Discord role for Officer");
        page.FindAll("[data-testid=role-picker] option").Select(option => option.TextContent).ShouldBe(["@Guild Master", "@Veteran"]);
        picker.Change("1");
        page.FindAll("[data-testid=role-picker] button").First(button => button.TextContent.Contains("Add", StringComparison.Ordinal)).Click();

        _communities.RoleChanges.ShouldHaveSingleItem().ShouldBe(("13", "Officer", true));
        page.WaitForAssertion(() => page.Find("[data-testid=saved-confirmation] .toast-title").TextContent.ShouldBe("Officer roles saved"));
        page.FindAll("[data-testid=role-picker]").ShouldBeEmpty();
    }

    /// <summary>Closes the picker without changing anything.</summary>
    [Fact]
    public void CancellingThePickerChangesNothing()
    {
        var page = OpenAdministratorPage();
        page.FindAll("[data-testid=role-row-RaidLeader] button").First(button => button.TextContent.Contains("+ Add Discord role", StringComparison.Ordinal)).Click();

        page.FindAll("[data-testid=role-picker] button").First(button => button.TextContent.Contains("Cancel", StringComparison.Ordinal)).Click();

        page.FindAll("[data-testid=role-picker]").ShouldBeEmpty();
        _communities.RoleChanges.ShouldBeEmpty();
    }

    /// <summary>Removes a mapping with its ×.</summary>
    [Fact]
    public void RemovingAChipRemovesTheMapping()
    {
        var page = OpenAdministratorPage();

        page.Find("[data-testid=role-row-Officer] .tag-chip-remove").Click();

        _communities.RoleChanges.ShouldHaveSingleItem().ShouldBe(("12", "Officer", false));
    }

    /// <summary>Shows another member the card without any control to change it.</summary>
    [Fact]
    public void OtherMembersSeeTheCardReadOnly()
    {
        var community = FakeCommunitiesApiClient.Community(_userId);
        _communities.Communities.Add(community);
        _communities.RoleSettings[community.CommunityId] = FakeCommunitiesApiClient.Card(community.CommunityId, canEdit: false);
        SignIn();

        var page = Render<CommunitySettings>();

        page.WaitForAssertion(() => page.Find("[data-testid=role-row-Officer] .tag-chip-text").TextContent.ShouldBe("@Officier"));
        page.FindAll(".tag-chip-remove").ShouldBeEmpty();
        page.Markup.ShouldNotContain("+ Add Discord role");
    }

    /// <summary>Explains a card Discord couldn't give, and shows it after a retry.</summary>
    [Fact]
    public void AnUnreadableCardOffersARetry()
    {
        _communities.RoleStatus = ViewModels.Features.Communities.CommunityApiStatus.DiscordUnavailable;
        var page = OpenAdministratorPage(waitForRows: false);
        page.WaitForAssertion(() => page.Find("[data-testid=roles-problem] .notice-title").TextContent.ShouldBe("The officer roles couldn't be shown"));

        _communities.RoleStatus = ViewModels.Features.Communities.CommunityApiStatus.Succeeded;
        page.Find("[data-testid=officer-roles] button").Click();

        page.WaitForAssertion(() => page.FindAll("[data-testid^=role-row-]").Count.ShouldBe(4));
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
    /// <summary>Opens the community page as its Administrator and waits for the roles card.</summary>
    /// <param name="waitForRows">Whether to wait until the roles card shows its rows.</param>
    /// <returns>The rendered page.</returns>
    private IRenderedComponent<CommunitySettings> OpenAdministratorPage(bool waitForRows = true)
    {
        _communities.Communities.Add(FakeCommunitiesApiClient.Community(_userId));
        SignIn();
        var page = Render<CommunitySettings>();
        if (waitForRows)
        {
            page.WaitForAssertion(() => page.FindAll("[data-testid^=role-row-]").Count.ShouldBe(4));
        }

        return page;
    }

    /// <summary>Signs the user in with their RaidManager user id.</summary>
    private void SignIn() =>
        AddAuthorization().SetAuthorized("Gihed Annabi").SetClaims(new Claim(RaidManagerClaimTypes.UserId, _userId.ToString()));
    #endregion Private Helpers
}
