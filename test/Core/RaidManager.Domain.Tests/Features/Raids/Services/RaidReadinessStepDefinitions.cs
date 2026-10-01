using Pivot.Framework.Domain.Exceptions;
using Reqnroll;
using Shouldly;
using RaidManager.Domain.Features.Characters.Aggregates;
using RaidManager.Domain.Features.Characters.Enums;
using RaidManager.Domain.Features.Characters.ValueObjects;
using RaidManager.Domain.Features.Raids.Aggregates;
using RaidManager.Domain.Features.Raids.Enums;
using RaidManager.Domain.Features.Raids.Services;
using RaidManager.Domain.Features.Raids.ValueObjects;
using RaidManager.Domain.Features.Shared.Enums;
using RaidManager.Domain.Features.Shared.Identifiers;

namespace RaidManager.Domain.Tests.Features.Raids.Services;

/// <summary>Defines business-readable steps for raid-start readiness and the locks it enforces.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-09-29<br/>
/// Purpose: Verifies per-target verdicts, their most restrictive combination, and that confirmed locks block signup and roster.
/// </remarks>
[Binding]
[Scope(Feature = "Raid-start readiness")]
public sealed class RaidReadinessStepDefinitions
{
    #region Constants
    /// <summary>Defines how many hours before raid start signups close.</summary>
    private const int SignupHoursBeforeStart = 4;
    #endregion Constants

    #region Fields
    /// <summary>Stores the fixed UTC instant treated as now throughout the scenario.</summary>
    private readonly DateTimeOffset _nowUtc = DateTimeOffset.UtcNow;

    /// <summary>Stores one character per character name used in the scenario.</summary>
    private readonly Dictionary<string, Character> _characters = [];

    /// <summary>Stores one stable user identifier per player name used in the scenario.</summary>
    private readonly Dictionary<string, UserId> _players = [];

    /// <summary>Stores the raid targets given by the background.</summary>
    private List<RaidTarget> _targets = [];

    /// <summary>Stores the raid under test once its evidence age is known.</summary>
    private Raid? _raid;

    /// <summary>Stores the scheduled raid start.</summary>
    private DateTimeOffset _startsAtUtc;

    /// <summary>Stores the latest assessment.</summary>
    private CharacterReadiness? _readiness;

    /// <summary>Stores the domain exception raised by the latest rejected operation, if any.</summary>
    private DomainException? _rejection;
    #endregion Fields

    #region Properties
    /// <summary>Gets the raid under test, failing the scenario when the background did not create it.</summary>
    private Raid Raid => _raid ?? throw new InvalidOperationException("No raid was created in this scenario.");

    /// <summary>Gets the latest assessment, failing the scenario when no character was assessed.</summary>
    private CharacterReadiness Readiness => _readiness ?? throw new InvalidOperationException("No character was assessed in this scenario.");
    #endregion Properties

    #region Given Steps
    /// <summary>Records the start and required targets of the raid under test.</summary>
    /// <param name="hours">Hours from now until raid start.</param>
    /// <param name="targets">Table with the columns <c>instance</c> and <c>difficulty</c>.</param>
    [Given("a raid starting in {int} hours requiring these targets")]
    public void GivenARaidStartingInHoursRequiringTheseTargets(int hours, DataTable targets)
    {
        _startsAtUtc = _nowUtc.AddHours(hours);
        _targets = [.. targets.Rows.Select(row =>
            new RaidTarget(Enum.Parse<RaidInstance>(row["instance"]), Enum.Parse<RaidDifficulty>(row["difficulty"])))];
    }

    /// <summary>Creates the raid, open for signups, with the accepted lockout evidence age.</summary>
    /// <param name="hours">The maximum accepted evidence age in hours.</param>
    [Given("the raid accepts lockout evidence up to {int} hours old")]
    public void GivenTheRaidAcceptsLockoutEvidenceUpToHoursOld(int hours)
    {
        _raid = Raid.Create(
            new CommunityId(Guid.NewGuid()),
            new UserId(Guid.NewGuid()),
            "Icecrown Citadel",
            _targets,
            _startsAtUtc,
            _startsAtUtc.AddHours(-SignupHoursBeforeStart),
            new RaidRequirements(null, true, TimeSpan.FromHours(hours)),
            null);
        _raid.OpenForSignups();
    }

    /// <summary>Synchronizes a complete snapshot without any raid save.</summary>
    /// <param name="character">The character name.</param>
    /// <param name="hours">Hours since the snapshot was observed.</param>
    [Given("the character {string} synchronized {int} hours ago without saves")]
    public void GivenTheCharacterSynchronizedHoursAgoWithoutSaves(string character, int hours) =>
        CharacterNamed(character).RecordCompleteRaidSaveScan([], _nowUtc.AddHours(-hours)).IsSuccess.ShouldBeTrue();

    /// <summary>Synchronizes a snapshot holding the saves listed in the table.</summary>
    /// <param name="character">The character name.</param>
    /// <param name="hours">Hours since the snapshot was observed.</param>
    /// <param name="saves">Table with the columns <c>instance</c>, <c>difficulty</c>, <c>resetsHoursFromRaidStart</c> and <c>extended</c>.</param>
    [Given("the character {string} synchronized {int} hours ago with these saves")]
    public void GivenTheCharacterSynchronizedHoursAgoWithTheseSaves(string character, int hours, DataTable saves) =>
        CharacterNamed(character).RecordCompleteRaidSaveScan(
            saves.Rows.Select(row => new RaidLockout(
                Enum.Parse<RaidInstance>(row["instance"]),
                Enum.Parse<RaidDifficulty>(row["difficulty"]),
                null,
                _startsAtUtc.AddHours(int.Parse(row["resetsHoursFromRaidStart"], System.Globalization.CultureInfo.InvariantCulture)),
                bool.Parse(row["extended"]))),
            _nowUtc.AddHours(-hours)).IsSuccess.ShouldBeTrue();

    /// <summary>Records a saved-instance scan that could not read every save.</summary>
    /// <param name="character">The character name.</param>
    /// <param name="hours">Hours since the scan was attempted.</param>
    [Given("the character {string} recorded an incomplete raid-save scan {int} hours ago")]
    public void GivenTheCharacterRecordedAnIncompleteRaidSaveScanHoursAgo(string character, int hours) =>
        CharacterNamed(character).RecordIncompleteRaidSaveScan(_nowUtc.AddHours(-hours));

    /// <summary>Synchronizes an addon loadout, which is not raid-save evidence.</summary>
    /// <param name="character">The character name.</param>
    /// <param name="hours">Hours since the loadout was observed.</param>
    [Given("the character {string} synchronized a loadout {int} hours ago")]
    public void GivenTheCharacterSynchronizedALoadoutHoursAgo(string character, int hours) =>
        CharacterNamed(character).SynchronizeLoadout(
            null,
            "Frost",
            CharacterRole.Tank,
            true,
            new GearScore(5800),
            new TalentConfiguration("Frost", 0, 53, 18, "0-53-18", [], []),
            new CombatStats(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0),
            [],
            CharacterDataSource.WowAddon,
            _nowUtc.AddHours(-hours));

    /// <summary>Imports a character that has no addon snapshot.</summary>
    /// <param name="character">The character name.</param>
    [Given("the character {string} was never synchronized")]
    public void GivenTheCharacterWasNeverSynchronized(string character) => CharacterNamed(character);

    /// <summary>Signs a player up offering a character assessed as currently eligible.</summary>
    /// <param name="player">The player name.</param>
    /// <param name="character">The character name.</param>
    [Given("{string} signed up offering {string}")]
    public void GivenSignedUpOffering(string player, string character) => Submit(player, character, Assess(character));
    #endregion Given Steps

    #region When Steps
    /// <summary>Assesses a character for the raid under test.</summary>
    /// <param name="character">The character name.</param>
    [When("the character {string} is assessed")]
    public void WhenTheCharacterIsAssessed(string character) => _readiness = Assess(character);

    /// <summary>Attempts a signup offering a character with its current assessment.</summary>
    /// <param name="player">The player name.</param>
    /// <param name="character">The character name.</param>
    [When("{string} signs up offering {string}")]
    public void WhenSignsUpOffering(string player, string character) => Attempt(() => Submit(player, character, Assess(character)));

    /// <summary>Attempts a signup whose assessment was made for another raid start.</summary>
    /// <param name="player">The player name.</param>
    /// <param name="character">The character name.</param>
    [When("{string} signs up offering {string} assessed for a different start time")]
    public void WhenSignsUpOfferingAssessedForADifferentStartTime(string player, string character)
    {
        var current = Assess(character);
        var outdated = CharacterReadiness.Create(current.CharacterId, _startsAtUtc.AddHours(-1), current.Assessments);
        Attempt(() => Submit(player, character, outdated));
    }

    /// <summary>Attempts to roster the player's offered character with its current assessment.</summary>
    /// <param name="character">The character name.</param>
    /// <param name="player">The player name.</param>
    /// <param name="groupNumber">The one-based raid group.</param>
    /// <param name="position">The one-based position.</param>
    [When("the officer selects {string} of {string} for group {int} position {int}")]
    public void WhenTheOfficerSelectsOfForGroupPosition(string character, string player, int groupNumber, int position)
    {
        var selected = _characters[character];
        var loadoutId = Raid.Signups.Single(signup => signup.UserId == PlayerId(player)).Options
            .Single(option => option.CharacterId == selected.Id).LoadoutId;
        Attempt(() => Raid.SelectRosterOption(PlayerId(player), selected.Id, loadoutId, Assess(character), groupNumber, position));
    }
    #endregion When Steps

    #region Then Steps
    /// <summary>Asserts the verdict of one target.</summary>
    /// <param name="instance">The instance name.</param>
    /// <param name="difficulty">The difficulty name.</param>
    /// <param name="expectedVerdict">The expected verdict name.</param>
    [Then("the {word} {word} verdict is {word}")]
    public void ThenTheVerdictIs(string instance, string difficulty, string expectedVerdict) =>
        AssessmentOf(instance, difficulty).Verdict.ShouldBe(Enum.Parse<ReadinessVerdict>(expectedVerdict));

    /// <summary>Asserts the reset time reported for one target.</summary>
    /// <param name="instance">The instance name.</param>
    /// <param name="difficulty">The difficulty name.</param>
    /// <param name="hours">Expected hours before raid start.</param>
    [Then("the {word} {word} verdict shows a reset {int} hours before raid start")]
    public void ThenTheVerdictShowsAResetHoursBeforeRaidStart(string instance, string difficulty, int hours) =>
        AssessmentOf(instance, difficulty).ResetsAtUtc.ShouldBe(_startsAtUtc.AddHours(-hours));

    /// <summary>Asserts the combined verdict.</summary>
    /// <param name="expectedVerdict">The expected verdict name.</param>
    [Then("the overall verdict is {word}")]
    public void ThenTheOverallVerdictIs(string expectedVerdict) =>
        Readiness.Verdict.ShouldBe(Enum.Parse<ReadinessVerdict>(expectedVerdict));

    /// <summary>Asserts that the signup was rejected and not stored.</summary>
    [Then("the signup is rejected")]
    public void ThenTheSignupIsRejected()
    {
        _rejection.ShouldNotBeNull();
        Raid.Signups.ShouldBeEmpty();
    }

    /// <summary>Asserts that the player's signup offers exactly the given character.</summary>
    /// <param name="player">The player name.</param>
    /// <param name="character">The character name.</param>
    [Then("{string} has signed up offering {string}")]
    public void ThenHasSignedUpOffering(string player, string character)
    {
        _rejection.ShouldBeNull();
        Raid.Signups.Single(signup => signup.UserId == PlayerId(player)).Options
            .ShouldHaveSingleItem().CharacterId.ShouldBe(_characters[character].Id);
    }

    /// <summary>Asserts that the roster assignment was rejected.</summary>
    [Then("the roster assignment is rejected")]
    public void ThenTheRosterAssignmentIsRejected() => _rejection.ShouldNotBeNull();

    /// <summary>Asserts that the player holds no roster selection.</summary>
    /// <param name="player">The player name.</param>
    [Then("the roster contains no selection for {string}")]
    public void ThenTheRosterContainsNoSelectionFor(string player) =>
        Raid.RosterSelections.ShouldNotContain(selection => selection.UserId == PlayerId(player));
    #endregion Then Steps

    #region Private Helpers
    /// <summary>Assesses a named character for the raid under test at the scenario's now.</summary>
    /// <param name="character">The character name.</param>
    /// <returns>The character's readiness.</returns>
    private CharacterReadiness Assess(string character) => RaidReadinessEvaluator.Assess(_characters[character], Raid, _nowUtc);

    /// <summary>Submits a confirmed signup offering one loadout of a named character.</summary>
    /// <param name="player">The player name.</param>
    /// <param name="character">The character name.</param>
    /// <param name="readiness">The readiness supplied with the signup.</param>
    private void Submit(string player, string character, CharacterReadiness readiness) =>
        Raid.SubmitSignup(
            PlayerId(player),
            RaidAvailability.Confirmed,
            null,
            [new SignupOption(_characters[character].Id, new LoadoutId(Guid.NewGuid()))],
            [readiness],
            null,
            _nowUtc);

    /// <summary>Runs an operation, capturing a domain rejection instead of failing the step.</summary>
    /// <param name="operation">The operation to attempt.</param>
    private void Attempt(Action operation)
    {
        try
        {
            operation();
        }
        catch (DomainException exception)
        {
            _rejection = exception;
        }
    }

    /// <summary>Returns the latest assessment of one target.</summary>
    /// <param name="instance">The instance name.</param>
    /// <param name="difficulty">The difficulty name.</param>
    /// <returns>The target's assessment.</returns>
    private EligibilityAssessment AssessmentOf(string instance, string difficulty)
    {
        var target = new RaidTarget(Enum.Parse<RaidInstance>(instance), Enum.Parse<RaidDifficulty>(difficulty));
        return Readiness.Assessments.Single(assessment => assessment.Target == target);
    }

    /// <summary>Returns the character with the given name, importing it on first use.</summary>
    /// <param name="character">The character name.</param>
    /// <returns>The named character.</returns>
    private Character CharacterNamed(string character)
    {
        if (!_characters.TryGetValue(character, out var existing))
        {
            existing = Character.Import(
                WarmaneRealm.Icecrown,
                CharacterName.Create(character),
                WowClass.DeathKnight,
                WowRace.Human,
                Faction.Alliance,
                80);
            _characters[character] = existing;
        }

        return existing;
    }

    /// <summary>Returns the stable user identifier for a player name.</summary>
    /// <param name="player">The player name.</param>
    /// <returns>The player's user identifier.</returns>
    private UserId PlayerId(string player)
    {
        if (!_players.TryGetValue(player, out var userId))
        {
            userId = new UserId(Guid.NewGuid());
            _players[player] = userId;
        }

        return userId;
    }
    #endregion Private Helpers
}
