using Microsoft.Extensions.DependencyInjection;
using Pivot.Framework.Domain.Shared;
using Shouldly;
using Xunit;
using RaidManager.Application.Features.Characters.Queries.GetCharacterProfile;
using RaidManager.Application.Features.Characters.Queries.GetMyCharacters;
using RaidManager.Domain.Features.Characters.Aggregates;
using RaidManager.Domain.Features.Characters.Enums;
using RaidManager.Domain.Features.Characters.Errors;
using RaidManager.Domain.Features.Characters.Repositories;
using RaidManager.Domain.Features.Characters.ValueObjects;
using RaidManager.Domain.Features.Shared.Enums;
using RaidManager.Domain.Features.Shared.Identifiers;
using RaidManager.Persistence.EntityFrameworkCore.Tests.Support;
using DomainUnitOfWork = Pivot.Framework.Domain.Repositories.IUnitOfWork;

namespace RaidManager.Persistence.EntityFrameworkCore.Tests.Features.Characters;

/// <summary>Verifies the characters list and profile queries against PostgreSQL, sent through MediatR as a host would.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-05<br/>
/// Purpose: Proves the read-only profile queries of story #19 show only the owner's characters (owner decision on #19,
/// 2026-10-05), count and list only raid saves that haven't reset, and round-trip professions and the visibility.
/// </remarks>
[Collection(PostgreSqlTestGroup.Name)]
public sealed class CharacterProfileQueriesTests
{
    #region Fields
    /// <summary>Stores the PostgreSQL fixture.</summary>
    private readonly PostgreSqlFixture _database;

    /// <summary>Stores a suffix that keeps this run's character names apart from earlier runs on the shared database.</summary>
    private readonly string _suffix = string.Concat(Enumerable.Range(0, 5).Select(_ => (char)('a' + Random.Shared.Next(26))));

    /// <summary>Stores a whole-second instant: PostgreSQL keeps timestamps to the microsecond, not to .NET's 100 nanoseconds.</summary>
    private readonly DateTimeOffset _now = DateTimeOffset.FromUnixTimeSeconds(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="CharacterProfileQueriesTests"/> class.</summary>
    /// <param name="database">The PostgreSQL fixture.</param>
    public CharacterProfileQueriesTests(PostgreSqlFixture database)
    {
        _database = database;
    }
    #endregion Constructors

    #region Tests
    /// <summary>Lists only the player's approved characters, by realm and name, with what board 1 shows.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task ListReturnsOnlyThePlayersApprovedCharactersWithTheirPrimaryLoadoutAndCurrentSaves()
    {
        var alice = new UserId(Guid.NewGuid());
        var bob = new UserId(Guid.NewGuid());

        var arthas = Owned(NewCharacter("Arthas", WarmaneRealm.Icecrown), alice);
        SynchronizeLoadouts(arthas);
        arthas.RecordCompleteRaidSaveScan(
            [Save(RaidInstance.IcecrownCitadel, _now.AddDays(2)), Save(RaidInstance.VaultOfArchavon, _now.AddDays(2)), Save(RaidInstance.Naxxramas, _now.AddDays(-1))],
            _now.AddHours(-1)).IsSuccess.ShouldBeTrue();
        arthas.RefreshArmoryIdentity(WowClass.DeathKnight, WowRace.Human, Faction.Alliance, 80, "Citadel Vanguard", _now.AddDays(-1));

        var jaina = Owned(NewCharacter("Jaina", WarmaneRealm.Lordaeron), alice);
        var thrall = Owned(NewCharacter("Thrall", WarmaneRealm.Icecrown), alice);
        var pending = NewCharacter("Pending", WarmaneRealm.Icecrown);
        pending.RequestClaim(alice, _now).IsSuccess.ShouldBeTrue();
        var bobs = Owned(NewCharacter("Bobs", WarmaneRealm.Icecrown), bob);

        await SaveNewAsync(arthas, jaina, thrall, pending, bobs);

        var result = await _database.SendAsync(new GetMyCharactersQuery(alice.Value));

        result.IsSuccess.ShouldBeTrue();
        result.Value.Select(character => character.CharacterId).ShouldBe([arthas.Id.Value, thrall.Id.Value, jaina.Id.Value]);
        var first = result.Value[0];
        first.CharacterId.ShouldBe(arthas.Id.Value);
        first.Realm.ShouldBe(WarmaneRealm.Icecrown);
        first.Class.ShouldBe(WowClass.DeathKnight);
        first.Level.ShouldBe(80);
        first.PrimaryLoadout.ShouldBe(new LoadoutSummaryResponse("Frost DPS", CharacterRole.MeleeDamage, 5712));
        first.CurrentRaidSaveCount.ShouldBe(2);
        first.LastSynchronizedAtUtc.ShouldBe(_now.AddHours(-1));
        result.Value[1].PrimaryLoadout.ShouldBeNull();
        result.Value[1].CurrentRaidSaveCount.ShouldBe(0);
        result.Value[1].LastSynchronizedAtUtc.ShouldBeNull();
    }

    /// <summary>Returns the owner's full profile: sources, professions in order, loadouts primary first, current saves.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task ProfileReturnsWhatBoardTwoShowsToTheOwner()
    {
        var alice = new UserId(Guid.NewGuid());
        var arthas = Owned(NewCharacter("Arthas", WarmaneRealm.Icecrown), alice);
        SynchronizeLoadouts(arthas);
        arthas.SynchronizeProfessions([new Profession("Mining", 450, 450), new Profession("Blacksmithing", 445, 450)], _now.AddHours(-2)).ShouldBeTrue();
        arthas.RecordCompleteRaidSaveScan(
            [Save(RaidInstance.IcecrownCitadel, _now.AddDays(2)), Save(RaidInstance.Naxxramas, _now.AddDays(-1))],
            _now.AddHours(-1)).IsSuccess.ShouldBeTrue();
        arthas.RefreshArmoryIdentity(WowClass.DeathKnight, WowRace.Human, Faction.Alliance, 80, "Citadel Vanguard", _now.AddDays(-1));
        await SaveNewAsync(arthas);

        var result = await _database.SendAsync(new GetCharacterProfileQuery(alice.Value, arthas.Id.Value));

        result.IsSuccess.ShouldBeTrue();
        var profile = result.Value;
        profile.Name.ShouldBe(arthas.Name.Value);
        profile.Class.ShouldBe(WowClass.DeathKnight);
        profile.Faction.ShouldBe(Faction.Alliance);
        profile.GuildName.ShouldBe("Citadel Vanguard");
        profile.Visibility.ShouldBe(CharacterVisibility.Community);
        profile.Sync.ShouldBe(new ProfileSyncResponse(_now.AddHours(-1), _now.AddDays(-1), _now.AddHours(-1)));
        profile.Professions.ShouldBe([new ProfileProfessionResponse("Mining", 450, 450), new ProfileProfessionResponse("Blacksmithing", 445, 450)]);
        profile.Loadouts.Select(loadout => (loadout.Name, loadout.IsPrimary)).ShouldBe([("Frost DPS", true), ("Blood Tank", false)]);
        var frost = profile.Loadouts[0];
        frost.Talents.ShouldBe("0/53/18");
        frost.Source.ShouldBe(CharacterDataSource.WowAddon);
        frost.Gear.Select(item => item.Slot).ShouldBe([EquipmentSlot.Head, EquipmentSlot.MainHand]);
        frost.Gear[1].ItemLink.ShouldContain("[Havoc's Call]");
        profile.RaidSaves.ShouldHaveSingleItem().Instance.ShouldBe(RaidInstance.IcecrownCitadel);
    }

    /// <summary>Replaces the stored professions when a later read is recorded on a reloaded character.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task ALaterReadReplacesTheStoredProfessions()
    {
        var alice = new UserId(Guid.NewGuid());
        var arthas = Owned(NewCharacter("Arthas", WarmaneRealm.Icecrown), alice);
        arthas.SynchronizeProfessions([new Profession("Mining", 450, 450), new Profession("Blacksmithing", 445, 450)], _now.AddHours(-2)).ShouldBeTrue();
        await SaveNewAsync(arthas);

        await using (var scope = _database.Services.CreateAsyncScope())
        {
            var reloaded = (await scope.ServiceProvider.GetRequiredService<ICharacterRepository>().FindByIdAsync(arthas.Id)).ShouldNotBeNull();
            reloaded.SynchronizeProfessions([new Profession("Jewelcrafting", 440, 450), new Profession("Mining", 450, 450)], _now.AddHours(-1)).ShouldBeTrue();
            var saved = await scope.ServiceProvider.GetRequiredService<DomainUnitOfWork>().SaveChangesAsync();
            saved.IsSuccess.ShouldBeTrue(saved.IsFailure ? saved.Error.Message : null);
        }

        var result = await _database.SendAsync(new GetCharacterProfileQuery(alice.Value, arthas.Id.Value));

        result.Value.Professions.ShouldBe([new ProfileProfessionResponse("Jewelcrafting", 440, 450), new ProfileProfessionResponse("Mining", 450, 450)]);
        result.Value.Sync.AddonAtUtc.ShouldBe(_now.AddHours(-1));
    }

    /// <summary>Answers not found to anyone but the owner, and for a character no one owns yet.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task ProfileIsNotFoundForAnyoneButTheOwner()
    {
        var alice = new UserId(Guid.NewGuid());
        var bob = new UserId(Guid.NewGuid());
        var arthas = Owned(NewCharacter("Arthas", WarmaneRealm.Icecrown), alice);
        var pending = NewCharacter("Pending", WarmaneRealm.Icecrown);
        pending.RequestClaim(bob, _now).IsSuccess.ShouldBeTrue();
        await SaveNewAsync(arthas, pending);

        foreach (var (user, character) in new[] { (bob, arthas), (bob, pending) })
        {
            var result = await _database.SendAsync(new GetCharacterProfileQuery(user.Value, character.Id.Value));

            result.IsFailure.ShouldBeTrue();
            result.Error.ShouldBe(CharacterErrors.NotFound);
            result.ResultExceptionType.ShouldBe(ResultExceptionType.NotFound);
        }
    }
    #endregion Tests

    #region Private Helpers

    /// <summary>Builds a raid save in 25-player mode.</summary>
    /// <param name="instance">The raid.</param>
    /// <param name="resetsAtUtc">When it resets.</param>
    /// <returns>The raid save.</returns>
    private static RaidLockout Save(RaidInstance instance, DateTimeOffset resetsAtUtc) =>
        new(instance, RaidDifficulty.TwentyFivePlayer, "43127", resetsAtUtc, false);

    /// <summary>Imports a level 80 human death knight, its name made unique with the run's suffix.</summary>
    /// <param name="name">The start of the character name, at most 7 letters.</param>
    /// <param name="realm">The realm.</param>
    /// <returns>The imported character.</returns>
    private Character NewCharacter(string name, WarmaneRealm realm) =>
        Character.Import(realm, CharacterName.Create(name + _suffix), WowClass.DeathKnight, WowRace.Human, Faction.Alliance, 80);

    /// <summary>Gives a character to a player through an approved claim.</summary>
    /// <param name="character">The character.</param>
    /// <param name="owner">The player.</param>
    /// <returns>The same character.</returns>
    private Character Owned(Character character, UserId owner)
    {
        character.RequestClaim(owner, _now.AddDays(-2)).IsSuccess.ShouldBeTrue();
        character.ApproveClaim(owner, _now.AddDays(-2)).IsSuccess.ShouldBeTrue();
        return character;
    }

    /// <summary>Synchronizes a Frost DPS primary loadout and a Blood Tank loadout.</summary>
    /// <param name="character">The character.</param>
    private void SynchronizeLoadouts(Character character)
    {
        // Each loadout owns its statistics, so each gets its own instance.
        static CombatStats Stats() => new(2100, 400, 4500, 50, 60, 42000, 30000, 5000, 0, 10, 1.2345m, 50, 12.5m, 30, 3.25m, 150, 26.5m, 26.5m, 0, 0m, 540, 15.2m, 18.75m, 0m, 0);

        character.SynchronizeLoadout(
            null,
            "Blood Tank",
            CharacterRole.Tank,
            false,
            new GearScore(5480),
            new TalentConfiguration("Blood", 51, 10, 10, "51-10-10", [], []),
            Stats(),
            [new GearItem(EquipmentSlot.Head, 51133, null, "|Hitem:51133|h[Sanctified Scourgelord Faceguard]|h", 264)],
            CharacterDataSource.WowAddon,
            _now.AddHours(-3));
        character.SynchronizeLoadout(
            null,
            "Frost DPS",
            CharacterRole.MeleeDamage,
            true,
            new GearScore(5712),
            new TalentConfiguration("Frost", 0, 53, 18, "0-53-18", [], []),
            Stats(),
            [
                new GearItem(EquipmentSlot.MainHand, 50737, null, "|Hitem:50737|h[Havoc's Call]|h", 264),
                new GearItem(EquipmentSlot.Head, 51127, null, "|Hitem:51127|h[Sanctified Scourgelord Helmet]|h", 264),
            ],
            CharacterDataSource.WowAddon,
            _now.AddHours(-3));
    }

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
