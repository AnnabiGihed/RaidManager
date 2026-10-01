using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Radzen;
using Shouldly;
using Xunit;
using RaidManager.Web.Features.Shared.Components;

namespace RaidManager.Web.Tests.Features.Shared.Components;

/// <summary>Verifies the <see cref="BrandLogo"/> component.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: The logo links wherever its parent says, so the shell and the signed-out pages can share it.
/// </remarks>
public sealed class BrandLogoTests : BunitContext
{
    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="BrandLogoTests"/> class.</summary>
    public BrandLogoTests()
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

    #endregion Tests
}
