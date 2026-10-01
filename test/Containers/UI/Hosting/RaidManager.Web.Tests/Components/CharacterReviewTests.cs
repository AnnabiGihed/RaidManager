using System.Security.Claims;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Radzen;
using Radzen.Blazor;
using Shouldly;
using Xunit;
using RaidManager.ViewModels.Features.Characters;
using RaidManager.Web.Components.Pages;
using RaidManager.Web.Features.Authentication;
using RaidManager.Web.Tests.Support;

namespace RaidManager.Web.Tests.Components;

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
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="CharacterReviewTests"/> class.</summary>
    public CharacterReviewTests()
    {
        Services.AddRadzenComponents();
        Services.AddSingleton<ICharacterClaimsApiClient>(_api);
        Services.AddSingleton(TimeProvider.System);
        Services.AddScoped<CharacterReviewViewModel>();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }
    #endregion Constructors

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
        Services.GetRequiredService<NotificationService>().Messages.ShouldContain(message =>
            message.Severity == NotificationSeverity.Success && message.Summary == "Arthasdk approved");
    }

    /// <summary>Rejects a claim after confirming.</summary>
    [Fact]
    public void RejectingAsksForConfirmationFirst()
    {
        var jainaice = FakeCharacterClaimsApiClient.Claim("Jainaice");
        _api.Claims = [jainaice];
        var (page, dialog) = RenderPageWithDialog();
        page.WaitForElement("[data-testid=reject-Jainaice]");

        page.Find("[data-testid=reject-Jainaice]").Click();
        dialog.WaitForAssertion(() => dialog.Markup.ShouldContain("Reject Jainaice?"));
        _api.Decisions.ShouldBeEmpty();
        dialog.FindAll("button").First(button => button.TextContent.Trim() == "Reject").Click();

        page.WaitForElement("[data-testid=all-set]");
        _api.Decisions.ShouldBe([("reject", _userId, jainaice.CharacterId)]);
    }

    /// <summary>Cancels a rejection.</summary>
    [Fact]
    public void CancellingTheConfirmationChangesNothing()
    {
        _api.Claims = [FakeCharacterClaimsApiClient.Claim("Jainaice")];
        var (page, dialog) = RenderPageWithDialog();
        page.WaitForElement("[data-testid=reject-Jainaice]");

        page.Find("[data-testid=reject-Jainaice]").Click();
        dialog.WaitForAssertion(() => dialog.Markup.ShouldContain("Reject Jainaice?"));
        dialog.FindAll("button").First(button => button.TextContent.Trim() == "Cancel").Click();

        page.WaitForAssertion(() => page.Find("[data-testid=approve-Jainaice]").ShouldNotBeNull());
        _api.Decisions.ShouldBeEmpty();
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
    /// <summary>Signs the player in and renders the page next to the dialog host the layout provides.</summary>
    /// <returns>The rendered page and dialog host.</returns>
    private (IRenderedComponent<CharacterReview> Page, IRenderedComponent<RadzenDialog> Dialog) RenderPageWithDialog()
    {
        SignIn();
        var dialog = Render<RadzenDialog>();
        return (Render<CharacterReview>(), dialog);
    }

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
