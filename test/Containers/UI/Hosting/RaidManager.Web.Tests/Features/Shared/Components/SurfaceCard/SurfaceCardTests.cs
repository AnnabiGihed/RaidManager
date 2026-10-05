using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Radzen;
using Shouldly;
using Xunit;
using RaidManager.Web.Features.Shared.Components;

namespace RaidManager.Web.Tests.Features.Shared.Components;

/// <summary>Verifies the <see cref="SurfaceCard"/> component.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: The card shows its optional title, body, actions and accent from parameters alone.
/// </remarks>
public sealed class SurfaceCardTests : BunitContext
{
    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="SurfaceCardTests"/> class.</summary>
    public SurfaceCardTests()
    {
        Services.AddRadzenComponents();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }
    #endregion Constructors

    #region Tests
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

    /// <summary>Shows header actions at the end of the title row only when given.</summary>
    [Fact]
    public void HeaderActionsSitBesideTheTitle()
    {
        var card = Render<SurfaceCard>(parameters => parameters.Add(component => component.Title, "Roles").Add(component => component.HeaderActions, "Create role"));
        var plain = Render<SurfaceCard>(parameters => parameters.Add(component => component.Title, "Community"));

        card.Find(".surface-card-header .surface-card-title").TextContent.ShouldBe("Roles");
        card.Find(".surface-card-header-actions").TextContent.ShouldBe("Create role");
        plain.FindAll(".surface-card-header-actions").ShouldBeEmpty();
    }

    /// <summary>Marks a compact card for the smaller title, and leaves an ordinary card unmarked.</summary>
    [Fact]
    public void CompactCardIsMarkedForTheSmallerTitle()
    {
        var compact = Render<SurfaceCard>(parameters => parameters.Add(component => component.Title, "Loadouts").Add(component => component.Compact, true));
        var ordinary = Render<SurfaceCard>(parameters => parameters.Add(component => component.Title, "Loadouts"));

        compact.Find("section").ClassName.ShouldBe("surface-card surface-card-compact");
        ordinary.Find("section").ClassName.ShouldBe("surface-card");
    }
    #endregion Tests
}
