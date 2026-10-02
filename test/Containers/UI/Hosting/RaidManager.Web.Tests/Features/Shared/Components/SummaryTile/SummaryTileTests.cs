using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Radzen;
using Shouldly;
using Xunit;
using RaidManager.Web.Features.Shared.Components;

namespace RaidManager.Web.Tests.Features.Shared.Components;

/// <summary>Verifies the <see cref="SummaryTile"/> component.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: The tile renders as a card or plain, from its appearance parameter.
/// </remarks>
public sealed class SummaryTileTests : BunitContext
{
    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="SummaryTileTests"/> class.</summary>
    public SummaryTileTests()
    {
        Services.AddRadzenComponents();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }
    #endregion Constructors

    #region Tests
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

    /// <summary>Shows a chevron and the link look only for a tile that leads somewhere.</summary>
    [Fact]
    public void ALinkTileShowsAChevron()
    {
        var link = Render<SummaryTile>(parameters => parameters.Add(component => component.Title, "Dark Templars").Add(component => component.ShowsLink, true));
        var plain = Render<SummaryTile>(parameters => parameters.Add(component => component.Title, "Anguish"));

        link.Find(".summary-tile").ClassList.ShouldContain("summary-tile-link");
        link.Find(".summary-tile-chevron").GetAttribute("aria-hidden").ShouldBe("true");
        plain.Find(".summary-tile").ClassList.ShouldNotContain("summary-tile-link");
        plain.FindAll(".summary-tile-chevron").ShouldBeEmpty();
    }
    #endregion Tests
}
