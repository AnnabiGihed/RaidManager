using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Radzen;
using Shouldly;
using Xunit;
using RaidManager.Web.Features.Shared.Components;

namespace RaidManager.Web.Tests.Features.Shared.Components;

/// <summary>Verifies the <see cref="ActionButton"/> component.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: The button wraps Radzen's with the design system's appearances, so pages never restyle a Radzen button.
/// </remarks>
public sealed class ActionButtonTests : BunitContext
{
    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="ActionButtonTests"/> class.</summary>
    public ActionButtonTests()
    {
        Services.AddRadzenComponents();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }
    #endregion Constructors

    #region Tests
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
    [InlineData(ActionButtonAppearance.Text, "action-button-text")]
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

    #endregion Tests
}
