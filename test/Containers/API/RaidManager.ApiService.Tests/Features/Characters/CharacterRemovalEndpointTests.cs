using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;
using RaidManager.ApiService.Features.Characters;
using RaidManager.ApiService.Features.Shared.Authentication;
using RaidManager.ApiService.Tests.Support;
using RaidManager.Application.Features.Characters.Commands.RemoveMyCharacters;
using RaidManager.Domain.Features.Characters.Aggregates;
using RaidManager.Domain.Features.Characters.Enums;
using RaidManager.Domain.Features.Characters.Repositories;
using RaidManager.Domain.Features.Characters.ValueObjects;
using RaidManager.Domain.Features.Shared.Enums;
using RaidManager.Domain.Features.Shared.Identifiers;
using DomainUnitOfWork = Pivot.Framework.Domain.Repositories.IUnitOfWork;

namespace RaidManager.ApiService.Tests.Features.Characters;

/// <summary>Verifies the dev and test reset that removes a player's characters, on a real database.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-09<br/>
/// Purpose: Covers story #597's API: the website's key is required, the player's own characters go with their data,
/// another player's character keeps its owner, and a removed character can be imported again under its name.
/// </remarks>
[Collection(ApiTestGroup.Name)]
public sealed class CharacterRemovalEndpointTests
{
    #region Fields
    /// <summary>Stores the API on Development, where the removal is mapped.</summary>
    private readonly ApiFactory _api;
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="CharacterRemovalEndpointTests"/> class.</summary>
    /// <param name="api">The API.</param>
    public CharacterRemovalEndpointTests(ApiFactory api)
    {
        _api = api;
    }
    #endregion Constructors

    #region Tests
    /// <summary>The removal needs the website's service key.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task RequestWithoutTheWebsiteKeyIsUnauthorized()
    {
        using var client = _api.CreateClient();

        var response = await client.DeleteAsync(CharactersRoute(Guid.NewGuid()));

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    /// <summary>The player's own and claimed characters go; another player's character keeps its owner.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task RemovalDeletesOnlyWhatIsThePlayersAlone()
    {
        var alice = Guid.NewGuid();
        var bob = Guid.NewGuid();
        var owned = Owned(NewCharacter(), alice);
        var claimed = NewCharacter();
        claimed.RequestClaim(new UserId(alice), DateTimeOffset.UtcNow).IsSuccess.ShouldBeTrue();
        var bobs = Owned(NewCharacter(), bob);
        bobs.RequestClaim(new UserId(alice), DateTimeOffset.UtcNow).IsSuccess.ShouldBeTrue();
        await SaveAsync(owned, claimed, bobs);
        using var client = WebsiteClient();

        var response = await client.DeleteAsync(CharactersRoute(alice));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadFromJsonAsync<CharacterRemoval>()).ShouldBe(new CharacterRemoval(3));
        (await FindAsync(owned.Id)).ShouldBeNull();
        (await FindAsync(claimed.Id)).ShouldBeNull();
        var kept = (await FindAsync(bobs.Id)).ShouldNotBeNull();
        kept.OwnerId.ShouldBe(new UserId(bob));
        kept.Claims.ShouldNotContain(claim => claim.RequestedByUserId == new UserId(alice));
    }

    /// <summary>A removed character can be imported again under its realm and name, as the next sync does.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task ARemovedCharacterCanBeImportedAgain()
    {
        var alice = Guid.NewGuid();
        var name = RandomName();
        await SaveAsync(Owned(NewCharacter(name), alice));
        using var client = WebsiteClient();
        (await client.DeleteAsync(CharactersRoute(alice))).EnsureSuccessStatusCode();

        var again = NewCharacter(name);
        again.RequestClaim(new UserId(alice), DateTimeOffset.UtcNow).IsSuccess.ShouldBeTrue();
        await SaveAsync(again);

        (await FindAsync(again.Id)).ShouldNotBeNull().Claims.ShouldHaveSingleItem().State.ShouldBe(CharacterClaimState.Pending);
    }

    /// <summary>An empty player is a validation problem.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task EmptyUserIsAValidationProblem()
    {
        using var client = WebsiteClient();

        var response = await client.DeleteAsync(CharactersRoute(Guid.Empty));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.Content.ReadFromJsonAsync<ValidationProblemDetails>()).ShouldNotBeNull()
            .Errors.Keys.ShouldContain(nameof(RemoveMyCharactersCommand.UserId));
    }
    #endregion Tests

    #region Private Helpers
    /// <summary>Builds the characters route of a player.</summary>
    /// <param name="userId">The player.</param>
    /// <returns>The route.</returns>
    private static string CharactersRoute(Guid userId) =>
        CharacterProfileEndpoints.CharactersRoute.Replace("{userId:guid}", userId.ToString(), StringComparison.Ordinal);

    /// <summary>Builds a random character name, so tests sharing the database don't collide.</summary>
    /// <returns>The name.</returns>
    private static string RandomName() => new([.. Enumerable.Range(0, 10).Select(_ => (char)('a' + Random.Shared.Next(26)))]);

    /// <summary>Imports a character on Icecrown.</summary>
    /// <param name="name">The name, random by default.</param>
    /// <returns>The character.</returns>
    private static Character NewCharacter(string? name = null) =>
        Character.Import(WarmaneRealm.Icecrown, CharacterName.Create(name ?? RandomName()), WowClass.Paladin, WowRace.Human, Faction.Alliance, 80);

    /// <summary>Makes a player the owner of a character through an approved claim.</summary>
    /// <param name="character">The character.</param>
    /// <param name="userId">The player.</param>
    /// <returns>The same character.</returns>
    private static Character Owned(Character character, Guid userId)
    {
        character.RequestClaim(new UserId(userId), DateTimeOffset.UtcNow).IsSuccess.ShouldBeTrue();
        character.ApproveClaim(new UserId(userId), DateTimeOffset.UtcNow).IsSuccess.ShouldBeTrue();
        return character;
    }

    /// <summary>Saves new characters in one commit.</summary>
    /// <param name="characters">The characters.</param>
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

    /// <summary>Finds a character in a new scope, as stored.</summary>
    /// <param name="characterId">The character.</param>
    /// <returns>The character, or <see langword="null"/> when it is gone.</returns>
    private async Task<Character?> FindAsync(CharacterId characterId)
    {
        await using var scope = _api.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<ICharacterRepository>().FindByIdAsync(characterId);
    }

    /// <summary>Creates a client with the website's service key.</summary>
    /// <returns>The client.</returns>
    private HttpClient WebsiteClient()
    {
        var client = _api.CreateClient();
        client.DefaultRequestHeaders.Add(WebsiteServiceDefaults.HeaderName, ApiFactory.WebsiteServiceKey);
        return client;
    }
    #endregion Private Helpers
}
