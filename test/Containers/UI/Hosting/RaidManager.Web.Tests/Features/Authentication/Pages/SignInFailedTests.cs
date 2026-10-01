using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Radzen;
using Shouldly;
using Xunit;
using RaidManager.ViewModels.Features.Authentication;
using RaidManager.Web.Features.Authentication.Pages;

namespace RaidManager.Web.Tests.Features.Authentication.Pages;

/// <summary>Verifies the page shown after a failed or cancelled Discord sign-in.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-09-30<br/>
/// Purpose: The page explains the reason from the query string and offers a retry that restarts sign-in.
/// </remarks>
public sealed class SignInFailedTests : BunitContext
{
    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="SignInFailedTests"/> class.</summary>
    public SignInFailedTests()
    {
        Services.AddRadzenComponents();
        Services.AddScoped<SignInFailedViewModel>();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }
    #endregion Constructors

    #region Tests
    /// <summary>Renders the page for a declined consent.</summary>
    [Fact]
    public void DeniedReasonShowsTheCancelledExplanation()
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo("/sign-in/failed?reason=denied");

        var page = Render<SignInFailed>();

        page.Find("h1").TextContent.ShouldBe("Sign-in cancelled");
        page.Find(".surface-card-accent").ClassList.ShouldContain("surface-card-accent-warning");
        page.Find("button.action-button-secondary").TextContent.ShouldContain("Back to home");
    }

    /// <summary>Renders the page for any other failure with the danger accent.</summary>
    [Fact]
    public void FailedReasonShowsTheDangerAccent()
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo("/sign-in/failed?reason=failed");

        var page = Render<SignInFailed>();

        page.Find("h1").TextContent.ShouldBe("Sign-in did not complete");
        page.Find(".surface-card-accent").ClassList.ShouldContain("surface-card-accent-danger");
    }

    /// <summary>Clicks "Back to home".</summary>
    [Fact]
    public void BackToHomeGoesHome()
    {
        var navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo("/sign-in/failed?reason=denied");
        var page = Render<SignInFailed>();

        page.Find("button.action-button-secondary").Click();

        navigation.Uri.ShouldBe("http://localhost/");
    }

    /// <summary>Clicks "Try again" and checks that sign-in restarts with a full page load.</summary>
    [Fact]
    public void TryAgainRestartsSignIn()
    {
        var navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo("/sign-in/failed?reason=failed");
        var page = Render<SignInFailed>();

        page.FindAll("button").First(button => button.TextContent.Contains("Try again", StringComparison.Ordinal)).Click();

        navigation.Uri.ShouldEndWith("/sign-in");
    }
    #endregion Tests
}
