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
