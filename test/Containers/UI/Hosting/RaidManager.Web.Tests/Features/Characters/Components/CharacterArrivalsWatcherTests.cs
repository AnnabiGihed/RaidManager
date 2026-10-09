using System.Security.Claims;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Radzen;
using Shouldly;
using Xunit;
using RaidManager.ViewModels.Features.Characters;
using RaidManager.Web.Features.Authentication;
using RaidManager.Web.Features.Characters.Components;
using RaidManager.Web.Features.Characters.Pages;
using RaidManager.Web.Features.Shared.Components;
using RaidManager.Web.Tests.Support;

namespace RaidManager.Web.Tests.Features.Characters.Components;

/// <summary>Verifies the check that tells a player, on any page, that a sync brought characters to review.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-09<br/>
/// Purpose: Covers story #595 on a fake clock: nothing before a sync, board 1's notification with its "Review them"
/// link on another page, and on the review page board 4's notification while the list updates itself.
/// </remarks>
public sealed class CharacterArrivalsWatcherTests : BunitContext
{
    #region Fields
    /// <summary>Stores the signed-in player.</summary>
    private readonly Guid _userId = Guid.NewGuid();

    /// <summary>Stores the fake claims API.</summary>
    private readonly FakeCharacterClaimsApiClient _api = new();

    /// <summary>Stores the clock the checks wait on.</summary>
    private readonly ArmedTimeProvider _time = new(DateTimeOffset.UtcNow);
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="CharacterArrivalsWatcherTests"/> class.</summary>
    public CharacterArrivalsWatcherTests()
    {
        Services.AddRadzenComponents();
        Services.AddSingleton<ICharacterClaimsApiClient>(_api);
        Services.AddSingleton<TimeProvider>(_time);
        Services.AddScoped<CharacterReviewViewModel>();
        Services.AddScoped<CharacterArrivalsViewModel>();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }
    #endregion Constructors

    #region Tests
    /// <summary>On another page, a sync's characters show board 1's notification, whose link opens the review page.</summary>
    [Fact]
    public void ASyncShowsTheNotificationWithItsLink()
    {
        _api.Claims = [FakeCharacterClaimsApiClient.Claim("Arthasdk")];
        var area = Open("/characters");

        Sync(1, FakeCharacterClaimsApiClient.Claim("Uthertank"), FakeCharacterClaimsApiClient.Claim("Valeerarog"));

        area.WaitForAssertion(() => area.Find("[data-testid=arrivals-notice] .toast-title").TextContent.ShouldBe("2 new characters to review"));
        area.Find("[data-testid=arrivals-notice]").ClassList.ShouldContain("toast-info");
        area.Find("[data-testid=arrivals-notice] .toast-message").TextContent.ShouldBe("Your companion found Uthertank and Valeerarog.");
        var link = area.Find("[data-testid=arrivals-notice] .toast-action");
        link.TextContent.ShouldBe("Review them");
        link.GetAttribute("href").ShouldBe("/characters/review?returnUrl=%2Fcharacters");
        link.Click();
        area.FindAll("[data-testid=arrivals-notice]").ShouldBeEmpty();
    }

    /// <summary>The notification on another page stays until closed or followed, for a player back from the game (#595).</summary>
    [Fact]
    public void TheNotificationOnAnotherPageStays()
    {
        var area = Open("/characters");
        Sync(1, FakeCharacterClaimsApiClient.Claim("Uthertank"));
        area.WaitForElement("[data-testid=arrivals-notice]");

        _time.Advance(Toast.Lifetime * 10);

        area.FindAll("[data-testid=arrivals-notice]").Count.ShouldBe(1);
    }

    /// <summary>Without a sync, nothing is shown, however long the page stays open.</summary>
    [Fact]
    public void NothingShowsWithoutASync()
    {
        _api.Claims = [FakeCharacterClaimsApiClient.Claim("Arthasdk")];
        var area = Open("/characters");

        Sync(1);
        Sync(2);

        area.FindAll("[data-testid=arrivals-notice]").ShouldBeEmpty();
    }

    /// <summary>On the review page, the list updates itself and board 4's notification has no link.</summary>
    [Fact]
    public void OnTheReviewPageTheListUpdatesItself()
    {
        _api.Claims = [FakeCharacterClaimsApiClient.Claim("Sylvanash", CharacterClaim.ConflictState)];
        var area = Open("/characters/review");
        var page = Render<CharacterReview>();
        page.WaitForElement("[data-testid=all-set]");

        Sync(1, FakeCharacterClaimsApiClient.Claim("Uthertank"));

        page.WaitForElement("[data-testid=approve-Uthertank]");
        area.WaitForAssertion(() => area.Find("[data-testid=arrivals-notice] .toast-title").TextContent.ShouldBe("1 new character arrived"));
        area.Find("[data-testid=arrivals-notice] .toast-message").TextContent.ShouldBe("Uthertank was added to the list.");
        area.FindAll("[data-testid=arrivals-notice] .toast-action").ShouldBeEmpty();
    }

    /// <summary>Closing the notification forgets it, and the next sync shows its own.</summary>
    [Fact]
    public void ClosingTheNotificationLetsTheNextOneShow()
    {
        var area = Open("/characters");
        Sync(1, FakeCharacterClaimsApiClient.Claim("Uthertank"));
        area.WaitForElement("[data-testid=arrivals-notice] .toast-close").Click();

        Sync(2, FakeCharacterClaimsApiClient.Claim("Valeerarog"));

        area.WaitForAssertion(() => area.Find("[data-testid=arrivals-notice] .toast-message").TextContent.ShouldBe("Your companion found Valeerarog."));
    }

    /// <summary>A session without a user id checks nothing.</summary>
    [Fact]
    public void ASessionWithoutAUserIdChecksNothing()
    {
        AddAuthorization().SetAuthorized("Arthas Menethil");

        Render<CharacterArrivalsWatcher>();

        SpinWait.SpinUntil(() => _time.Armed > 0, TimeSpan.FromMilliseconds(200)).ShouldBeFalse();
    }
    #endregion Tests

    #region Private Helpers
    /// <summary>Signs the player in on a page and starts the checks.</summary>
    /// <param name="uri">The page the player is on.</param>
    /// <returns>The notification area.</returns>
    private IRenderedComponent<NotificationArea> Open(string uri)
    {
        AddAuthorization().SetAuthorized("Arthas Menethil").SetClaims(new Claim(RaidManagerClaimTypes.UserId, _userId.ToString()));
        Services.GetRequiredService<NavigationManager>().NavigateTo(uri);
        var area = Render<NotificationArea>();
        Render<CharacterArrivalsWatcher>();
        return area;
    }

    /// <summary>Adds what a sync brought, then lets the next check run once the watcher waits for it.</summary>
    /// <param name="check">The number of the check about to run, counted from the first wait.</param>
    /// <param name="arrived">The new claims.</param>
    private void Sync(int check, params CharacterClaim[] arrived)
    {
        SpinWait.SpinUntil(() => _time.Armed >= check, TimeSpan.FromSeconds(10)).ShouldBeTrue();
        _api.Claims = [.. _api.Claims, .. arrived];
        _time.Advance(CharacterArrivalsViewModel.CheckInterval);
    }
    #endregion Private Helpers
}
