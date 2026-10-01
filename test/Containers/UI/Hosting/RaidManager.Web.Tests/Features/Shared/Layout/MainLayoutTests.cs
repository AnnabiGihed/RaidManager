using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Radzen;
using Shouldly;
using Xunit;
using RaidManager.Web.Features.Shared.Layout;

namespace RaidManager.Web.Tests.Features.Shared.Layout;

/// <summary>Verifies the session controls the layout shows to visitors and to signed-in players.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-09-30<br/>
/// Purpose: Visitors see "Sign in with Discord"; players see their name and a "Sign out" form.
/// </remarks>
public sealed class MainLayoutTests : BunitContext
{
    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="MainLayoutTests"/> class.</summary>
    public MainLayoutTests()
    {
        Services.AddRadzenComponents();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }
    #endregion Constructors

    #region Tests
    /// <summary>Renders the layout for an anonymous visitor.</summary>
    [Fact]
    public void VisitorSeesSignInWithDiscord()
    {
        AddAuthorization();

        var layout = RenderLayout();

        layout.Markup.ShouldContain("Sign in with Discord");
        layout.Markup.ShouldNotContain("Sign out");
    }

    /// <summary>Renders the layout for a signed-in player.</summary>
    [Fact]
    public void SignedInPlayerSeesTheirNameAndSignOut()
    {
        AddAuthorization().SetAuthorized("Arthas Menethil");

        var layout = RenderLayout();

        layout.Find("[data-testid=signed-in-name]").TextContent.ShouldBe("Arthas Menethil");
        layout.Find("form[action='/sign-out']").ShouldNotBeNull();
        layout.Markup.ShouldNotContain("Sign in with Discord");
    }

    /// <summary>Clicks "Sign in with Discord" and checks the return to the current page.</summary>
    [Fact]
    public void SignInReturnsToTheCurrentPage()
    {
        AddAuthorization();
        var navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo("/raids");
        var layout = RenderLayout();

        layout.FindAll("button").First(button => button.TextContent.Contains("Sign in with Discord", StringComparison.Ordinal)).Click();

        navigation.Uri.ShouldEndWith("/sign-in?returnUrl=%2Fraids");
    }
    #endregion Tests

    #region Private Helpers
    /// <summary>Renders the layout around a placeholder page.</summary>
    /// <returns>The rendered layout.</returns>
    private IRenderedComponent<MainLayout> RenderLayout() =>
        Render<MainLayout>(parameters => parameters.Add(layout => layout.Body, (RenderFragment)(builder => builder.AddContent(0, "page"))));
    #endregion Private Helpers
}
