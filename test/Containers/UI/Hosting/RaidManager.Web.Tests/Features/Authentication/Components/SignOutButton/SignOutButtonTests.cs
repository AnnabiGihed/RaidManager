using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Radzen;
using Shouldly;
using Xunit;
using RaidManager.Web.Features.Authentication.Components;

namespace RaidManager.Web.Tests.Features.Authentication.Components;

/// <summary>Verifies the sign-out button's form.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: The form posts to the sign-out endpoint with an antiforgery token, so any page can offer Sign out.
/// </remarks>
public sealed class SignOutButtonTests : BunitContext
{
    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="SignOutButtonTests"/> class.</summary>
    public SignOutButtonTests()
    {
        Services.AddRadzenComponents();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }
    #endregion Constructors

    #region Tests
    /// <summary>Renders the form with a submit button.</summary>
    [Fact]
    public void FormPostsToSignOutWithASubmitButton()
    {
        var button = Render<SignOutButton>();

        var form = button.Find("form");
        form.GetAttribute("method").ShouldBe("post");
        form.GetAttribute("action").ShouldBe("/sign-out");
        form.QuerySelector("button[type=submit]").ShouldNotBeNull().TextContent.ShouldContain("Sign out");
    }

    /// <summary>Uses another label when told.</summary>
    [Fact]
    public void LabelCanChange()
    {
        var button = Render<SignOutButton>(parameters => parameters.Add(component => component.Text, "Leave"));

        button.Find("button").TextContent.ShouldContain("Leave");
    }
    #endregion Tests
}
