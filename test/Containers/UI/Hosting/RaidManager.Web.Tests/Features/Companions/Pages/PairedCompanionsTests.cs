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

/// <summary>Verifies the paired companions list a player revokes computers from.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: Covers story #15's revocation on the website: the list (board 2), the confirmation (board 3), the list after revoking (board 4), an expired companion and a failed revocation (board 14), the empty list (board 15) and the load error (board 16).
/// </remarks>
public sealed class PairedCompanionsTests : BunitContext
{
    #region Fields
    /// <summary>Stores the signed-in player.</summary>
    private readonly Guid _userId = Guid.NewGuid();

    /// <summary>Stores the fake companions API.</summary>
    private readonly FakeCompanionsApiClient _api = new();
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="PairedCompanionsTests"/> class.</summary>
    public PairedCompanionsTests()
    {
        Services.AddRadzenComponents();
        Services.AddSingleton<ICompanionsApiClient>(_api);
        Services.AddSingleton(TimeProvider.System);
        Services.AddTransient<PairedCompanionsViewModel>();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }
    #endregion Constructors

    #region Tests
    /// <summary>Lists active, revoked and expired companions, as boards 2, 4 and 14.</summary>
    [Fact]
    public void CompanionsAreListedWithTheirStatus()
    {
        _api.Companions =
        [
            FakeCompanionsApiClient.Companion("BRYN-DESKTOP"),
            FakeCompanionsApiClient.Companion("BRYN-LAPTOP", PairedCompanion.RevokedStatus, DateTimeOffset.UtcNow),
            FakeCompanionsApiClient.Companion("OLD-PC", PairedCompanion.ExpiredStatus),
        ];

        var page = RenderPage();

        page.WaitForElement("[data-testid=companions]");
        page.FindAll("th").Select(heading => heading.TextContent).ShouldBe(["Computer", "Paired", "Last upload", "Status", string.Empty]);
        var rows = page.FindAll("tbody tr");
        rows.Count.ShouldBe(3);
        rows[0].QuerySelector(".companion-upload")!.TextContent.ShouldBe("No upload yet");
        rows[0].QuerySelector(".tag-chip")!.ClassList.ShouldContain("tag-chip-success");
        page.Find("[data-testid=revoke-BRYN-DESKTOP]").ShouldNotBeNull();
        rows[1].QuerySelector(".companion-note")!.TextContent.ShouldStartWith("Revoked today, ");
        rows[1].QuerySelector(".tag-chip")!.TextContent.Trim().ShouldBe("Revoked");
        rows[2].QuerySelector(".companion-note")!.TextContent.ShouldBe("Unused for 180 days");
        rows[2].QuerySelector(".tag-chip")!.ClassList.ShouldContain("tag-chip-neutral");
        page.FindAll("[data-testid=revoke-BRYN-LAPTOP]").ShouldBeEmpty();
        page.FindAll("[data-testid=revoke-OLD-PC]").ShouldBeEmpty();
        page.Find(".companions-note").TextContent.ShouldBe("To pair another computer, start pairing in its companion.");
    }

    /// <summary>Arrives from a confirmed pairing, as board 2.</summary>
    [Fact]
    public void ArrivalFromPairingShowsItsNotification()
    {
        _api.Companions = [FakeCompanionsApiClient.Companion("BRYN-DESKTOP")];

        var page = RenderPage("/companion?paired=BRYN-DESKTOP");

        var notice = page.Find("[data-testid=companions-notice]");
        notice.ClassList.ShouldContain("toast-success");
        notice.QuerySelector(".toast-title")!.TextContent.ShouldBe("BRYN-DESKTOP is paired");
    }

    /// <summary>Revokes a companion after confirming, as boards 3 and 4.</summary>
    [Fact]
    public void RevokingAsksForConfirmationFirst()
    {
        var laptop = FakeCompanionsApiClient.Companion("BRYN-LAPTOP");
        _api.Companions = [laptop];
        var page = RenderPage();
        page.WaitForElement("[data-testid=revoke-BRYN-LAPTOP]");

        page.Find("[data-testid=revoke-BRYN-LAPTOP]").Click();
        var dialog = page.Find("[data-testid=revoke-dialog]");
        dialog.QuerySelector("h2")!.TextContent.ShouldBe("Revoke BRYN-LAPTOP?");
        dialog.QuerySelectorAll("p").Select(paragraph => paragraph.TextContent).ShouldBe([PairedCompanionsViewModel.RevokeMessage(), PairedCompanionsViewModel.RevokeAdvice]);
        _api.Calls.ShouldBe(["list"]);
        page.FindAll("[data-testid=revoke-dialog] button").First(button => button.TextContent.Trim() == "Revoke").Click();

        page.WaitForAssertion(() => page.Find("[data-testid=companions-notice] .toast-title").TextContent.ShouldBe("BRYN-LAPTOP was revoked"));
        page.FindAll("[data-testid=revoke-dialog]").ShouldBeEmpty();
        page.Find(".companion-note").TextContent.ShouldStartWith("Revoked today, ");
        _api.Calls.ShouldBe(["list", $"revoke {laptop.CompanionId}", "list"]);
    }

    /// <summary>Cancels a revocation.</summary>
    [Fact]
    public void CancellingTheConfirmationChangesNothing()
    {
        _api.Companions = [FakeCompanionsApiClient.Companion("BRYN-LAPTOP")];
        var page = RenderPage();
        page.WaitForElement("[data-testid=revoke-BRYN-LAPTOP]");

        page.Find("[data-testid=revoke-BRYN-LAPTOP]").Click();
        page.FindAll("[data-testid=revoke-dialog] button").First(button => button.TextContent.Trim() == "Cancel").Click();

        page.FindAll("[data-testid=revoke-dialog]").ShouldBeEmpty();
        _api.Calls.ShouldBe(["list"]);
    }

    /// <summary>Fails to revoke, as board 14.</summary>
    [Fact]
    public void FailedRevocationNotifiesThatNothingChanged()
    {
        _api.Companions = [FakeCompanionsApiClient.Companion("BRYN-DESKTOP")];
        _api.ChangesFail = true;
        var page = RenderPage();
        page.WaitForElement("[data-testid=revoke-BRYN-DESKTOP]");

        page.Find("[data-testid=revoke-BRYN-DESKTOP]").Click();
        page.FindAll("[data-testid=revoke-dialog] button").First(button => button.TextContent.Trim() == "Revoke").Click();

        var notice = page.WaitForElement("[data-testid=companions-notice]");
        notice.ClassList.ShouldContain("toast-danger");
        notice.QuerySelector(".toast-title")!.TextContent.ShouldBe("BRYN-DESKTOP wasn't revoked");
    }

    /// <summary>Shows the empty list, as board 15.</summary>
    [Fact]
    public void NoCompanionsShowsTheEmptyState()
    {
        var page = RenderPage();

        var empty = page.WaitForElement("[data-testid=no-companions]");
        empty.QuerySelector(".empty-state-title")!.TextContent.ShouldBe("No paired computers yet");
        empty.QuerySelectorAll(".empty-state-mark").ShouldBeEmpty();
    }

    /// <summary>Fails to load, as board 16, then tries again.</summary>
    [Fact]
    public void LoadErrorOffersToTryAgain()
    {
        _api.Fails = true;
        var page = RenderPage();
        page.WaitForElement("[data-testid=load-failed]");

        _api.Fails = false;
        page.FindAll("button").First(button => button.TextContent.Trim() == "Try again").Click();

        page.WaitForElement("[data-testid=no-companions]");
    }
    #endregion Tests

    #region Private Helpers
    /// <summary>Signs the player in and renders the page.</summary>
    /// <param name="uri">The address the page opens at.</param>
    /// <returns>The rendered page.</returns>
    private IRenderedComponent<PairedCompanions> RenderPage(string uri = "/companion")
    {
        AddAuthorization().SetAuthorized("Bryn Valewood").SetClaims(new Claim(RaidManagerClaimTypes.UserId, _userId.ToString()));
        Services.GetRequiredService<NavigationManager>().NavigateTo(uri);
        return Render<PairedCompanions>();
    }
    #endregion Private Helpers
}
