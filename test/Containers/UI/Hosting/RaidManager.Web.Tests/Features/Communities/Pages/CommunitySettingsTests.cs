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
using RaidManager.Web.Features.Shared.Components;
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

    /// <summary>Stores the layout's notification area, once a test looks at it.</summary>
    private IRenderedComponent<NotificationArea>? _notifications;
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="CommunitySettingsTests"/> class.</summary>
    public CommunitySettingsTests()
    {
        Services.AddRadzenComponents();
        Services.AddSingleton<ICommunitiesApiClient>(_communities);
        Services.AddTransient<CommunityViewModel>();
        Services.AddTransient<CommunityRolesViewModel>();
        Services.AddSingleton(TimeProvider.System);
        JSInterop.Mode = JSRuntimeMode.Loose;
    }
    #endregion Constructors

    #region Properties
    /// <summary>Gets the layout's notification area, where the page's notifications show (#577).</summary>
    private IRenderedComponent<NotificationArea> Notifications => _notifications ??= Render<NotificationArea>();
    #endregion Properties

    #region Tests
    /// <summary>Shows the server, realm and Administrator in the heading, and the confirmation right after linking.</summary>
    [Fact]
    public void JustLinkedCommunityShowsItsCardAndTheConfirmation()
    {
        _communities.Communities.Add(FakeCommunitiesApiClient.Community(_userId, "Dark Templars", "Icecrown"));
        SignIn();
        Services.GetRequiredService<NavigationManager>().NavigateTo(CommunityRoutes.SettingsAfterLinking());

        var page = Render<CommunitySettings>();

        page.WaitForAssertion(() => page.Find("h1").TextContent.ShouldBe("Dark Templars"));
        page.Find(".page-heading-eyebrow").TextContent.ShouldBe("Community");
        page.Find(".page-heading-subtitle").TextContent.ShouldBe("Discord server linked to RaidManager on Icecrown. Administrator: Gihed Annabi.");
        Notifications.Find("[data-testid=linked-confirmation] .toast-title").TextContent.ShouldBe("Dark Templars is linked");
        Notifications.Find("[data-testid=linked-confirmation] .toast-message").TextContent.ShouldBe("Members see it at their next sign-in.");
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
        Notifications.FindAll("[data-testid=linked-confirmation]").ShouldBeEmpty();
    }

    /// <summary>Closing the confirmation of linking keeps it closed (#577).</summary>
    [Fact]
    public void ClosingTheLinkedConfirmationKeepsItClosed()
    {
        _communities.Communities.Add(FakeCommunitiesApiClient.Community(_userId));
        SignIn();
        Services.GetRequiredService<NavigationManager>().NavigateTo(CommunityRoutes.SettingsAfterLinking());
        var page = Render<CommunitySettings>();
        page.WaitForAssertion(() => page.Find("h1").TextContent.ShouldBe("Dark Templars"));

        Notifications.Find("[data-testid=linked-confirmation] .toast-close").Click();
        page.Render();

        Notifications.FindAll("[data-testid=linked-confirmation]").ShouldBeEmpty();
    }

    /// <summary>Shows the Administrator each role with its chips and counts, and the controls of board 8.</summary>
    [Fact]
    public void AdministratorSeesTheRolesCardWithItsControls()
    {
        var page = OpenAdministratorPage();

        page.FindAll("[data-testid^=role-row-] .role-row-label").Select(label => label.TextContent).ShouldBe(["Administrator", "Officer", "Raid leader", "Member"]);
        page.FindAll("[data-testid^=role-row-] .role-row-members").Select(count => count.TextContent).ShouldBe(["1 member", "1 member", "0 members", "2 members"]);
        page.Find("[data-testid=role-row-Administrator] .role-row-source").TextContent.ShouldBe("Added RaidManager to the server; allows everything");
        page.Find($"[data-testid=role-row-{FakeCommunitiesApiClient.OfficerId}] .role-row-allows").TextContent.ShouldBe("Allows raids, rosters, raid night, conflicts");
        page.FindAll("[data-testid=create-role]").ShouldHaveSingleItem();
        page.FindAll("[data-testid=locked-notice]").ShouldBeEmpty();
        page.Find($"[data-testid=role-row-{FakeCommunitiesApiClient.OfficerId}] .tag-chip-text").TextContent.ShouldBe("@Officier");
        page.Find($"[data-testid=role-row-{FakeCommunitiesApiClient.OfficerId}] .tag-chip-remove").GetAttribute("aria-label").ShouldBe("Remove @Officier");
        page.Find($"[data-testid=role-row-{FakeCommunitiesApiClient.RaidLeaderId}] .tag-chip").ClassList.ShouldContain("tag-chip-danger");
        page.Find($"[data-testid=role-row-{FakeCommunitiesApiClient.RaidLeaderId}] .tag-chip-text").TextContent.ShouldBe("Deleted role");
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
        page.FindAll($"[data-testid=role-row-{FakeCommunitiesApiClient.OfficerId}] button").First(button => button.TextContent.Contains("+ Add Discord role", StringComparison.Ordinal)).Click();

        var picker = page.Find("[data-testid=role-picker] select");
        picker.GetAttribute("aria-label").ShouldBe("Discord role for Officer");
        page.FindAll("[data-testid=role-picker] option").Select(option => option.TextContent).ShouldBe(["@Guild Master", "@Veteran"]);
        picker.Change("1");
        page.FindAll("[data-testid=role-picker] button").First(button => button.TextContent.Contains("Add", StringComparison.Ordinal)).Click();

        _communities.RoleChanges.ShouldHaveSingleItem().ShouldBe(("13", FakeCommunitiesApiClient.OfficerId, true));
        Notifications.WaitForAssertion(() => Notifications.Find("[data-testid=saved-confirmation] .toast-title").TextContent.ShouldBe("Roles saved"));
        page.FindAll("[data-testid=role-picker]").ShouldBeEmpty();
    }

    /// <summary>Closing the confirmation of a change forgets it (#577).</summary>
    [Fact]
    public void ClosingTheSavedConfirmationForgetsIt()
    {
        var page = OpenAdministratorPage();
        page.FindAll($"[data-testid=role-row-{FakeCommunitiesApiClient.OfficerId}] button").First(button => button.TextContent.Contains("+ Add Discord role", StringComparison.Ordinal)).Click();
        page.FindAll("[data-testid=role-picker] button").First(button => button.TextContent.Contains("Add", StringComparison.Ordinal)).Click();

        Notifications.WaitForElement("[data-testid=saved-confirmation] .toast-close").Click();

        Notifications.FindAll("[data-testid=saved-confirmation]").ShouldBeEmpty();
    }

    /// <summary>Closes the picker without changing anything.</summary>
    [Fact]
    public void CancellingThePickerChangesNothing()
    {
        var page = OpenAdministratorPage();
        page.FindAll($"[data-testid=role-row-{FakeCommunitiesApiClient.RaidLeaderId}] button").First(button => button.TextContent.Contains("+ Add Discord role", StringComparison.Ordinal)).Click();

        page.FindAll("[data-testid=role-picker] button").First(button => button.TextContent.Contains("Cancel", StringComparison.Ordinal)).Click();

        page.FindAll("[data-testid=role-picker]").ShouldBeEmpty();
        _communities.RoleChanges.ShouldBeEmpty();
    }

    /// <summary>Removes a mapping with its ×.</summary>
    [Fact]
    public void RemovingAChipRemovesTheMapping()
    {
        var page = OpenAdministratorPage();

        page.Find($"[data-testid=role-row-{FakeCommunitiesApiClient.OfficerId}] .tag-chip-remove").Click();

        _communities.RoleChanges.ShouldHaveSingleItem().ShouldBe(("12", FakeCommunitiesApiClient.OfficerId, false));
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

        page.WaitForAssertion(() => page.Find($"[data-testid=role-row-{FakeCommunitiesApiClient.OfficerId}] .tag-chip-text").TextContent.ShouldBe("@Officier"));
        page.FindAll(".tag-chip-remove").ShouldBeEmpty();
        page.Markup.ShouldNotContain("+ Add Discord role");
    }

    /// <summary>Explains a card Discord couldn't give, and shows it after a retry.</summary>
    [Fact]
    public void AnUnreadableCardOffersARetry()
    {
        _communities.RoleStatus = ViewModels.Features.Communities.CommunityApiStatus.DiscordUnavailable;
        var page = OpenAdministratorPage(waitForRows: false);
        page.WaitForAssertion(() => page.Find("[data-testid=roles-problem] .notice-title").TextContent.ShouldBe("The roles couldn't be shown"));

        _communities.RoleStatus = ViewModels.Features.Communities.CommunityApiStatus.Succeeded;
        page.Find("[data-testid=roles-card] button").Click();

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

    /// <summary>Creates a role from the dialog: a name and a permission, then the confirmation.</summary>
    [Fact]
    public void CreatingARoleSavesItAndConfirms()
    {
        var page = OpenAdministratorPage();

        page.Find("[data-testid=create-role]").Click();
        page.Find("[data-testid=role-form] h2").TextContent.ShouldBe("Create a role");
        page.Find("[data-testid=role-name]").Input("Veteran");
        page.Find("[data-testid=permission-RunRaidNight] input").Change(true);
        page.Find("[data-testid=save-role]").Click();

        Notifications.WaitForAssertion(() => Notifications.Find("[data-testid=saved-confirmation] .toast-title").TextContent.ShouldBe("Roles saved"));
        _communities.RoleWrites.ShouldHaveSingleItem().ShouldSatisfyAllConditions(
            write => write.Action.ShouldBe("create"),
            write => write.Name.ShouldBe("Veteran"),
            write => write.Permissions.ShouldNotBeNull().ShouldBe(["RunRaidNight"]));
        page.FindAll("[data-testid=role-form]").ShouldBeEmpty();
    }

    /// <summary>Keeps the dialog open with the reason when another role has the name.</summary>
    [Fact]
    public void ATakenNameIsExplainedInTheDialog()
    {
        var page = OpenAdministratorPage();
        _communities.RoleWriteStatus = ViewModels.Features.Communities.CommunityApiStatus.NameTaken;

        page.Find("[data-testid=create-role]").Click();
        page.Find("[data-testid=role-name]").Input("Officer");
        page.Find("[data-testid=save-role]").Click();

        page.WaitForAssertion(() => page.Find("[data-testid=role-form-problem] .notice-message").TextContent.ShouldBe("Another role already has this name."));
        page.FindAll("button").First(button => button.TextContent.Trim() == "Cancel").Click();
        page.FindAll("[data-testid=role-form]").ShouldBeEmpty();
    }

    /// <summary>Edits a role, then deletes it after the confirmation.</summary>
    [Fact]
    public void EditingThenDeletingARole()
    {
        var page = OpenAdministratorPage();

        page.Find($"[data-testid=role-row-{FakeCommunitiesApiClient.RaidLeaderId}] [aria-label='Edit Raid leader']").Click();
        page.Find("[data-testid=role-form] h2").TextContent.ShouldBe("Edit Raid leader");
        page.Find("[data-testid=role-name]").GetAttribute("value").ShouldBe("Raid leader");
        page.Find("[data-testid=permission-ManageRaids] input").HasAttribute("checked").ShouldBeTrue();
        page.Find("[data-testid=delete-role]").Click();
        var dialog = page.Find("[data-testid=delete-dialog]");
        dialog.QuerySelector("h2")!.TextContent.ShouldBe("Delete Raid leader?");
        page.FindAll("[data-testid=delete-dialog] button").First(button => button.TextContent.Trim() == "Delete role").Click();

        page.WaitForAssertion(() => _communities.RoleWrites.ShouldHaveSingleItem().ShouldBe(("delete", FakeCommunitiesApiClient.RaidLeaderId, null, null)));
        page.WaitForAssertion(() => page.FindAll("[data-testid=role-form]").ShouldBeEmpty());
    }

    /// <summary>Shows a role manager the locked role and keeps Manage community roles out of their reach.</summary>
    [Fact]
    public void ARoleManagerSeesLockedRoles()
    {
        var community = FakeCommunitiesApiClient.Community(_userId);
        _communities.Communities.Add(community);
        var card = FakeCommunitiesApiClient.Card(community.CommunityId, canEdit: true) with { CanGrantRoleManagement = false };
        _communities.RoleSettings[community.CommunityId] = card with { Rows = [.. card.Rows.Select(row => row.RoleId == FakeCommunitiesApiClient.RaidLeaderId ? row with { CanChange = false } : row)] };
        SignIn();
        var page = Render<CommunitySettings>();

        page.WaitForAssertion(() => page.Find("[data-testid=locked-notice] .notice-title").TextContent.ShouldBe("Some roles are locked"));
        page.Find($"[data-testid=role-row-{FakeCommunitiesApiClient.RaidLeaderId}] .role-row-locked").TextContent.ShouldBe("Locked");
        page.FindAll($"[data-testid=role-row-{FakeCommunitiesApiClient.RaidLeaderId}] [aria-label='Edit Raid leader']").ShouldBeEmpty();
        page.Find("[data-testid=create-role]").Click();
        page.Find("[data-testid=permission-ManageCommunityRoles] input").HasAttribute("disabled").ShouldBeTrue();
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
