using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;
using RaidManager.Application.Features.Characters.Commands.ApproveCharacterClaim;
using RaidManager.Domain.Features.Characters.Aggregates;
using RaidManager.Domain.Features.Characters.Enums;
using RaidManager.Domain.Features.Characters.Repositories;
using RaidManager.Domain.Features.Characters.ValueObjects;
using RaidManager.Domain.Features.Shared.Enums;
using RaidManager.Domain.Features.Shared.Identifiers;
using RaidManager.Persistence.EntityFrameworkCore.Tests.Support;
using DomainUnitOfWork = Pivot.Framework.Domain.Repositories.IUnitOfWork;

namespace RaidManager.Persistence.EntityFrameworkCore.Tests.Features.Characters;

/// <summary>Verifies that the Character aggregate persists on PostgreSQL and that claim decisions commit with their events.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-09-30<br/>
/// Purpose: Proves the mapping round-trips every part of the aggregate, the unit of work writes the outbox, and the migrations match the model.
/// </remarks>
[Collection(PostgreSqlTestGroup.Name)]
public sealed class CharacterPersistenceTests
{
    #region Fields
    /// <summary>Stores the PostgreSQL fixture.</summary>
    private readonly PostgreSqlFixture _database;
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="CharacterPersistenceTests"/> class.</summary>
    /// <param name="database">The PostgreSQL fixture.</param>
    public CharacterPersistenceTests(PostgreSqlFixture database)
    {
        _database = database;
    }
    #endregion Constructors

    #region Tests
    /// <summary>Saves a fully populated character and reloads it in a new scope.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task SavedCharacterKeepsClaimsLoadoutsAndRaidSavesWhenReloaded()
    {
        var owner = new UserId(Guid.NewGuid());

        // Whole seconds: PostgreSQL keeps timestamps to the microsecond, not to .NET's 100 nanoseconds.
        var observedAtUtc = DateTimeOffset.FromUnixTimeSeconds(DateTimeOffset.UtcNow.ToUnixTimeSeconds()).AddMinutes(-5);
        var character = Character.Import(WarmaneRealm.Icecrown, CharacterName.Create("Roundtrip"), WowClass.DeathKnight, WowRace.Human, Faction.Alliance, 80);
        character.RequestClaim(owner, observedAtUtc).IsSuccess.ShouldBeTrue();
        character.ApproveClaim(owner, observedAtUtc).IsSuccess.ShouldBeTrue();
        var loadoutId = character.SynchronizeLoadout(
            null,
            "Frost tank",
            CharacterRole.Tank,
            true,
            new GearScore(5812),
            new TalentConfiguration("Frost", 0, 53, 18, "0-53-18", [43533, 43547], [43544]),
            new CombatStats(2100, 400, 4500, 50, 60, 42000, 30000, 5000, 0, 10, 1.2345m, 50, 12.5m, 30, 3.25m, 150, 26.5m, 26.5m, 0, 0m, 540, 15.2m, 18.75m, 0m, 0),
            [new GearItem(EquipmentSlot.Head, 51133, 64000, "|Hitem:51133|h[Sanctified Scourgelord Faceguard]|h", 264)],
            CharacterDataSource.WowAddon,
            observedAtUtc);
        character.RecordCompleteRaidSaveScan(
            [new RaidLockout(RaidInstance.IcecrownCitadel, RaidDifficulty.TwentyFivePlayer, "12345", observedAtUtc.AddDays(3), false)],
            observedAtUtc).IsSuccess.ShouldBeTrue();
        await SaveNewAsync(character);

        await using var scope = _database.Services.CreateAsyncScope();
        var reloaded = (await scope.ServiceProvider.GetRequiredService<ICharacterRepository>().FindByIdAsync(character.Id)).ShouldNotBeNull();
        reloaded.Name.Value.ShouldBe("Roundtrip");
        reloaded.Class.ShouldBe(WowClass.DeathKnight);
        reloaded.OwnerId.ShouldBe(owner);
        reloaded.Claims.ShouldHaveSingleItem().State.ShouldBe(CharacterClaimState.Approved);
        var loadout = reloaded.Loadouts.ShouldHaveSingleItem();
        loadout.Id.ShouldBe(loadoutId);
        loadout.GearScore.Value.ShouldBe(5812);
        loadout.TalentConfiguration.MajorGlyphIds.ShouldBe([43533, 43547]);
        loadout.TalentConfiguration.MinorGlyphIds.ShouldBe([43544]);
        loadout.Stats.HitPercent.ShouldBe(1.2345m);
        loadout.Stats.ParryPercent.ShouldBe(18.75m);
        loadout.GearItems.ShouldHaveSingleItem().ItemId.ShouldBe(51133);
        var lockout = reloaded.RaidLockouts.ShouldHaveSingleItem();
        lockout.Instance.ShouldBe(RaidInstance.IcecrownCitadel);
        lockout.ResetsAtUtc.ShouldBe(observedAtUtc.AddDays(3));
        reloaded.LastCompleteRaidSaveScanAtUtc.ShouldBe(observedAtUtc);
        reloaded.Audit.ShouldNotBeNull();
    }

    /// <summary>Approves a pending claim through MediatR with the real repository and unit of work.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task ApprovingAClaimThroughTheCommandPersistsTheOwnerAndRecordsTheEvent()
    {
        var player = new UserId(Guid.NewGuid());
        var character = Character.Import(WarmaneRealm.Lordaeron, CharacterName.Create("Approvable"), WowClass.Paladin, WowRace.Human, Faction.Alliance, 80);
        character.RequestClaim(player, DateTimeOffset.UtcNow).IsSuccess.ShouldBeTrue();
        await SaveNewAsync(character);

        var result = await _database.SendAsync(new ApproveCharacterClaimCommand(character.Id.Value, player.Value));

        result.IsSuccess.ShouldBeTrue(result.IsFailure ? $"{result.Error.Code}: {result.Error.Message}" : null);
        await using var scope = _database.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<RaidManagerDbContext>();
        var saved = await context.Characters.SingleAsync(candidate => candidate.Id == character.Id);
        saved.OwnerId.ShouldBe(player);
        saved.Claims.ShouldHaveSingleItem().State.ShouldBe(CharacterClaimState.Approved);
        var eventTypes = await context.OutboxMessages.Select(message => message.EventType).ToListAsync();
        eventTypes.ShouldContain(type => type != null && type.Contains(".CharacterClaimed,", StringComparison.Ordinal));
    }

    /// <summary>Confirms that the committed migrations describe the current model.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task MigrationsMatchTheModel()
    {
        await using var scope = _database.Services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<RaidManagerDbContext>().Database;
        database.HasPendingModelChanges().ShouldBeFalse();
        (await database.GetPendingMigrationsAsync()).ShouldBeEmpty();
    }
    #endregion Tests

    #region Private Helpers
    /// <summary>Adds a new character and commits it through the unit of work in its own scope.</summary>
    /// <param name="character">The character to save.</param>
    /// <returns>A task that completes when the character is committed.</returns>
    private async Task SaveNewAsync(Character character)
    {
        await using var scope = _database.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<ICharacterRepository>().AddAsync(character);
        var saved = await scope.ServiceProvider.GetRequiredService<DomainUnitOfWork>().SaveChangesAsync();
        saved.IsSuccess.ShouldBeTrue(saved.IsFailure ? saved.Error.Message : null);
    }
    #endregion Private Helpers
}
