using System.Security.Claims;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Radzen;
using Shouldly;
using Xunit;
using RaidManager.ViewModels.Features.Characters;
using RaidManager.Web.Features.Authentication;
using RaidManager.Web.Features.Characters.Pages;
using RaidManager.Web.Tests.Support;

namespace RaidManager.Web.Tests.Features.Characters.Pages;

/// <summary>Verifies the My characters page.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-05<br/>
/// Purpose: Covers board 1 of the character profile mockup (story #19): the table with its links and badges, the empty
/// state, and the load error with a retry.
/// </remarks>
public sealed class MyCharactersTests : BunitContext
{
    #region Fields
    /// <summary>Stores the fake profiles API.</summary>
    private readonly FakeCharacterProfilesApiClient _api = new();
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="MyCharactersTests"/> class.</summary>
    public MyCharactersTests()
    {
        Services.AddRadzenComponents();
        Services.AddSingleton<ICharacterProfilesApiClient>(_api);
        Services.AddSingleton(TimeProvider.System);
        Services.AddTransient<MyCharactersViewModel>();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }
    #endregion Constructors

    #region Tests
    /// <summary>Lists each character with its class dot, profile link, loadout, saves and a fresh or stale badge.</summary>
    [Fact]
    public void CharactersAreListedAsBoardOneShowsThem()
    {
        var arthasdk = new CharacterSummary(Guid.NewGuid(), "Icecrown", "Arthasdk", "DeathKnight", 80, new CharacterLoadoutSummary("Frost DPS", "MeleeDamage", 5712), 2, DateTimeOffset.UtcNow.AddHours(-1));
        var jainaice = new CharacterSummary(Guid.NewGuid(), "Lordaeron", "Jainaice", "Mage", 80, null, 0, DateTimeOffset.UtcNow.AddDays(-5));
        _api.Characters = [arthasdk, jainaice];

        var page = RenderPage();

        page.WaitForElement("[data-testid=characters]");
        var link = page.Find("[data-testid=open-Arthasdk]");
        link.GetAttribute("href").ShouldBe($"/characters/{arthasdk.CharacterId}");
        link.ParentElement!.ClassList.ShouldContain("wow-class-deathknight");
        page.Markup.ShouldContain("Death Knight · Icecrown");
        page.Markup.ShouldContain("Frost DPS · GearScore 5,712");
        page.Markup.ShouldContain("2 this week");
        page.Markup.ShouldContain("No loadout synced yet");
        var badges = page.FindAll(".tag-chip");
        badges[0].ClassList.ShouldContain("tag-chip-success");
        badges[1].ClassList.ShouldContain("tag-chip-warning");
        badges[1].TextContent.Trim().ShouldBe("5 days ago");
        page.Markup.ShouldContain(MyCharactersViewModel.FreshnessNote);
    }

    /// <summary>Shows the empty state to a player without characters.</summary>
    [Fact]
    public void PlayerWithoutCharactersSeesTheEmptyState()
    {
        var page = RenderPage();

        page.WaitForElement("[data-testid=no-characters]");
        page.FindAll("[data-testid=characters]").ShouldBeEmpty();
    }

    /// <summary>Shows the error and loads again on Try again.</summary>
    [Fact]
    public void LoadErrorOffersARetry()
    {
        _api.Fails = true;
        var page = RenderPage();
        page.WaitForElement("[data-testid=load-failed]");

        _api.Fails = false;
        page.FindAll("button").First(button => button.TextContent.Trim() == "Try again").Click();

        page.WaitForElement("[data-testid=no-characters]");
        _api.Loads.ShouldBe(2);
    }
    #endregion Tests

    #region Private Helpers
    /// <summary>Signs a player in and renders the page.</summary>
    /// <returns>The rendered page.</returns>
    private IRenderedComponent<MyCharacters> RenderPage()
    {
        AddAuthorization().SetAuthorized("Arthas Menethil").SetClaims(new Claim(RaidManagerClaimTypes.UserId, Guid.NewGuid().ToString()));
        Services.GetRequiredService<NavigationManager>().NavigateTo("/characters");
        return Render<MyCharacters>();
    }
    #endregion Private Helpers
}
