using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;
using RaidManager.Application.Features.Characters.Queries.GetPendingCharacterClaims;
using RaidManager.Domain.Features.Characters.Aggregates;
using RaidManager.Domain.Features.Characters.Enums;
using RaidManager.Domain.Features.Characters.Repositories;
using RaidManager.Domain.Features.Characters.ValueObjects;
using RaidManager.Domain.Features.Shared.Enums;
using RaidManager.Domain.Features.Shared.Identifiers;
using RaidManager.Persistence.EntityFrameworkCore.Tests.Support;
using DomainUnitOfWork = Pivot.Framework.Domain.Repositories.IUnitOfWork;

namespace RaidManager.Persistence.EntityFrameworkCore.Tests.Features.Characters;

/// <summary>Verifies the pending-claims query against SQL Server, sent through MediatR as a host would.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-09-30<br/>
/// Purpose: Proves the read-only query returns exactly the player's pending and conflicted claims, oldest first.
/// </remarks>
[Collection(SqlServerTestGroup.Name)]
public sealed class PendingCharacterClaimsQueryTests
{
    #region Fields
    /// <summary>Stores the SQL Server fixture.</summary>
    private readonly SqlServerFixture _database;
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="PendingCharacterClaimsQueryTests"/> class.</summary>
    /// <param name="database">The SQL Server fixture.</param>
    public PendingCharacterClaimsQueryTests(SqlServerFixture database)
    {
        _database = database;
    }
    #endregion Constructors

    #region Tests
    /// <summary>Lists only the player's pending and conflicted claims, oldest request first.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task QueryReturnsOnlyThePlayersClaimsAwaitingADecisionOldestFirst()
    {
        var alice = new UserId(Guid.NewGuid());
        var bob = new UserId(Guid.NewGuid());
        var earlier = DateTimeOffset.UtcNow.AddHours(-2);
        var later = DateTimeOffset.UtcNow.AddHours(-1);

        var pending = NewCharacter("Pendingone");
        pending.RequestClaim(alice, later).IsSuccess.ShouldBeTrue();

        var conflicted = NewCharacter("Conflicted");
        conflicted.RequestClaim(bob, earlier).IsSuccess.ShouldBeTrue();
        conflicted.ApproveClaim(bob, earlier).IsSuccess.ShouldBeTrue();
        conflicted.RequestClaim(alice, earlier).IsSuccess.ShouldBeTrue();

        var approved = NewCharacter("Approvedone");
        approved.RequestClaim(alice, earlier).IsSuccess.ShouldBeTrue();
        approved.ApproveClaim(alice, earlier).IsSuccess.ShouldBeTrue();

        var othersClaim = NewCharacter("Bobsonly");
        othersClaim.RequestClaim(bob, earlier).IsSuccess.ShouldBeTrue();

        await SaveNewAsync(pending, conflicted, approved, othersClaim);

        var result = await _database.SendAsync(new GetPendingCharacterClaimsQuery(alice.Value));

        result.IsSuccess.ShouldBeTrue();
        result.Value.Select(claim => (claim.Name, claim.ClaimState)).ShouldBe(
        [
            ("Conflicted", CharacterClaimState.Conflict),
            ("Pendingone", CharacterClaimState.Pending),
        ]);
        var first = result.Value[0];
        first.CharacterId.ShouldBe(conflicted.Id.Value);
        first.Realm.ShouldBe(WarmaneRealm.Icecrown);
        first.Class.ShouldBe(WowClass.Mage);
        first.Level.ShouldBe(80);
        first.RequestedAtUtc.ShouldBe(earlier);
    }
    #endregion Tests

    #region Private Helpers
    /// <summary>Imports a level 80 human mage on Icecrown.</summary>
    /// <param name="name">The character name.</param>
    /// <returns>The imported character.</returns>
    private static Character NewCharacter(string name) =>
        Character.Import(WarmaneRealm.Icecrown, CharacterName.Create(name), WowClass.Mage, WowRace.Human, Faction.Alliance, 80);

    /// <summary>Adds new characters and commits them through the unit of work in one scope.</summary>
    /// <param name="characters">The characters to save.</param>
    /// <returns>A task that completes when the characters are committed.</returns>
    private async Task SaveNewAsync(params Character[] characters)
    {
        await using var scope = _database.Services.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<ICharacterRepository>();
        foreach (var character in characters)
        {
            await repository.AddAsync(character);
        }

        var saved = await scope.ServiceProvider.GetRequiredService<DomainUnitOfWork>().SaveChangesAsync();
        saved.IsSuccess.ShouldBeTrue(saved.IsFailure ? saved.Error.Message : null);
    }
    #endregion Private Helpers
}
