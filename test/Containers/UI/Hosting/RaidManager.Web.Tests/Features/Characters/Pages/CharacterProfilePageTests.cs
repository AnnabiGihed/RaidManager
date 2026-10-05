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

/// <summary>Verifies a character's profile page.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-05<br/>
/// Purpose: Covers board 2 of the character profile mockup (story #19): every card, the empty cards before a sync, a
/// character that isn't the player's, and the load error with a retry.
/// </remarks>
public sealed class CharacterProfilePageTests : BunitContext
{
    #region Fields
    /// <summary>Stores the character.</summary>
    private readonly Guid _characterId = Guid.NewGuid();

    /// <summary>Stores the fake profiles API.</summary>
    private readonly FakeCharacterProfilesApiClient _api = new();
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="CharacterProfilePageTests"/> class.</summary>
    public CharacterProfilePageTests()
    {
        Services.AddRadzenComponents();
        Services.AddSingleton<ICharacterProfilesApiClient>(_api);
        Services.AddSingleton(TimeProvider.System);
        Services.AddTransient<CharacterProfileViewModel>();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }
    #endregion Constructors

    #region Tests
    /// <summary>Shows the header and the six cards of board 2.</summary>
    [Fact]
    public void ProfileShowsEveryCardOfBoardTwo()
    {
        _api.Profile = FakeCharacterProfilesApiClient.Arthasdk(_characterId);

        var page = RenderPage();

        page.WaitForElement("[data-testid=profile]");
        page.Find(".page-heading-eyebrow").TextContent.ShouldBe("Death Knight · Level 80");
        page.Find(".page-heading-title").TextContent.ShouldBe("Arthasdk");
        page.Find(".page-heading-subtitle").TextContent.ShouldBe("Icecrown · Alliance · <Citadel Vanguard>");
        page.Find("[data-testid=data-sources]").TextContent.ShouldContain("Warmane Armory");
        page.Find("[data-testid=professions]").TextContent.ShouldContain("Blacksmithing 450");
        page.Find("[data-testid=visibility]").TextContent.ShouldContain("Community");
        page.Find("[data-testid=note-and-visibility]").TextContent.ShouldContain(CharacterProfileViewModel.NoNote);
        var loadouts = page.Find("[data-testid=loadouts]");
        loadouts.TextContent.ShouldContain("Melee damage · GearScore 5,712");
        loadouts.TextContent.ShouldContain("Talents 51/10/10 · from the addon");
        loadouts.QuerySelectorAll(".tag-chip").ShouldHaveSingleItem().TextContent.Trim().ShouldBe("Primary");
        page.Find("[data-testid=raid-saves]").TextContent.ShouldContain("Icecrown Citadel · 25 players, heroic");
        var equipment = page.Find("[data-testid=equipment]");
        equipment.TextContent.ShouldContain("Primary loadout, Frost DPS");
        equipment.QuerySelectorAll(".profile-gear-item").Select(item => item.TextContent).ShouldBe(["Sanctified Scourgelord Helmet", "Whispering Fanged Skull"]);
        equipment.QuerySelectorAll(".profile-gear-slot").Select(slot => slot.TextContent).ShouldBe(["Head", "Trinket"]);
        equipment.QuerySelectorAll(".profile-gear-level").Select(level => level.TextContent).ShouldBe(["264", "—"]);
    }

    /// <summary>Shows each card's empty text before anything was synchronized.</summary>
    [Fact]
    public void UnsyncedProfileShowsEmptyCards()
    {
        _api.Profile = FakeCharacterProfilesApiClient.Arthasdk(_characterId) with { Professions = [], Loadouts = [], RaidSaves = [], AddonSynchronizedAtUtc = null };

        var page = RenderPage();

        page.WaitForElement("[data-testid=profile]");
        page.Markup.ShouldContain("No professions synced yet.");
        page.Markup.ShouldContain("No current raid saves.");
        page.Find("[data-testid=equipment]").TextContent.ShouldContain("No loadout synced yet");
        page.Find("[data-testid=data-sources]").TextContent.ShouldContain(CharacterProfileViewModel.NeverSynced);
    }

    /// <summary>Says a character isn't the player's and goes back to the list.</summary>
    [Fact]
    public void AnotherPlayersCharacterIsNotFound()
    {
        var page = RenderPage();
        page.WaitForElement("[data-testid=not-found]");

        page.FindAll("button").First(button => button.TextContent.Trim() == "Back to My characters").Click();

        Services.GetRequiredService<NavigationManager>().Uri.ShouldEndWith("/characters");
    }

    /// <summary>Shows the error and loads again on Try again.</summary>
    [Fact]
    public void LoadErrorOffersARetry()
    {
        _api.Fails = true;
        var page = RenderPage();
        page.WaitForElement("[data-testid=load-failed]");

        _api.Fails = false;
        _api.Profile = FakeCharacterProfilesApiClient.Arthasdk(_characterId);
        page.FindAll("button").First(button => button.TextContent.Trim() == "Try again").Click();

        page.WaitForElement("[data-testid=profile]");
        _api.Loads.ShouldBe(2);
    }
    #endregion Tests

    #region Private Helpers
    /// <summary>Signs a player in and renders the profile page.</summary>
    /// <returns>The rendered page.</returns>
    private IRenderedComponent<CharacterProfilePage> RenderPage()
    {
        AddAuthorization().SetAuthorized("Arthas Menethil").SetClaims(new Claim(RaidManagerClaimTypes.UserId, Guid.NewGuid().ToString()));
        Services.GetRequiredService<NavigationManager>().NavigateTo($"/characters/{_characterId}");
        return Render<CharacterProfilePage>(parameters => parameters.Add(page => page.CharacterId, _characterId));
    }
    #endregion Private Helpers
}
