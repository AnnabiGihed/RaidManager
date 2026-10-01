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

    /// <summary>Raises its click and carries the primary look by default.</summary>
    [Fact]
    public void ActionButtonRaisesItsClick()
    {
        var clicks = 0;
        var button = Render<ActionButton>(parameters => parameters
            .Add(component => component.Text, "Sign in with Discord")
            .Add(component => component.Click, () => clicks++));

        var element = button.Find("button.action-button");
        element.ClassList.ShouldContain("action-button-primary");
        element.TextContent.ShouldContain("Sign in with Discord");
        element.GetAttribute("type").ShouldBe("button");
        element.Click();
        clicks.ShouldBe(1);
    }

    /// <summary>Applies each appearance as its own class.</summary>
    /// <param name="appearance">The appearance.</param>
    /// <param name="expectedClass">The class it adds.</param>
    [Theory]
    [InlineData(ActionButtonAppearance.Primary, "action-button-primary")]
    [InlineData(ActionButtonAppearance.Secondary, "action-button-secondary")]
    [InlineData(ActionButtonAppearance.Danger, "action-button-danger")]
    public void ActionButtonAppliesItsAppearance(ActionButtonAppearance appearance, string expectedClass)
    {
        var button = Render<ActionButton>(parameters => parameters
            .Add(component => component.Text, "Go")
            .Add(component => component.Appearance, appearance));

        button.Find("button").ClassList.ShouldContain(expectedClass);
    }

    /// <summary>Submits a form, fills its container and stays disabled when told.</summary>
    [Fact]
    public void ActionButtonCanSubmitFillAndBeDisabled()
    {
        var button = Render<ActionButton>(parameters => parameters
            .Add(component => component.Text, "Save")
            .Add(component => component.ButtonType, ButtonType.Submit)
            .Add(component => component.FullWidth, true)
            .Add(component => component.Disabled, true));

        var element = button.Find("button.action-button");
        element.GetAttribute("type").ShouldBe("submit");
        element.HasAttribute("disabled").ShouldBeTrue();
        button.Find(".action-button-host").ClassList.ShouldContain("action-button-full");
    }

    /// <summary>Shows a titled card with its body, actions and accent.</summary>
    [Fact]
    public void SurfaceCardShowsTitleBodyActionsAndAccent()
    {
        var card = Render<SurfaceCard>(parameters => parameters
            .Add(component => component.Title, "Sign-in cancelled")
            .Add(component => component.HeadingLevel, HeadingLevel.H1)
            .Add(component => component.Accent, CardAccent.Warning)
            .AddChildContent("Discord did not share your account.")
            .Add(component => component.Actions, (RenderFragment)(builder => builder.AddMarkupContent(0, "<button>Try again</button>"))));

        card.Find("h1.surface-card-title").TextContent.ShouldBe("Sign-in cancelled");
        card.Find(".surface-card-body").TextContent.ShouldBe("Discord did not share your account.");
        card.Find(".surface-card-actions button").TextContent.ShouldBe("Try again");
        card.Find(".surface-card-accent").ClassList.ShouldContain("surface-card-accent-warning");
    }

    /// <summary>Renders each heading level and leaves out what isn't given.</summary>
    /// <param name="level">The heading level.</param>
    /// <param name="tag">The expected tag.</param>
    [Theory]
    [InlineData(HeadingLevel.H2, "h2")]
    [InlineData(HeadingLevel.H3, "h3")]
    public void SurfaceCardUsesTheHeadingLevelAndOmitsAbsentParts(HeadingLevel level, string tag)
    {
        var card = Render<SurfaceCard>(parameters => parameters
            .Add(component => component.Title, "Sign in to continue")
            .Add(component => component.HeadingLevel, level)
            .AddChildContent("Body"));

        card.Find($"{tag}.surface-card-title").TextContent.ShouldBe("Sign in to continue");
        card.FindAll(".surface-card-accent").ShouldBeEmpty();
        card.FindAll(".surface-card-actions").ShouldBeEmpty();
    }

    /// <summary>Shows a card without a title.</summary>
    [Fact]
    public void SurfaceCardWithoutTitleHasNoHeading()
    {
        var card = Render<SurfaceCard>(parameters => parameters.AddChildContent("Only a body"));

        card.FindAll(".surface-card-title").ShouldBeEmpty();
    }

    /// <summary>Centers a heading with every part.</summary>
    [Fact]
    public void PageHeadingShowsEyebrowTitleAndSubtitle()
    {
        var heading = Render<PageHeading>(parameters => parameters
            .Add(component => component.Eyebrow, "Signed in")
            .Add(component => component.Title, "Welcome, Anguish")
            .Add(component => component.Subtitle, "Your characters and raids will appear here.")
            .Add(component => component.Alignment, ContentAlignment.Center));

        heading.Find("header").ClassList.ShouldContain("page-heading-center");
        heading.Find(".page-heading-eyebrow").TextContent.ShouldBe("Signed in");
        heading.Find("h1").TextContent.ShouldBe("Welcome, Anguish");
        heading.Find(".page-heading-subtitle").TextContent.ShouldBe("Your characters and raids will appear here.");
    }

    /// <summary>Aligns a heading with only a title to the start.</summary>
    [Fact]
    public void PageHeadingWithOnlyATitleStartsAligned()
    {
        var heading = Render<PageHeading>(parameters => parameters.Add(component => component.Title, "Raids"));

        heading.Find("header").ClassList.ShouldNotContain("page-heading-center");
        heading.FindAll(".page-heading-eyebrow").ShouldBeEmpty();
        heading.FindAll(".page-heading-subtitle").ShouldBeEmpty();
    }

    /// <summary>Shows a note aligned as told.</summary>
    /// <param name="alignment">The alignment.</param>
    /// <param name="centered">Whether the note is centered.</param>
    [Theory]
    [InlineData(ContentAlignment.Center, true)]
    [InlineData(ContentAlignment.Start, false)]
    public void TextNoteShowsItsTextAligned(ContentAlignment alignment, bool centered)
    {
        var note = Render<TextNote>(parameters => parameters
            .Add(component => component.Text, "Your first sign-in creates your RaidManager account.")
            .Add(component => component.Alignment, alignment));

        var paragraph = note.Find("p.text-note");
        paragraph.TextContent.ShouldBe("Your first sign-in creates your RaidManager account.");
        paragraph.ClassList.Contains("text-note-center").ShouldBe(centered);
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
