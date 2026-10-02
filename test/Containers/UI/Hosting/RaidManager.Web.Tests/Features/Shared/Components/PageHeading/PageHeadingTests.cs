using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Radzen;
using Shouldly;
using Xunit;
using RaidManager.Web.Features.Shared.Components;

namespace RaidManager.Web.Tests.Features.Shared.Components;

/// <summary>Verifies the <see cref="PageHeading"/> component.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: The heading shows its optional eyebrow and subtitle only when given.
/// </remarks>
public sealed class PageHeadingTests : BunitContext
{
    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="PageHeadingTests"/> class.</summary>
    public PageHeadingTests()
    {
        Services.AddRadzenComponents();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }
    #endregion Constructors

    #region Tests
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

    /// <summary>Shows actions at the end of the heading only when given.</summary>
    [Fact]
    public void ActionsAreShownWhenGiven()
    {
        var plain = Render<PageHeading>(parameters => parameters.Add(component => component.Title, "Overview"));
        var withActions = Render<PageHeading>(parameters => parameters
            .Add(component => component.Title, "Review your new characters")
            .Add(component => component.Actions, "Decide later"));

        plain.FindAll(".page-heading-actions").ShouldBeEmpty();
        withActions.Find(".page-heading-actions").TextContent.ShouldBe("Decide later");
    }
    #endregion Tests
}
