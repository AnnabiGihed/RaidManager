using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Radzen;
using Shouldly;
using Xunit;
using RaidManager.Web.Features.Shared.Components;

namespace RaidManager.Web.Tests.Features.Shared.Components;

/// <summary>Verifies the <see cref="IconTile"/> component.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: The tile shows any glyph from its parameter, hidden from screen readers.
/// </remarks>
public sealed class IconTileTests : BunitContext
{
    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="IconTileTests"/> class.</summary>
    public IconTileTests()
    {
        Services.AddRadzenComponents();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }
    #endregion Constructors

    #region Tests
    /// <summary>Shows a glyph hidden from screen readers.</summary>
    [Fact]
    public void IconTileShowsItsGlyph()
    {
        var tile = Render<IconTile>(parameters => parameters.Add(component => component.Glyph, "+"));

        tile.Find(".icon-tile").TextContent.ShouldBe("+");
        tile.Find(".icon-tile").GetAttribute("aria-hidden").ShouldBe("true");
        tile.Find(".icon-tile").ClassList.ShouldNotContain("icon-tile-community");
    }

    /// <summary>Shows a community's initials in the community look.</summary>
    [Fact]
    public void CommunityLookShowsInitials()
    {
        var tile = Render<IconTile>(parameters => parameters.Add(component => component.Glyph, "DT").Add(component => component.Appearance, IconTileAppearance.Community));

        tile.Find(".icon-tile").ClassList.ShouldContain("icon-tile-community");
        tile.Find(".icon-tile").TextContent.ShouldBe("DT");
    }
    #endregion Tests
}
