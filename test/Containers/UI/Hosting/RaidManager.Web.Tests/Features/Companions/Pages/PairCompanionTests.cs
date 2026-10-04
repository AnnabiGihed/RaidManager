using System.Security.Claims;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Radzen;
using Shouldly;
using Xunit;
using RaidManager.ViewModels.Features.Companions;
using RaidManager.Web.Features.Authentication;
using RaidManager.Web.Features.Companions.Pages;
using RaidManager.Web.Tests.Support;

namespace RaidManager.Web.Tests.Features.Companions.Pages;

/// <summary>Verifies the page where a player confirms a companion's code.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: Covers story #15's confirmation on the website: the code card (board 1), each reason a code can't be confirmed (boards 9 to 13), a failed confirmation (board 17) and the move to the list.
/// </remarks>
public sealed class PairCompanionTests : BunitContext
{
    #region Fields
    /// <summary>Stores the signed-in player.</summary>
    private readonly Guid _userId = Guid.NewGuid();

    /// <summary>Stores the fake companions API.</summary>
    private readonly FakeCompanionsApiClient _api = new();
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="PairCompanionTests"/> class.</summary>
    public PairCompanionTests()
    {
        Services.AddRadzenComponents();
        Services.AddSingleton<ICompanionsApiClient>(_api);
        Services.AddSingleton(TimeProvider.System);
        Services.AddTransient<PairCompanionViewModel>();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }
    #endregion Constructors

    #region Tests
    /// <summary>Shows the code card as board 1.</summary>
    [Fact]
    public void WaitingCodeShowsTheCodeCard()
    {
        var page = RenderPage("K7M-4QX");

        page.WaitForElement("[data-testid=pair-card]");
        page.Find(".code-display-label").TextContent.ShouldBe("Pairing code");
        page.Find(".code-display-code").TextContent.ShouldBe("K7M-4QX");
        page.Find(".code-display-caption").TextContent.ShouldStartWith("Expires in ");
        page.Find("[data-testid=computer] .labeled-value-value").TextContent.ShouldBe("BRYN-DESKTOP");
        page.Find("[data-testid=computer] .labeled-value-value").ClassList.ShouldContain("labeled-value-plain");
        page.Find("[data-testid=confirm-pairing]").TextContent.Trim().ShouldBe("Confirm pairing");
        page.Find("h1").TextContent.ShouldBe("Pair this companion");
        _api.Calls.ShouldBe(["lookup K7M-4QX"]);
    }

    /// <summary>Confirms the code.</summary>
    [Fact]
    public void ConfirmingMovesToTheListWithItsNotification()
    {
        var page = RenderPage("K7M-4QX");
        page.WaitForElement("[data-testid=confirm-pairing]");

        page.Find("[data-testid=confirm-pairing]").Click();

        page.WaitForAssertion(() => Services.GetRequiredService<NavigationManager>().Uri.ShouldEndWith("/companion?paired=BRYN-DESKTOP"));
        _api.Calls.ShouldBe(["lookup K7M-4QX", "confirm K7M-4QX"]);
    }

    /// <summary>Fails to confirm, as board 17.</summary>
    [Fact]
    public void FailedConfirmationKeepsTheCardAndNotifies()
    {
        _api.ChangesFail = true;
        var page = RenderPage("K7M-4QX");
        page.WaitForElement("[data-testid=confirm-pairing]");

        page.Find("[data-testid=confirm-pairing]").Click();

        var notice = page.WaitForElement("[data-testid=pair-notice]");
        notice.ClassList.ShouldContain("toast-danger");
        notice.QuerySelector(".toast-title")!.TextContent.ShouldBe("BRYN-DESKTOP wasn't paired");
        page.Find("[data-testid=pair-card]").ShouldNotBeNull();
    }

    /// <summary>Shows why a code can't be confirmed, as boards 9 to 11.</summary>
    /// <param name="status">What the API says.</param>
    /// <param name="noticeClass">The expected notice style.</param>
    /// <param name="title">The expected title.</param>
    [Theory]
    [InlineData(PairingCodeStatus.Expired, "notice-warning", "This code expired")]
    [InlineData(PairingCodeStatus.AlreadyConfirmed, "notice-info", "This code was already confirmed")]
    [InlineData(PairingCodeStatus.Unknown, "notice-warning", "No companion is waiting for this code")]
    public void RefusedCodeShowsItsNotice(PairingCodeStatus status, string noticeClass, string title)
    {
        _api.Lookup = new PairingLookup(status, null);

        var page = RenderPage("K7M-4QX");

        var notice = page.WaitForElement("[data-testid=pair-problem]");
        notice.ClassList.ShouldContain(noticeClass);
        notice.QuerySelector(".notice-title")!.TextContent.ShouldBe(title);
        page.FindAll("[data-testid=pair-card]").ShouldBeEmpty();
        page.FindAll("[data-testid=try-again]").ShouldBeEmpty();
    }

    /// <summary>Opens the page without a code, as board 12, and goes to the list.</summary>
    [Fact]
    public void MissingCodeOffersThePairedCompanions()
    {
        var page = RenderPage(code: null);

        page.WaitForElement("[data-testid=pair-problem]").QuerySelector(".notice-title")!.TextContent.ShouldBe("Start pairing in the companion");
        page.Find("[data-testid=see-companions]").Click();

        Services.GetRequiredService<NavigationManager>().Uri.ShouldEndWith("/companion");
        _api.Calls.ShouldBeEmpty();
    }

    /// <summary>Fails to check the code, as board 13, then tries again.</summary>
    [Fact]
    public void UncheckableCodeCanBeTriedAgain()
    {
        _api.Fails = true;
        var page = RenderPage("K7M-4QX");
        page.WaitForElement("[data-testid=try-again]");
        page.Find("[data-testid=pair-problem]").ClassList.ShouldContain("notice-danger");

        _api.Fails = false;
        page.Find("[data-testid=try-again]").Click();

        page.WaitForElement("[data-testid=pair-card]");
        _api.Calls.ShouldBe(["lookup K7M-4QX", "lookup K7M-4QX"]);
    }

    /// <summary>Cancels from the code card.</summary>
    [Fact]
    public void CancelGoesToTheListWithoutConfirming()
    {
        var page = RenderPage("K7M-4QX");
        page.WaitForElement("[data-testid=cancel-pairing]");

        page.Find("[data-testid=cancel-pairing]").Click();

        Services.GetRequiredService<NavigationManager>().Uri.ShouldEndWith("/companion");
        _api.Calls.ShouldBe(["lookup K7M-4QX"]);
    }
    #endregion Tests

    #region Private Helpers
    /// <summary>Signs the player in and renders the page for a code.</summary>
    /// <param name="code">The code in the address, if any.</param>
    /// <returns>The rendered page.</returns>
    private IRenderedComponent<PairCompanion> RenderPage(string? code)
    {
        AddAuthorization().SetAuthorized("Bryn Valewood").SetClaims(new Claim(RaidManagerClaimTypes.UserId, _userId.ToString()));
        var navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo(code is null ? "/companion/pair" : $"/companion/pair?code={Uri.EscapeDataString(code)}");
        return Render<PairCompanion>();
    }
    #endregion Private Helpers
}
