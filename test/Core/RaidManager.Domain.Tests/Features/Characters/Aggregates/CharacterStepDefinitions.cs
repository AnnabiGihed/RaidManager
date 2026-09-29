using Reqnroll;
using Shouldly;
using RaidManager.Domain.Features.Characters.Aggregates;
using RaidManager.Domain.Features.Characters.Enums;
using RaidManager.Domain.Features.Shared.Enums;
using RaidManager.Domain.Features.Characters.Events;
using RaidManager.Domain.Features.Characters.ValueObjects;

namespace RaidManager.Domain.Tests.Features.Characters.Aggregates;

/// <summary>Defines business-readable step definitions for character synchronization behavior.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-09-29<br/>
/// Purpose: Verifies that loadout-specific raid readiness remains separate from character-wide lockouts.
/// </remarks>
[Binding]
[Scope(Feature = "Character synchronization")]
public sealed class CharacterStepDefinitions
{
    #region Fields
    /// <summary>Stores the character under test.</summary>
    private Character _character = null!;

    /// <summary>Stores the UTC instant shared by the synchronization scenario.</summary>
    private DateTimeOffset _synchronizedAtUtc;
    #endregion Fields

    #region Given Steps
    /// <summary>Creates a valid level 80 Human Paladin on Icecrown.</summary>
    [Given("a level 80 Human Paladin named Paladinlol on Icecrown")]
    public void GivenALevel80HumanPaladinNamedPaladinlolOnIcecrown()
    {
        _synchronizedAtUtc = DateTimeOffset.UtcNow;
        _character = Character.Import(
            WarmaneRealm.Icecrown,
            CharacterName.Create("Paladinlol"),
            WowClass.Paladin,
            WowRace.Human,
            Faction.Alliance,
            80);
    }
    #endregion Given Steps

    #region When Steps
    /// <summary>Synchronizes a Retribution loadout with the supplied GearScore.</summary>
    /// <param name="gearScore">The GearScore to synchronize.</param>
    [When("the Retribution loadout is synchronized with GearScore (.*)")]
    public void WhenTheRetributionLoadoutIsSynchronizedWithGearScore(int gearScore)
    {
        _character.SynchronizeLoadout(
            null,
            "Retribution PvE",
            CharacterRole.MeleeDamage,
            true,
            new GearScore(gearScore),
            new TalentConfiguration("Retribution", 5, 11, 55, "5-11-55", [], []),
            EmptyStats(),
            [],
            CharacterDataSource.WowAddon,
            _synchronizedAtUtc);
    }

    /// <summary>Synchronizes an Icecrown Citadel twenty-five-player lockout.</summary>
    [When("the character raid lockouts are synchronized with Icecrown Citadel 25-player")]
    public void WhenTheCharacterRaidLockoutsAreSynchronizedWithIcecrownCitadel25Player()
    {
        _character.RecordCompleteRaidSaveScan(
            [new RaidLockout(RaidInstance.IcecrownCitadel, RaidDifficulty.TwentyFivePlayer, "12345", _synchronizedAtUtc.AddDays(2), false)],
            _synchronizedAtUtc).IsSuccess.ShouldBeTrue();
    }
    #endregion When Steps

    #region Then Steps
    /// <summary>Asserts the expected number of loadouts.</summary>
    /// <param name="expectedCount">The expected number of loadouts.</param>
    [Then("the character should contain (.*) loadout")]
    public void ThenTheCharacterShouldContainLoadout(int expectedCount) => _character.Loadouts.Count.ShouldBe(expectedCount);

    /// <summary>Asserts the synchronized loadout GearScore.</summary>
    /// <param name="expectedGearScore">The expected GearScore.</param>
    [Then("the synchronized loadout GearScore should be (.*)")]
    public void ThenTheSynchronizedLoadoutGearScoreShouldBe(int expectedGearScore) =>
        _character.Loadouts.Single().GearScore.Value.ShouldBe(expectedGearScore);

    /// <summary>Asserts that a loadout synchronization event was raised.</summary>
    [Then("a loadout synchronized domain event should exist")]
    public void ThenALoadoutSynchronizedDomainEventShouldExist() =>
        _character.GetDomainEvents().OfType<LoadoutSynchronized>().ShouldNotBeEmpty();

    /// <summary>Asserts that the character is saved to Icecrown Citadel twenty-five-player.</summary>
    [Then("the character should be saved to Icecrown Citadel 25-player")]
    public void ThenTheCharacterShouldBeSavedToIcecrownCitadel25Player() =>
        _character.IsSavedTo(RaidInstance.IcecrownCitadel, RaidDifficulty.TwentyFivePlayer, _synchronizedAtUtc).ShouldBeTrue();

    /// <summary>Asserts that a raid-lockout synchronization event was raised.</summary>
    [Then("a raid lockouts synchronized domain event should exist")]
    public void ThenARaidLockoutsSynchronizedDomainEventShouldExist() =>
        _character.GetDomainEvents().OfType<RaidLockoutsSynchronized>().ShouldNotBeEmpty();
    #endregion Then Steps

    #region Helpers
    /// <summary>Creates an empty combat-stat snapshot for tests that do not exercise stat behavior.</summary>
    /// <returns>An empty combat-stat snapshot.</returns>
    private static CombatStats EmptyStats() => new(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
    #endregion Helpers
}
