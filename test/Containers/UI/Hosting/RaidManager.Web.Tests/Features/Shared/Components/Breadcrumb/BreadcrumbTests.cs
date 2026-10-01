using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Radzen;
using Shouldly;
using Xunit;
using RaidManager.Web.Features.Shared.Components;

namespace RaidManager.Web.Tests.Features.Shared.Components;

/// <summary>Verifies the <see cref="Breadcrumb"/> component.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: The trail marks only the last label as the current page.
/// </remarks>
public sealed class BreadcrumbTests : BunitContext
{
    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="BreadcrumbTests"/> class.</summary>
    public BreadcrumbTests()
    {
        Services.AddRadzenComponents();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }
    #endregion Constructors

    #region Tests
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

    #endregion Tests
}
