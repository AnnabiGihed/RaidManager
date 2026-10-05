using System.Security.Claims;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Radzen;
using Shouldly;
using Xunit;
using RaidManager.ViewModels.Features.Communities;
using RaidManager.ViewModels.Features.Shared.Shell;
using RaidManager.Web.Features.Authentication;
using RaidManager.Web.Features.Shared.Layout;
using RaidManager.Web.Tests.Support;

namespace RaidManager.Web.Tests.Features.Shared.Layout;

/// <summary>Verifies the app shell signed-in players see and the public frame visitors see.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Covers ADR-0019's shell: sidebar navigation of existing pages, the community and user cards, the breadcrumb
/// and Sign out; and the shell-less frame of signed-out pages.
/// </remarks>
public sealed class MainLayoutTests : BunitContext
{
    #region Fields
    /// <summary>Stores the fake API's community endpoints.</summary>
    private readonly FakeCommunitiesApiClient _communities = new();
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="MainLayoutTests"/> class.</summary>
    public MainLayoutTests()
    {
        Services.AddRadzenComponents();
        Services.AddShellNavigation();
        Services.AddSingleton<ICommunitiesApiClient>(_communities);
        Services.AddScoped<ShellCommunityViewModel>();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }
    #endregion Constructors

    #region Tests
    /// <summary>Renders the layout for an anonymous visitor.</summary>
    [Fact]
    public void VisitorSeesThePublicFrameWithoutTheShell()
    {
        AddAuthorization();

        var layout = RenderLayout();

        layout.Find("[data-testid=public-frame]").TextContent.ShouldContain("page");
        layout.FindAll(".rz-sidebar").ShouldBeEmpty();
        layout.Markup.ShouldNotContain("Sign out");
    }

    /// <summary>Renders the shell for a signed-in player.</summary>
    [Fact]
    public void SignedInPlayerSeesTheShell()
    {
        AddAuthorization().SetAuthorized("Arthas Menethil");

        var layout = RenderLayout();

        var userCard = layout.Find("[data-testid=user-card]");
        userCard.QuerySelector(".summary-tile-title").ShouldNotBeNull().TextContent.ShouldBe("Arthas Menethil");
        userCard.TextContent.ShouldContain("Player");
        layout.Find("[data-testid=community-card]").TextContent.ShouldContain("No community yet");
        layout.Find("[data-testid=community-card] .summary-tile-chevron").ShouldNotBeNull();
        layout.Find("form[action='/sign-out']").ShouldNotBeNull();
        layout.FindAll(".user-avatar-initials").Select(avatar => avatar.TextContent).ShouldAllBe(initials => initials == "AM");
        layout.FindAll("[data-testid=public-frame]").ShouldBeEmpty();
    }

    /// <summary>Shows the Administrator's community in the card, leading to the community page, and their role.</summary>
    [Fact]
    public void AdministratorSeesTheirCommunityAndRole()
    {
        var userId = Guid.NewGuid();
        _communities.Communities.Add(FakeCommunitiesApiClient.Community(userId, "Dark Templars", "Lordaeron"));
        AddAuthorization().SetAuthorized("Gihed Annabi").SetClaims(new Claim(RaidManagerClaimTypes.UserId, userId.ToString()));

        var layout = RenderLayout();

        layout.WaitForAssertion(() => layout.Find("[data-testid=community-card] .summary-tile-title").TextContent.ShouldBe("Dark Templars"));
        layout.Find("[data-testid=community-card]").TextContent.ShouldContain("Lordaeron · Community");
        layout.Find("[data-testid=community-card] .icon-tile-community").TextContent.ShouldBe("DT");
        layout.Find("a.shell-community-link").GetAttribute("href").ShouldBe("/community");
        layout.Find("[data-testid=user-card]").TextContent.ShouldContain("Administrator");
    }

    /// <summary>Leads a user without a community to the Overview, which explains how to link one.</summary>
    [Fact]
    public void UserWithoutACommunityIsLedToTheOverview()
    {
        AddAuthorization().SetAuthorized("Arthas Menethil").SetClaims(new Claim(RaidManagerClaimTypes.UserId, Guid.NewGuid().ToString()));

        var layout = RenderLayout();

        layout.WaitForAssertion(() => layout.Find("a.shell-community-link").GetAttribute("href").ShouldBe("/"));
        layout.Find("[data-testid=community-card]").TextContent.ShouldContain("Link a Discord server");
    }

    /// <summary>Lists only existing pages and highlights the current one.</summary>
    [Fact]
    public void NavigationListsThePlayerPagesAndHighlightsTheCurrentOne()
    {
        AddAuthorization().SetAuthorized("Arthas Menethil");

        var layout = RenderLayout();

        layout.Find(".shell-navigation").TextContent.ShouldContain("PLAYER");
        layout.FindAll(".shell-navigation a").Select(link => link.GetAttribute("href")).ShouldBe(["/", "/characters", "/companion"]);
        layout.Markup.ShouldNotContain("OFFICER");
        layout.Find(".rz-navigation-item-wrapper-active").TextContent.ShouldContain("Overview");
        layout.Find("[data-testid=breadcrumb-page]").TextContent.ShouldBe("Overview");
    }

    /// <summary>Highlights My characters on a profile under it and names it in the breadcrumb (#385).</summary>
    [Fact]
    public void ProfilePageHighlightsMyCharacters()
    {
        AddAuthorization().SetAuthorized("Arthas Menethil");
        Services.GetRequiredService<NavigationManager>().NavigateTo($"/characters/{Guid.NewGuid()}");

        var layout = RenderLayout();

        layout.Find("[data-testid=breadcrumb-page]").TextContent.ShouldBe("My characters");
        layout.Find(".rz-navigation-item-wrapper-active").TextContent.ShouldContain("My characters");
    }

    /// <summary>Highlights Companion &amp; sync on the confirm page under it (owner decision on #513).</summary>
    [Fact]
    public void ConfirmPageHighlightsCompanionAndSync()
    {
        AddAuthorization().SetAuthorized("Arthas Menethil");
        Services.GetRequiredService<NavigationManager>().NavigateTo("/companion/pair?code=K7M-4QX");

        var layout = RenderLayout();

        layout.Find("[data-testid=breadcrumb-page]").TextContent.ShouldBe("Companion & sync");
        layout.Find(".rz-navigation-item-wrapper-active").TextContent.ShouldContain("Companion & sync");

        Services.GetRequiredService<NavigationManager>().NavigateTo("/");

        layout.WaitForAssertion(() => layout.FindAll(".rz-navigation-item-wrapper-active").ShouldHaveSingleItem().TextContent.ShouldContain("Overview"));
    }

    /// <summary>Follows navigation to a page outside the sidebar in the breadcrumb.</summary>
    [Fact]
    public void BreadcrumbFollowsNavigation()
    {
        AddAuthorization().SetAuthorized("Arthas Menethil");
        var layout = RenderLayout();

        Services.GetRequiredService<NavigationManager>().NavigateTo("/characters/review?returnUrl=%2F");

        layout.WaitForAssertion(() => layout.Find("[data-testid=breadcrumb-page]").TextContent.ShouldBe("Review new characters"));
    }

    /// <summary>Leaves the breadcrumb page out for a page the shell doesn't know.</summary>
    [Fact]
    public void UnknownPageShowsOnlyTheBreadcrumbRoot()
    {
        AddAuthorization().SetAuthorized("Arthas Menethil");
        Services.GetRequiredService<NavigationManager>().NavigateTo("/somewhere");

        var layout = RenderLayout();

        layout.Find(".breadcrumb").TextContent.Trim().ShouldBe("RaidManager");
    }

    /// <summary>Shows the Discord avatar when the session has one.</summary>
    [Fact]
    public void DiscordAvatarReplacesTheInitials()
    {
        const string avatarUrl = "https://cdn.discordapp.com/avatars/1/a.png";
        AddAuthorization().SetAuthorized("Arthas Menethil").SetClaims(new Claim(RaidManagerClaimTypes.AvatarUrl, avatarUrl));

        var layout = RenderLayout();

        layout.FindAll("img.user-avatar").Select(image => image.GetAttribute("src")).ShouldAllBe(source => source == avatarUrl);
        layout.FindAll(".user-avatar-initials").ShouldBeEmpty();
    }

    /// <summary>Shows a page that fails as a design-system error with Try again.</summary>
    [Fact]
    public void FailingPageShowsTheErrorWithTryAgain()
    {
        AddAuthorization().SetAuthorized("Arthas Menethil");

        RenderFragment failing = builder =>
        {
            builder.OpenComponent<FailingPage>(0);
            builder.CloseComponent();
        };

        var layout = Render<MainLayout>(parameters => parameters.Add(component => component.Body, failing));

        var error = layout.Find("[data-testid=page-error]");
        error.QuerySelector("h1")!.TextContent.ShouldBe("Something went wrong");
        error.QuerySelector(".notice-title")!.TextContent.ShouldBe("This page could not be shown");
        layout.FindAll("[data-testid=page-error] button").Single().Click();
        layout.Find("[data-testid=page-error]").ShouldNotBeNull();
    }
    #endregion Tests

    #region Private Helpers
    /// <summary>Renders the layout around a placeholder page.</summary>
    /// <returns>The rendered layout.</returns>
    private IRenderedComponent<MainLayout> RenderLayout() =>
        Render<MainLayout>(parameters => parameters.Add(layout => layout.Body, (RenderFragment)(builder => builder.AddContent(0, "page"))));
    #endregion Private Helpers

    #region Nested Types
    /// <summary>Stands in for a page that throws while rendering.</summary>
    private sealed class FailingPage : ComponentBase
    {
        /// <inheritdoc />
        protected override void OnInitialized() => throw new InvalidOperationException("The page failed.");
    }
    #endregion Nested Types
}
