using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;
using RaidManager.ApiService.Features.Characters;
using RaidManager.ApiService.Features.Shared.Authentication;
using RaidManager.ApiService.Tests.Support;
using RaidManager.Application.Features.Characters.Queries.GetCharacterProfile;
using RaidManager.Domain.Features.Characters.Aggregates;
using RaidManager.Domain.Features.Characters.Enums;
using RaidManager.Domain.Features.Characters.Repositories;
using RaidManager.Domain.Features.Characters.ValueObjects;
using RaidManager.Domain.Features.Shared.Enums;
using RaidManager.Domain.Features.Shared.Identifiers;
using DomainUnitOfWork = Pivot.Framework.Domain.Repositories.IUnitOfWork;

namespace RaidManager.ApiService.Tests.Features.Characters;

/// <summary>Verifies the website-only character profile endpoints through HTTP and PostgreSQL.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-05<br/>
/// Purpose: Proves the website key guards the endpoints, that a player lists and opens only their own characters
/// (owner decision on #19, 2026-10-05), and that enums travel as names.
/// </remarks>
[Collection(ApiTestGroup.Name)]
public sealed class CharacterProfileEndpointTests
{
    #region Fields
    /// <summary>Stores the API factory.</summary>
    private readonly ApiFactory _api;
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="CharacterProfileEndpointTests"/> class.</summary>
    /// <param name="api">The API factory.</param>
    public CharacterProfileEndpointTests(ApiFactory api)
    {
        _api = api;
    }
    #endregion Constructors

    #region Tests
    /// <summary>Lists characters without the website key.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task RequestWithoutTheWebsiteKeyIsUnauthorized()
    {
        using var client = _api.CreateClient();

        var response = await client.GetAsync(CharactersRoute(Guid.NewGuid()));

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    /// <summary>Lists a player's approved characters with their primary loadout, saves and latest sync.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task ListShowsOnlyThePlayersApprovedCharacters()
    {
        var alice = Guid.NewGuid();
        var owned = OwnedCharacter(alice);
        var pending = NewCharacter();
        pending.RequestClaim(new UserId(alice), DateTimeOffset.UtcNow).IsSuccess.ShouldBeTrue();
        await SaveAsync(owned, pending);
        using var client = WebsiteClient();

        var characters = (await client.GetFromJsonAsync<List<CharacterSummary>>(CharactersRoute(alice))).ShouldNotBeNull();

        var character = characters.ShouldHaveSingleItem();
        character.CharacterId.ShouldBe(owned.Id.Value);
        (character.Realm, character.Class, character.Level).ShouldBe(("Icecrown", "DeathKnight", 80));
        character.PrimaryLoadout.ShouldBe(new CharacterLoadoutSummary("Frost DPS", "MeleeDamage", 5712));
        character.CurrentRaidSaveCount.ShouldBe(1);
        character.LastSynchronizedAtUtc.ShouldNotBeNull();
    }

    /// <summary>Opens the profile of an owned character.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task OwnerGetsTheProfileWithEnumNames()
    {
        var alice = Guid.NewGuid();
        var owned = OwnedCharacter(alice);
        await SaveAsync(owned);
        using var client = WebsiteClient();

        var profile = (await client.GetFromJsonAsync<CharacterProfile>(ProfileRoute(alice, owned.Id.Value))).ShouldNotBeNull();

        (profile.Name, profile.Class, profile.Race, profile.Faction, profile.Visibility).ShouldBe((owned.Name.Value, "DeathKnight", "Human", "Alliance", "Community"));
        profile.Professions.ShouldBe([new CharacterProfession("Blacksmithing", 450, 450)]);
        var loadout = profile.Loadouts.ShouldHaveSingleItem();
        (loadout.Role, loadout.Source, loadout.Talents).ShouldBe(("MeleeDamage", "WowAddon", "0/53/18"));
        loadout.Gear.ShouldHaveSingleItem().Slot.ShouldBe("MainHand");
        var save = profile.RaidSaves.ShouldHaveSingleItem();
        (save.Instance, save.Difficulty, save.LockoutId).ShouldBe(("IcecrownCitadel", "TwentyFivePlayerHeroic", "43127"));
        profile.AddonSynchronizedAtUtc.ShouldNotBeNull();
        profile.CompleteRaidSaveScanAtUtc.ShouldNotBeNull();
        profile.ArmorySynchronizedAtUtc.ShouldBeNull();
    }

    /// <summary>Opens another player's character, and a character that doesn't exist.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task AnotherPlayersOrAnUnknownCharacterIsNotFound()
    {
        var owned = OwnedCharacter(Guid.NewGuid());
        await SaveAsync(owned);
        using var client = WebsiteClient();
        var bob = Guid.NewGuid();

        foreach (var characterId in new[] { owned.Id.Value, Guid.NewGuid() })
        {
            var response = await client.GetAsync(ProfileRoute(bob, characterId));

            response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
            (await response.Content.ReadFromJsonAsync<ProblemDetails>()).ShouldNotBeNull().Title.ShouldBe("Character.NotFound");
        }
    }

    /// <summary>Sends an empty user identifier to both endpoints.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task EmptyUserIsAValidationProblem()
    {
        using var client = WebsiteClient();

        foreach (var route in new[] { CharactersRoute(Guid.Empty), ProfileRoute(Guid.Empty, Guid.NewGuid()) })
        {
            var response = await client.GetAsync(route);

            response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            var problem = (await response.Content.ReadFromJsonAsync<ValidationProblemDetails>()).ShouldNotBeNull();
            problem.Errors.Keys.ShouldContain(nameof(GetCharacterProfileQuery.UserId));
        }
    }

    /// <summary>Reads the OpenAPI document and checks the endpoints and their transport records.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task OpenApiDocumentDescribesTheEndpoints()
    {
        using var client = _api.CreateClient();

        var document = await client.GetStringAsync("/openapi/v1.json");

        document.ShouldContain("/internal/users/{userId}/characters");
        document.ShouldContain("/internal/users/{userId}/characters/{characterId}");
        document.ShouldContain(nameof(CharacterSummary));
        document.ShouldContain(nameof(CharacterProfile));
    }
    #endregion Tests

    #region Private Helpers
    /// <summary>Builds the characters route of a player.</summary>
    /// <param name="userId">The player.</param>
    /// <returns>The relative URL.</returns>
    private static string CharactersRoute(Guid userId) =>
        CharacterProfileEndpoints.CharactersRoute.Replace("{userId:guid}", userId.ToString(), StringComparison.Ordinal);

    /// <summary>Builds the profile route of a player's character.</summary>
    /// <param name="userId">The player.</param>
    /// <param name="characterId">The character.</param>
    /// <returns>The relative URL.</returns>
    private static string ProfileRoute(Guid userId, Guid characterId) => $"{CharactersRoute(userId)}/{characterId}";

    /// <summary>Imports a level 80 Icecrown death knight whose name no other test uses.</summary>
    /// <returns>The character.</returns>
    private static Character NewCharacter()
    {
        var name = new string([.. Enumerable.Range(0, 10).Select(_ => (char)('a' + Random.Shared.Next(26)))]);
        return Character.Import(WarmaneRealm.Icecrown, CharacterName.Create(name), WowClass.DeathKnight, WowRace.Human, Faction.Alliance, 80);
    }

    /// <summary>Builds a character the player owns, with a loadout, a profession and a current raid save.</summary>
    /// <param name="userId">The owner.</param>
    /// <returns>The character.</returns>
    private static Character OwnedCharacter(Guid userId)
    {
        var observed = DateTimeOffset.UtcNow.AddHours(-1);
        var character = NewCharacter();
        character.RequestClaim(new UserId(userId), observed).IsSuccess.ShouldBeTrue();
        character.ApproveClaim(new UserId(userId), observed).IsSuccess.ShouldBeTrue();
        character.SynchronizeLoadout(
            null,
            "Frost DPS",
            CharacterRole.MeleeDamage,
            true,
            new GearScore(5712),
            new TalentConfiguration("Frost", 0, 53, 18, "0-53-18", [], []),
            new CombatStats(2100, 400, 4500, 50, 60, 42000, 30000, 5000, 0, 10, 1.2345m, 50, 12.5m, 30, 3.25m, 150, 26.5m, 26.5m, 0, 0m, 540, 15.2m, 18.75m, 0m, 0),
            [new GearItem(EquipmentSlot.MainHand, 50737, null, "|Hitem:50737|h[Havoc's Call]|h", 264)],
            CharacterDataSource.WowAddon,
            observed);
        character.SynchronizeProfessions([new Profession("Blacksmithing", 450, 450)], observed).ShouldBeTrue();
        character.RecordCompleteRaidSaveScan(
            [new RaidLockout(RaidInstance.IcecrownCitadel, RaidDifficulty.TwentyFivePlayerHeroic, "43127", DateTimeOffset.UtcNow.AddDays(2), false)],
            observed).IsSuccess.ShouldBeTrue();
        return character;
    }

    /// <summary>Adds characters through the API's own repository and unit of work.</summary>
    /// <param name="characters">The new characters.</param>
    /// <returns>A task that completes when they are saved.</returns>
    private async Task SaveAsync(params Character[] characters)
    {
        await using var scope = _api.Services.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<ICharacterRepository>();
        foreach (var character in characters)
        {
            await repository.AddAsync(character);
        }

        var saved = await scope.ServiceProvider.GetRequiredService<DomainUnitOfWork>().SaveChangesAsync();
        saved.IsSuccess.ShouldBeTrue(saved.IsFailure ? saved.Error.Message : null);
    }

    /// <summary>Creates a client that presents the website key, as the website server does.</summary>
    /// <returns>The HTTP client.</returns>
    private HttpClient WebsiteClient()
    {
        var client = _api.CreateClient();
        client.DefaultRequestHeaders.Add(WebsiteServiceDefaults.HeaderName, ApiFactory.WebsiteServiceKey);
        return client;
    }
    #endregion Private Helpers
}
