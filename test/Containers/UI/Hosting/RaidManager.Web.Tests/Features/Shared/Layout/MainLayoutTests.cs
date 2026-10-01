using System.Security.Claims;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Radzen;
using Shouldly;
using Xunit;
using RaidManager.Web.Features.Authentication;
using RaidManager.Web.Features.Shared.Layout;

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
    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="MainLayoutTests"/> class.</summary>
    public MainLayoutTests()
    {
        Services.AddRadzenComponents();
        Services.AddShellNavigation();
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
        layout.Find("form[action='/sign-out']").ShouldNotBeNull();
        layout.FindAll(".user-avatar-initials").Select(avatar => avatar.TextContent).ShouldAllBe(initials => initials == "AM");
        layout.FindAll("[data-testid=public-frame]").ShouldBeEmpty();
    }

    /// <summary>Lists only existing pages and highlights the current one.</summary>
    [Fact]
    public void NavigationListsOverviewAndHighlightsIt()
    {
        AddAuthorization().SetAuthorized("Arthas Menethil");

        var layout = RenderLayout();

        layout.Find(".shell-navigation").TextContent.ShouldContain("PLAYER");
        layout.FindAll(".shell-navigation a").Select(link => link.GetAttribute("href")).ShouldBe(["/"]);
        layout.Markup.ShouldNotContain("OFFICER");
        layout.Find(".rz-navigation-item-wrapper-active").TextContent.ShouldContain("Overview");
        layout.Find("[data-testid=breadcrumb-page]").TextContent.ShouldBe("Overview");
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
    #endregion Tests

    #region Private Helpers
    /// <summary>Renders the layout around a placeholder page.</summary>
    /// <returns>The rendered layout.</returns>
    private IRenderedComponent<MainLayout> RenderLayout() =>
        Render<MainLayout>(parameters => parameters.Add(layout => layout.Body, (RenderFragment)(builder => builder.AddContent(0, "page"))));
    #endregion Private Helpers
}
