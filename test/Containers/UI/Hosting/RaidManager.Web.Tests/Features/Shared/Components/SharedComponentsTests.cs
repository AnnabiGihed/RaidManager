using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Radzen;
using Shouldly;
using Xunit;
using RaidManager.ViewModels.Features.Shared.Shell;
using RaidManager.Web.Features.Shared.Components;

namespace RaidManager.Web.Tests.Features.Shared.Components;

/// <summary>Verifies the shared, generic components the shell is built from.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Each component works from its parameters alone, so other pages can reuse it (task #268).
/// </remarks>
public sealed class SharedComponentsTests : BunitContext
{
    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="SharedComponentsTests"/> class.</summary>
    public SharedComponentsTests()
    {
        Services.AddRadzenComponents();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }
    #endregion Constructors

    #region Tests
    /// <summary>Links the logo to its target.</summary>
    [Fact]
    public void BrandLogoLinksWhereItIsTold()
    {
        var logo = Render<BrandLogo>(parameters => parameters.Add(component => component.Href, "/raids"));

        var link = logo.Find("a.brand-logo");
        link.GetAttribute("href").ShouldBe("/raids");
        link.GetAttribute("aria-label").ShouldBe("RaidManager home");
        string.Concat(link.TextContent.Where(character => !char.IsWhiteSpace(character))).ShouldBe("RAIDMANAGER");
    }

    /// <summary>Shows a glyph hidden from screen readers.</summary>
    [Fact]
    public void IconTileShowsItsGlyph()
    {
        var tile = Render<IconTile>(parameters => parameters.Add(component => component.Glyph, "+"));

        tile.Find(".icon-tile").TextContent.ShouldBe("+");
        tile.Find(".icon-tile").GetAttribute("aria-hidden").ShouldBe("true");
    }

    /// <summary>Frames a card tile and passes extra attributes through.</summary>
    [Fact]
    public void SummaryTileCardShowsTitleSubtitleAndLeading()
    {
        var tile = Render<SummaryTile>(parameters => parameters
            .Add(component => component.Title, "Citadel Vanguard")
            .Add(component => component.Subtitle, "Icecrown · Community")
            .Add(component => component.Leading, (RenderFragment)(builder => builder.AddMarkupContent(0, "<i class=\"lead\"></i>")))
            .AddUnmatched("data-testid", "community"));

        var root = tile.Find("[data-testid=community]");
        root.ClassList.ShouldContain("summary-tile-card");
        root.QuerySelector(".lead").ShouldNotBeNull();
        tile.Find(".summary-tile-title").TextContent.ShouldBe("Citadel Vanguard");
        tile.Find(".summary-tile-subtitle").TextContent.ShouldBe("Icecrown · Community");
    }

    /// <summary>Leaves the frame and an empty subtitle out.</summary>
    [Fact]
    public void SummaryTilePlainOmitsTheFrameAndAnEmptySubtitle()
    {
        var tile = Render<SummaryTile>(parameters => parameters
            .Add(component => component.Title, "Anguish")
            .Add(component => component.Appearance, SummaryTileAppearance.Plain));

        tile.Find(".summary-tile").ClassList.ShouldNotContain("summary-tile-card");
        tile.FindAll(".summary-tile-subtitle").ShouldBeEmpty();
    }

    /// <summary>Marks the last of several labels as the current page.</summary>
    [Fact]
    public void BreadcrumbMarksTheLastLabelAsCurrent()
    {
        var breadcrumb = Render<Breadcrumb>(parameters => parameters.Add(component => component.Items, ["Citadel Vanguard", "Raids", "Edit"]));

        breadcrumb.Find(".breadcrumb").TextContent.ShouldContain("Citadel Vanguard");
        breadcrumb.FindAll("[aria-current=page]").Single().TextContent.ShouldBe("Edit");
        breadcrumb.FindAll("[aria-hidden=true]").Count.ShouldBe(2);
    }

    /// <summary>Shows a single label without marking it as a page.</summary>
    [Fact]
    public void BreadcrumbWithOneLabelHasNoCurrentPage()
    {
        var breadcrumb = Render<Breadcrumb>(parameters => parameters.Add(component => component.Items, ["RaidManager"]));

        breadcrumb.Find(".breadcrumb").TextContent.Trim().ShouldBe("RaidManager");
        breadcrumb.FindAll("[aria-current=page]").ShouldBeEmpty();
    }

    /// <summary>Lists the section's pages under its heading.</summary>
    [Fact]
    public void NavigationSectionLinksItsPages()
    {
        var section = Render<NavigationSection>(parameters => parameters
            .Add(component => component.Heading, "OFFICER")
            .Add(component => component.Entries, [new ShellEntry(ShellSection.Officer, "Schedule", "/schedule"), new ShellEntry(ShellSection.Officer, "Conflicts", "/conflicts")]));

        section.Find(".navigation-section-heading").TextContent.ShouldBe("OFFICER");
        section.FindAll("a").Select(link => link.GetAttribute("href")).ShouldBe(["/schedule", "/conflicts"]);
    }

    /// <summary>Raises its click and carries the dark-surface look.</summary>
    [Fact]
    public void SurfaceButtonRaisesItsClick()
    {
        var clicks = 0;
        var button = Render<SurfaceButton>(parameters => parameters
            .Add(component => component.Text, "Refresh")
            .Add(component => component.Click, () => clicks++));

        var element = button.Find("button.surface-button");
        element.TextContent.ShouldContain("Refresh");
        element.GetAttribute("type").ShouldBe("button");
        element.Click();
        clicks.ShouldBe(1);
    }

    /// <summary>Submits a form when asked, and stays disabled when told.</summary>
    [Fact]
    public void SurfaceButtonCanSubmitAndBeDisabled()
    {
        var button = Render<SurfaceButton>(parameters => parameters
            .Add(component => component.Text, "Save")
            .Add(component => component.ButtonType, ButtonType.Submit)
            .Add(component => component.Disabled, true));

        var element = button.Find("button.surface-button");
        element.GetAttribute("type").ShouldBe("submit");
        element.HasAttribute("disabled").ShouldBeTrue();
    }

    /// <summary>Shows the avatar image when there is one.</summary>
    [Fact]
    public void UserAvatarShowsTheImageAtItsSize()
    {
        var avatar = Render<UserAvatar>(parameters => parameters
            .Add(component => component.Name, "Arthas Menethil")
            .Add(component => component.ImageUrl, "https://cdn.discordapp.com/avatars/1/a.png")
            .Add(component => component.Size, 40));

        var image = avatar.Find("img.user-avatar");
        image.GetAttribute("width").ShouldBe("40");
        avatar.FindAll(".user-avatar-initials").ShouldBeEmpty();
    }
    #endregion Tests
}
