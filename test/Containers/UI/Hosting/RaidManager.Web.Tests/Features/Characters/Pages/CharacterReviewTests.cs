using System.Security.Claims;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Radzen;
using Radzen.Blazor;
using Shouldly;
using Xunit;
using RaidManager.ViewModels.Features.Characters;
using RaidManager.Web.Features.Characters.Pages;
using RaidManager.Web.Features.Authentication;
using RaidManager.Web.Features.Shared.Components;
using RaidManager.Web.Tests.Support;

namespace RaidManager.Web.Tests.Features.Characters.Pages;

/// <summary>Verifies the character review page a player sees after sign-in.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Covers story #18's page: the list, approve, reject with confirmation, "Decide later", the all-set state
/// with a conflict, and the load error with a retry.
/// </remarks>
public sealed class CharacterReviewTests : BunitContext
{
    #region Fields
    /// <summary>Stores the signed-in player.</summary>
    private readonly Guid _userId = Guid.NewGuid();

    /// <summary>Stores the fake claims API.</summary>
    private readonly FakeCharacterClaimsApiClient _api = new();

    /// <summary>Stores the layout's notification area, once a test looks at it.</summary>
    private IRenderedComponent<NotificationArea>? _notifications;
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="CharacterReviewTests"/> class.</summary>
    public CharacterReviewTests()
    {
        Services.AddRadzenComponents();
        Services.AddSingleton<ICharacterClaimsApiClient>(_api);
        Services.AddSingleton(TimeProvider.System);
        Services.AddScoped<CharacterReviewViewModel>();
        Services.AddScoped<CharacterArrivalsViewModel>();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }
    #endregion Constructors

    #region Properties
    /// <summary>Gets the layout's notification area, where the page's notifications show (#577).</summary>
    private IRenderedComponent<NotificationArea> Notifications => _notifications ??= Render<NotificationArea>();
    #endregion Properties

    #region Tests
    /// <summary>Lists pending and conflicted claims.</summary>
    [Fact]
    public void PendingAndConflictedClaimsAreListed()
    {
        _api.Claims = [FakeCharacterClaimsApiClient.Claim("Arthasdk"), FakeCharacterClaimsApiClient.Claim("Sylvanash", CharacterClaim.ConflictState)];

        var page = RenderPage();

        page.WaitForAssertion(() => page.Markup.ShouldContain("1 character is waiting for your decision"));
        page.Find("[data-testid=approve-Arthasdk]").ShouldNotBeNull();
        page.FindAll("[data-testid=approve-Sylvanash]").ShouldBeEmpty();
        page.Markup.ShouldContain("An officer will review it");
        page.Find("[data-testid=decide-later]").ShouldNotBeNull();
    }

    /// <summary>Approves the last pending claim.</summary>
    [Fact]
    public void ApprovingTheLastClaimShowsAllSetWithTheConflictReminder()
    {
        var arthasdk = FakeCharacterClaimsApiClient.Claim("Arthasdk");
        _api.Claims = [arthasdk, FakeCharacterClaimsApiClient.Claim("Sylvanash", CharacterClaim.ConflictState)];
        var page = RenderPage();
        page.WaitForElement("[data-testid=approve-Arthasdk]");

        page.Find("[data-testid=approve-Arthasdk]").Click();

        page.WaitForElement("[data-testid=all-set]");
        page.Markup.ShouldContain("Sylvanash stays in conflict review until an officer decides.");
        page.FindAll("[data-testid=decide-later]").ShouldBeEmpty();
        _api.Decisions.ShouldBe([("approve", _userId, arthasdk.CharacterId)]);
        var notice = Notifications.Find("[data-testid=review-notice]");
        notice.ClassList.ShouldContain("toast-success");
        notice.QuerySelector(".toast-title")!.TextContent.ShouldBe("Arthasdk approved");
    }

    /// <summary>Closing the notification forgets it, and the next decision shows its own (#577).</summary>
    [Fact]
    public void ClosingTheNotificationLetsTheNextOneShow()
    {
        _api.Claims = [FakeCharacterClaimsApiClient.Claim("Arthasdk"), FakeCharacterClaimsApiClient.Claim("Jainaice")];
        var page = RenderPage();
        page.WaitForElement("[data-testid=approve-Arthasdk]");
        page.Find("[data-testid=approve-Arthasdk]").Click();
        Notifications.WaitForElement("[data-testid=review-notice] .toast-close").Click();

        Notifications.FindAll("[data-testid=review-notice]").ShouldBeEmpty();
        page.Find("[data-testid=approve-Jainaice]").Click();

        Notifications.WaitForAssertion(() => Notifications.Find("[data-testid=review-notice] .toast-title").TextContent.ShouldBe("Jainaice approved"));
    }

    /// <summary>Rejects a claim after confirming.</summary>
    [Fact]
    public void RejectingAsksForConfirmationFirst()
    {
        var jainaice = FakeCharacterClaimsApiClient.Claim("Jainaice");
        _api.Claims = [jainaice];
        var page = RenderPage();
        page.WaitForElement("[data-testid=reject-Jainaice]");

        page.Find("[data-testid=reject-Jainaice]").Click();
        var dialog = page.Find("[data-testid=reject-dialog]");
        dialog.QuerySelector("h2")!.TextContent.ShouldBe("Reject Jainaice?");
        dialog.QuerySelectorAll("p").Select(paragraph => paragraph.TextContent).ShouldBe(
            [CharacterReviewViewModel.RejectMessage(jainaice), CharacterReviewViewModel.RejectAdvice]);
        _api.Decisions.ShouldBeEmpty();
        page.FindAll("[data-testid=reject-dialog] button").First(button => button.TextContent.Trim() == "Reject").Click();

        page.WaitForElement("[data-testid=all-set]");
        _api.Decisions.ShouldBe([("reject", _userId, jainaice.CharacterId)]);
    }

    /// <summary>Cancels a rejection.</summary>
    [Fact]
    public void CancellingTheConfirmationChangesNothing()
    {
        _api.Claims = [FakeCharacterClaimsApiClient.Claim("Jainaice")];
        var page = RenderPage();
        page.WaitForElement("[data-testid=reject-Jainaice]");

        page.Find("[data-testid=reject-Jainaice]").Click();
        page.FindAll("[data-testid=reject-dialog] button").First(button => button.TextContent.Trim() == "Cancel").Click();

        page.FindAll("[data-testid=reject-dialog]").ShouldBeEmpty();
        page.Find("[data-testid=approve-Jainaice]").ShouldNotBeNull();
        _api.Decisions.ShouldBeEmpty();
        Notifications.FindAll("[data-testid=review-notice]").ShouldBeEmpty();
    }

    /// <summary>Lists a claim in the design system's table: the class by its readable name and color, the status as a badge.</summary>
    [Fact]
    public void ClaimsAreShownWithTheirClassAndStatus()
    {
        _api.Claims = [FakeCharacterClaimsApiClient.Claim("Arthasdk") with { Class = "DeathKnight" }, FakeCharacterClaimsApiClient.Claim("Sylvanash", CharacterClaim.ConflictState)];
        var page = RenderPage();
        page.WaitForElement("[data-testid=claims]");

        var rows = page.FindAll("[data-testid=claims] tbody tr");
        page.FindAll("[data-testid=claims] th").Select(heading => heading.TextContent).ShouldBe(["Character", "Class", "Race", "Level", "Found", "Status", "Decision"]);
        rows[0].QuerySelector(".wow-class")!.ClassList.ShouldContain("wow-class-deathknight");
        rows[0].QuerySelector(".wow-class")!.TextContent.Trim().ShouldBe("Death Knight");
        rows[0].QuerySelector(".tag-chip")!.ClassList.ShouldContain("tag-chip-warning");
        rows[1].QuerySelector(".tag-chip")!.ClassList.ShouldContain("tag-chip-danger");
        rows[1].TextContent.ShouldContain("An officer will review it");
        page.Find(".notice-title").TextContent.ShouldBe("1 character is waiting for your decision");
    }

    /// <summary>Shows a failed decision as a red notification.</summary>
    [Fact]
    public void AFailedDecisionShowsADangerNotification()
    {
        _api.Claims = [FakeCharacterClaimsApiClient.Claim("Arthasdk")];
        var page = RenderPage();
        page.WaitForElement("[data-testid=approve-Arthasdk]");
        _api.Fails = true;

        page.Find("[data-testid=approve-Arthasdk]").Click();

        Notifications.WaitForAssertion(() => Notifications.Find("[data-testid=review-notice]").ClassList.ShouldContain("toast-danger"));
    }

    /// <summary>Leaves the review undecided.</summary>
    [Fact]
    public void DecideLaterReturnsToTheRequestedPage()
    {
        _api.Claims = [FakeCharacterClaimsApiClient.Claim("Arthasdk")];
        var page = RenderPage("/raids?week=40");
        page.WaitForElement("[data-testid=approve-Arthasdk]");

        page.Find("[data-testid=decide-later]").Click();

        Services.GetRequiredService<NavigationManager>().Uri.ShouldEndWith("/raids?week=40");
        _api.Decisions.ShouldBeEmpty();
    }

    /// <summary>Opens the page while the API is unavailable, then retries once it is back.</summary>
    [Fact]
    public void LoadErrorOffersATryAgainThatReloads()
    {
        _api.Fails = true;
        var page = RenderPage();
        page.WaitForElement("[data-testid=load-failed]");
        page.Markup.ShouldContain("Nothing was approved or rejected.");

        _api.Fails = false;
        _api.Claims = [FakeCharacterClaimsApiClient.Claim("Arthasdk")];
        page.FindAll("button").First(button => button.TextContent.Contains("Try again", StringComparison.Ordinal)).Click();

        page.WaitForElement("[data-testid=approve-Arthasdk]");
    }

    /// <summary>Opens the page with nothing to decide and continues.</summary>
    [Fact]
    public void NothingToDecideOffersContinue()
    {
        var page = RenderPage("https://evil.example/steal");
        page.WaitForElement("[data-testid=all-set]");

        page.FindAll("button").First(button => button.TextContent.Contains("Continue", StringComparison.Ordinal)).Click();

        Services.GetRequiredService<NavigationManager>().Uri.ShouldBe("http://localhost/");
    }

    /// <summary>Opens the page with only a conflict waiting for an officer.</summary>
    [Fact]
    public void OnlyAConflictShowsAllSetWithTheReminder()
    {
        _api.Claims = [FakeCharacterClaimsApiClient.Claim("Sylvanash", CharacterClaim.ConflictState)];

        var page = RenderPage("/raids");

        page.WaitForElement("[data-testid=all-set]");
        page.Markup.ShouldContain("Sylvanash stays in conflict review until an officer decides.");
        page.FindAll("[data-testid=decide-later]").ShouldBeEmpty();
        page.FindAll("button").First(button => button.TextContent.Contains("Continue", StringComparison.Ordinal)).Click();
        Services.GetRequiredService<NavigationManager>().Uri.ShouldEndWith("/raids");
    }

    /// <summary>Opens the page with a session that holds no user id.</summary>
    [Fact]
    public void SessionWithoutAUserIdShowsTheLoadError()
    {
        AddAuthorization().SetAuthorized("Arthas Menethil");
        Services.GetRequiredService<NavigationManager>().NavigateTo("/characters/review");

        var page = Render<CharacterReview>();

        page.WaitForElement("[data-testid=load-failed]");
    }
    #endregion Tests

    #region Private Helpers
    /// <summary>Signs the player in and renders the page with a return URL.</summary>
    /// <param name="returnUrl">The page to continue to.</param>
    /// <returns>The rendered page.</returns>
    private IRenderedComponent<CharacterReview> RenderPage(string returnUrl = "/")
    {
        SignIn(returnUrl);
        return Render<CharacterReview>();
    }

    /// <summary>Signs the player in and opens the review page address.</summary>
    /// <param name="returnUrl">The page to continue to.</param>
    private void SignIn(string returnUrl = "/")
    {
        AddAuthorization().SetAuthorized("Arthas Menethil").SetClaims(new Claim(RaidManagerClaimTypes.UserId, _userId.ToString()));
        Services.GetRequiredService<NavigationManager>().NavigateTo($"/characters/review?returnUrl={Uri.EscapeDataString(returnUrl)}");
    }
    #endregion Private Helpers
}
