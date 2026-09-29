using Reqnroll;
using Shouldly;
using RaidManager.Domain.Features.Characters.Enums;
using RaidManager.Domain.Features.Shared.Enums;
using RaidManager.Domain.Features.Characters.ValueObjects;
using RaidManager.Domain.Features.Raids.Aggregates;
using RaidManager.Domain.Features.Raids.Enums;
using RaidManager.Domain.Features.Raids.ValueObjects;
using RaidManager.Domain.Features.Shared.Identifiers;

namespace RaidManager.Domain.Tests.Features.Raids.Aggregates;

/// <summary>Defines business-readable steps for raid roster selection behavior.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-09-29<br/>
/// Purpose: Verifies that multiple offered loadouts for one person still produce at most one final roster place.
/// </remarks>
[Binding]
[Scope(Feature = "Raid roster selection")]
public sealed class RaidStepDefinitions
{
    #region Fields
    /// <summary>Stores the raid under test.</summary>
    private Raid _raid = null!;

    /// <summary>Stores the participant identifier.</summary>
    private UserId _userId = null!;

    /// <summary>Stores the first offered character identifier.</summary>
    private CharacterId _firstCharacterId = null!;

    /// <summary>Stores the first offered loadout identifier.</summary>
    private LoadoutId _firstLoadoutId = null!;

    /// <summary>Stores the second offered character identifier.</summary>
    private CharacterId _secondCharacterId = null!;

    /// <summary>Stores the second offered loadout identifier.</summary>
    private LoadoutId _secondLoadoutId = null!;
    #endregion Fields

    #region Given Steps
    /// <summary>Creates a raid open for participant signups.</summary>
    [Given("an Icecrown Citadel 25-player raid open for signups")]
    public void GivenAnIcecrownCitadel25PlayerRaidOpenForSignups()
    {
        var startsAtUtc = DateTimeOffset.UtcNow.AddDays(2);
        _raid = Raid.Create(
            new CommunityId(Guid.NewGuid()),
            new UserId(Guid.NewGuid()),
            [new RaidTarget(RaidInstance.IcecrownCitadel, RaidDifficulty.TwentyFivePlayer)],
            startsAtUtc,
            startsAtUtc.AddHours(-4),
            new RaidRequirements(new GearScore(6000), true, TimeSpan.FromDays(2)),
            null);
        _raid.OpenForSignups();
    }

    /// <summary>Submits a participant signup offering two independent character loadouts.</summary>
    [Given("a participant has signed up offering two loadouts")]
    public void GivenAParticipantHasSignedUpOfferingTwoLoadouts()
    {
        _userId = new UserId(Guid.NewGuid());
        _firstCharacterId = new CharacterId(Guid.NewGuid());
        _firstLoadoutId = new LoadoutId(Guid.NewGuid());
        _secondCharacterId = new CharacterId(Guid.NewGuid());
        _secondLoadoutId = new LoadoutId(Guid.NewGuid());
        _raid.SubmitSignup(
            _userId,
            RaidSignupStatus.Available,
            [new SignupOption(_firstCharacterId, _firstLoadoutId), new SignupOption(_secondCharacterId, _secondLoadoutId)],
            null,
            DateTimeOffset.UtcNow);
    }

    /// <summary>Selects the first loadout before the tested replacement operation.</summary>
    [Given("the participant first offered loadout is selected for group 1 position 1")]
    public void GivenTheParticipantFirstOfferedLoadoutIsSelectedForGroup1Position1() =>
        _raid.SelectRosterOption(_userId, _firstCharacterId, _firstLoadoutId, 1, 1);
    #endregion Given Steps

    #region When Steps
    /// <summary>Selects the first offered loadout.</summary>
    [When("the raid leader selects the participant first offered loadout for group 1 position 1")]
    public void WhenTheRaidLeaderSelectsTheParticipantFirstOfferedLoadoutForGroup1Position1() =>
        _raid.SelectRosterOption(_userId, _firstCharacterId, _firstLoadoutId, 1, 1);

    /// <summary>Selects the second offered loadout.</summary>
    [When("the raid leader selects the participant second offered loadout for group 2 position 1")]
    public void WhenTheRaidLeaderSelectsTheParticipantSecondOfferedLoadoutForGroup2Position1() =>
        _raid.SelectRosterOption(_userId, _secondCharacterId, _secondLoadoutId, 2, 1);
    #endregion When Steps

    #region Then Steps
    /// <summary>Asserts that the participant occupies exactly one roster place.</summary>
    /// <param name="expectedCount">The expected number of selections.</param>
    [Then("the raid roster should contain (.*) selection for that participant")]
    public void ThenTheRaidRosterShouldContainSelectionForThatParticipant(int expectedCount) =>
        _raid.RosterSelections.Count(selection => selection.UserId == _userId).ShouldBe(expectedCount);

    /// <summary>Asserts that the second offered loadout replaced the first selection.</summary>
    [Then("the participant selected loadout should be the second offered loadout")]
    public void ThenTheParticipantSelectedLoadoutShouldBeTheSecondOfferedLoadout() =>
        _raid.RosterSelections.Single(selection => selection.UserId == _userId).LoadoutId.ShouldBe(_secondLoadoutId);
    #endregion Then Steps
}
