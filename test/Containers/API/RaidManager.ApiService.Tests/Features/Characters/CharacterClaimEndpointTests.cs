using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;
using RaidManager.ApiService.Features.Characters;
using RaidManager.ApiService.Features.Shared.Authentication;
using RaidManager.ApiService.Tests.Support;
using RaidManager.Application.Features.Characters.Queries.GetPendingCharacterClaims;
using RaidManager.Domain.Features.Characters.Aggregates;
using RaidManager.Domain.Features.Characters.Enums;
using RaidManager.Domain.Features.Characters.Repositories;
using RaidManager.Domain.Features.Characters.ValueObjects;
using RaidManager.Domain.Features.Shared.Enums;
using RaidManager.Domain.Features.Shared.Identifiers;
using DomainUnitOfWork = Pivot.Framework.Domain.Repositories.IUnitOfWork;

namespace RaidManager.ApiService.Tests.Features.Characters;

/// <summary>Verifies the website-only character claim review endpoints through HTTP and PostgreSQL.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-09-30<br/>
/// Purpose: Proves the website key guards the endpoints, that only a player's undecided claims are listed, and that
/// approve and reject map every domain outcome to the right status, including a kept conflict review.
/// </remarks>
[Collection(ApiTestGroup.Name)]
public sealed class CharacterClaimEndpointTests
{
    #region Fields
    /// <summary>Stores the API factory.</summary>
    private readonly ApiFactory _api;
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="CharacterClaimEndpointTests"/> class.</summary>
    /// <param name="api">The API factory.</param>
    public CharacterClaimEndpointTests(ApiFactory api)
    {
        _api = api;
    }
    #endregion Constructors

    #region Tests
    /// <summary>Lists claims without the website key.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task RequestWithoutTheWebsiteKeyIsUnauthorized()
    {
        using var client = _api.CreateClient();

        var response = await client.GetAsync(PendingRoute(Guid.NewGuid()));

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    /// <summary>Lists a player's claims awaiting a decision, with enum names and the oldest first.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task PendingClaimsListOnlyUndecidedClaimsOldestFirst()
    {
        var alice = Guid.NewGuid();
        var bob = Guid.NewGuid();
        var earlier = DateTimeOffset.UtcNow.AddHours(-2);
        var pending = NewCharacter();
        pending.RequestClaim(new UserId(alice), DateTimeOffset.UtcNow.AddHours(-1)).IsSuccess.ShouldBeTrue();
        var conflicted = NewCharacter();
        conflicted.RequestClaim(new UserId(bob), earlier).IsSuccess.ShouldBeTrue();
        conflicted.ApproveClaim(new UserId(bob), earlier).IsSuccess.ShouldBeTrue();
        conflicted.RequestClaim(new UserId(alice), earlier).IsSuccess.ShouldBeTrue();
        var approved = NewCharacter();
        approved.RequestClaim(new UserId(alice), earlier).IsSuccess.ShouldBeTrue();
        approved.ApproveClaim(new UserId(alice), earlier).IsSuccess.ShouldBeTrue();
        await SaveAsync(pending, conflicted, approved);
        using var client = WebsiteClient();

        var response = await client.GetAsync(PendingRoute(alice));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var claims = (await response.Content.ReadFromJsonAsync<List<PendingCharacterClaim>>()).ShouldNotBeNull();
        claims.Select(claim => (claim.CharacterId, claim.ClaimState)).ShouldBe(
        [
            (conflicted.Id.Value, nameof(CharacterClaimState.Conflict)),
            (pending.Id.Value, nameof(CharacterClaimState.Pending)),
        ]);
        var first = claims[0];
        (first.Realm, first.Class, first.Race, first.Level).ShouldBe(("Icecrown", "Mage", "Human", 80));
    }

    /// <summary>Lists claims for a player who has none.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task PlayerWithoutClaimsGetsAnEmptyList()
    {
        using var client = WebsiteClient();

        var claims = await client.GetFromJsonAsync<List<PendingCharacterClaim>>(PendingRoute(Guid.NewGuid()));

        claims.ShouldNotBeNull().ShouldBeEmpty();
    }

    /// <summary>Approves a pending claim, then approves it again.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task ApprovingRemovesTheClaimFromTheListAndASecondDecisionConflicts()
    {
        var alice = Guid.NewGuid();
        var character = await ClaimedCharacterAsync(alice);
        using var client = WebsiteClient();

        var first = await client.PostAsync(DecisionRoute(alice, character, "approve"), null);
        var second = await client.PostAsync(DecisionRoute(alice, character, "approve"), null);

        first.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        second.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await ProblemAsync(second)).Title.ShouldBe("Character.Claim.NotPending");
        (await PendingAsync(client, alice)).ShouldBeEmpty();
    }

    /// <summary>Rejects a pending claim.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task RejectingRemovesTheClaimFromTheList()
    {
        var alice = Guid.NewGuid();
        var character = await ClaimedCharacterAsync(alice);
        using var client = WebsiteClient();

        var response = await client.PostAsync(DecisionRoute(alice, character, "reject"), null);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await PendingAsync(client, alice)).ShouldBeEmpty();
    }

    /// <summary>Approves a claim on a character that doesn't exist.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task UnknownCharacterIsNotFound()
    {
        using var client = WebsiteClient();

        var response = await client.PostAsync(DecisionRoute(Guid.NewGuid(), Guid.NewGuid(), "reject"), null);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await ProblemAsync(response)).Title.ShouldBe("Character.NotFound");
    }

    /// <summary>Approves a pending claim on a character that another player approved first.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task ApprovingACharacterOwnedByAnotherPlayerKeepsItInConflictReview()
    {
        var alice = Guid.NewGuid();
        var bob = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var character = NewCharacter();

        // Both claim the character while nobody owns it, then Bob approves his claim first.
        character.RequestClaim(new UserId(alice), now).IsSuccess.ShouldBeTrue();
        character.RequestClaim(new UserId(bob), now).IsSuccess.ShouldBeTrue();
        character.ApproveClaim(new UserId(bob), now).IsSuccess.ShouldBeTrue();
        await SaveAsync(character);
        using var client = WebsiteClient();

        var response = await client.PostAsync(DecisionRoute(alice, character.Id.Value, "approve"), null);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await ProblemAsync(response)).Title.ShouldBe("Character.Claim.OwnedByAnotherUser");
        var claim = (await PendingAsync(client, alice)).ShouldHaveSingleItem();
        claim.CharacterId.ShouldBe(character.Id.Value);
        claim.ClaimState.ShouldBe(nameof(CharacterClaimState.Conflict));
    }

    /// <summary>Sends an empty user identifier.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task EmptyUserIsAValidationProblem()
    {
        using var client = WebsiteClient();

        var response = await client.GetAsync(PendingRoute(Guid.Empty));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var problem = (await response.Content.ReadFromJsonAsync<ValidationProblemDetails>()).ShouldNotBeNull();
        problem.Errors.Keys.ShouldContain(nameof(GetPendingCharacterClaimsQuery.UserId));
    }

    /// <summary>Reads the OpenAPI document and checks the endpoints and their transport record.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task OpenApiDocumentDescribesTheEndpoints()
    {
        using var client = _api.CreateClient();

        var document = await client.GetStringAsync("/openapi/v1.json");

        document.ShouldContain("/internal/users/{userId}/character-claims/pending");
        document.ShouldContain("/internal/users/{userId}/character-claims/{characterId}/approve");
        document.ShouldContain("/internal/users/{userId}/character-claims/{characterId}/reject");
        document.ShouldContain(nameof(PendingCharacterClaim));
    }
    #endregion Tests

    #region Private Helpers
    /// <summary>Builds the pending-claims route of a player.</summary>
    /// <param name="userId">The player.</param>
    /// <returns>The relative URL.</returns>
    private static string PendingRoute(Guid userId) =>
        CharacterClaimEndpoints.ClaimsRoute.Replace("{userId:guid}", userId.ToString(), StringComparison.Ordinal) + "/pending";

    /// <summary>Builds the route of a decision on a player's claim.</summary>
    /// <param name="userId">The player.</param>
    /// <param name="characterId">The claimed character.</param>
    /// <param name="decision"><c>approve</c> or <c>reject</c>.</param>
    /// <returns>The relative URL.</returns>
    private static string DecisionRoute(Guid userId, Guid characterId, string decision) =>
        CharacterClaimEndpoints.ClaimsRoute.Replace("{userId:guid}", userId.ToString(), StringComparison.Ordinal) + $"/{characterId}/{decision}";

    /// <summary>Imports a level 80 Icecrown mage whose name no other test uses.</summary>
    /// <returns>The character.</returns>
    private static Character NewCharacter()
    {
        var name = new string([.. Enumerable.Range(0, 10).Select(_ => (char)('a' + Random.Shared.Next(26)))]);
        return Character.Import(WarmaneRealm.Icecrown, CharacterName.Create(name), WowClass.Mage, WowRace.Human, Faction.Alliance, 80);
    }

    /// <summary>Reads a player's pending claims through the API.</summary>
    /// <param name="client">The website client.</param>
    /// <param name="userId">The player.</param>
    /// <returns>The claims.</returns>
    private static async Task<List<PendingCharacterClaim>> PendingAsync(HttpClient client, Guid userId) =>
        (await client.GetFromJsonAsync<List<PendingCharacterClaim>>(PendingRoute(userId))).ShouldNotBeNull();

    /// <summary>Reads the ProblemDetails of a failed response.</summary>
    /// <param name="response">The response.</param>
    /// <returns>The problem.</returns>
    private static async Task<ProblemDetails> ProblemAsync(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<ProblemDetails>()).ShouldNotBeNull();

    /// <summary>Saves a new character with a pending claim by the player.</summary>
    /// <param name="userId">The player.</param>
    /// <returns>The character identifier.</returns>
    private async Task<Guid> ClaimedCharacterAsync(Guid userId)
    {
        var character = NewCharacter();
        character.RequestClaim(new UserId(userId), DateTimeOffset.UtcNow).IsSuccess.ShouldBeTrue();
        await SaveAsync(character);
        return character.Id.Value;
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
