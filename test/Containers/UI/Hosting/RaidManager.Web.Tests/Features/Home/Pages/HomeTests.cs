using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Radzen;
using Shouldly;
using Xunit;
using HomePage = RaidManager.Web.Features.Home.Pages.Home;

namespace RaidManager.Web.Tests.Features.Home.Pages;

/// <summary>Verifies the home page for visitors and signed-in players.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Visitors get the sign-in card of the sign-in mockup; signed-in players get a welcome.
/// </remarks>
public sealed class HomeTests : BunitContext
{
    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="HomeTests"/> class.</summary>
    public HomeTests()
    {
        Services.AddRadzenComponents();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }
    #endregion Constructors

    #region Tests
    /// <summary>Clicks "Sign in with Discord" as a visitor.</summary>
    [Fact]
    public void VisitorCanStartDiscordSignIn()
    {
        AddAuthorization();
        var navigation = Services.GetRequiredService<NavigationManager>();
        var page = Render<HomePage>();

        page.Find("h1").TextContent.ShouldBe("Sign in to continue");
        page.FindAll("button").First(button => button.TextContent.Contains("Sign in with Discord", StringComparison.Ordinal)).Click();

        navigation.Uri.ShouldEndWith("/sign-in");
    }

    /// <summary>Welcomes a signed-in player by name.</summary>
    [Fact]
    public void SignedInPlayerIsWelcomed()
    {
        AddAuthorization().SetAuthorized("Arthas Menethil");

        var page = Render<HomePage>();

        page.Find("h1").TextContent.ShouldBe("Welcome, Arthas Menethil");
        page.Markup.ShouldNotContain("Sign in with Discord");
    }
    #endregion Tests
}
