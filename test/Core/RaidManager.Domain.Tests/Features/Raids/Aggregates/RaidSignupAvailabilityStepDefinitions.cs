using Pivot.Framework.Domain.Exceptions;
using Reqnroll;
using Shouldly;
using RaidManager.Domain.Features.Raids.Aggregates;
using RaidManager.Domain.Features.Raids.Enums;
using RaidManager.Domain.Features.Raids.ValueObjects;
using RaidManager.Domain.Features.Shared.Enums;
using RaidManager.Domain.Features.Shared.Identifiers;
using RaidManager.Domain.Tests.Features.Raids.Support;

namespace RaidManager.Domain.Tests.Features.Raids.Aggregates;

/// <summary>Defines business-readable steps for signup availability and its independence from roster selection.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-09-29<br/>
/// Purpose: Verifies late-arrival rules, declined signups without characters, and that selection keeps availability.
/// </remarks>
[Binding]
[Scope(Feature = "Raid signup availability")]
public sealed class RaidSignupAvailabilityStepDefinitions
{
    #region Constants
    /// <summary>Defines how far ahead of now the raid under test starts.</summary>
    private const int DaysUntilRaidStart = 2;

    /// <summary>Defines how many hours before raid start signups close.</summary>
    private const int SignupHoursBeforeStart = 4;
    #endregion Constants

    #region Fields
    /// <summary>Stores one stable user identifier per player name used in the scenario.</summary>
    private readonly Dictionary<string, UserId> _players = [];

    /// <summary>Stores the offered option of each player that signed up with one loadout.</summary>
    private readonly Dictionary<string, SignupOption> _offeredOptions = [];

    /// <summary>Stores the raid under test.</summary>
    private Raid _raid = null!;

    /// <summary>Stores the domain exception raised by the latest rejected signup, if any.</summary>
    private DomainException? _rejection;
    #endregion Fields

    #region Given Steps
    /// <summary>Creates an Icecrown Citadel twenty-five-player raid open for signups.</summary>
    [Given("an Icecrown Citadel raid open for signups")]
    public void GivenAnIcecrownCitadelRaidOpenForSignups()
    {
        var startsAtUtc = DateTimeOffset.UtcNow.AddDays(DaysUntilRaidStart);
        _raid = Raid.Create(
            new CommunityId(Guid.NewGuid()),
            new UserId(Guid.NewGuid()),
            "Icecrown Citadel",
            [new RaidTarget(RaidInstance.IcecrownCitadel, RaidDifficulty.TwentyFivePlayer)],
            startsAtUtc,
            startsAtUtc.AddHours(-SignupHoursBeforeStart),
            new RaidRequirements(null, true, TimeSpan.FromDays(DaysUntilRaidStart)),
            null);
        _raid.OpenForSignups();
    }

    /// <summary>Submits a signup with the given availability and a single offered loadout.</summary>
    /// <param name="player">The player name.</param>
    /// <param name="availability">The availability name.</param>
    [Given("{string} signed up as {string} offering one loadout")]
    public void GivenSignedUpAsOfferingOneLoadout(string player, string availability)
    {
        var option = NewOption();
        _offeredOptions[player] = option;
        _raid.SubmitSignup(
            PlayerId(player),
            Enum.Parse<RaidAvailability>(availability),
            null,
            [option],
            [ReadinessAssessments.Available(_raid, option.CharacterId)],
            null,
            DateTimeOffset.UtcNow);
    }
    #endregion Given Steps

    #region When Steps
    /// <summary>Submits a late signup with an arrival time after raid start.</summary>
    /// <param name="player">The player name.</param>
    /// <param name="minutes">Minutes after raid start.</param>
    [When("{string} signs up late arriving {int} minutes after raid start")]
    public void WhenSignsUpLateArrivingMinutesAfterRaidStart(string player, int minutes) =>
        Attempt(player, RaidAvailability.Late, _raid.StartsAtUtc.AddMinutes(minutes), [NewOption()]);

    /// <summary>Attempts a late signup without an arrival time.</summary>
    /// <param name="player">The player name.</param>
    [When("{string} signs up late without an arrival time")]
    public void WhenSignsUpLateWithoutAnArrivalTime(string player) =>
        Attempt(player, RaidAvailability.Late, null, [NewOption()]);

    /// <summary>Attempts a signup that is not late but carries an arrival time.</summary>
    /// <param name="player">The player name.</param>
    /// <param name="availability">The availability name.</param>
    [When("{string} signs up as {string} with an arrival time")]
    public void WhenSignsUpAsWithAnArrivalTime(string player, string availability) =>
        Attempt(player, Enum.Parse<RaidAvailability>(availability), _raid.StartsAtUtc, [NewOption()]);

    /// <summary>Declines the raid without offering any character.</summary>
    /// <param name="player">The player name.</param>
    [When("{string} declines without offering a character")]
    public void WhenDeclinesWithoutOfferingACharacter(string player) =>
        Attempt(player, RaidAvailability.Declined, null, []);

    /// <summary>Attempts a signup with the given availability and no offered character.</summary>
    /// <param name="player">The player name.</param>
    /// <param name="availability">The availability name.</param>
    [When("{string} signs up as {string} without offering a character")]
    public void WhenSignsUpAsWithoutOfferingACharacter(string player, string availability) =>
        Attempt(player, Enum.Parse<RaidAvailability>(availability), null, []);

    /// <summary>Selects the player's offered loadout for a roster position.</summary>
    /// <param name="player">The player name.</param>
    /// <param name="groupNumber">The one-based raid group.</param>
    /// <param name="position">The one-based position.</param>
    [When("the officer selects the offered loadout of {string} for group {int} position {int}")]
    public void WhenTheOfficerSelectsTheOfferedLoadoutOfForGroupPosition(string player, int groupNumber, int position)
    {
        var option = _offeredOptions[player];
        _raid.SelectRosterOption(
            PlayerId(player),
            option.CharacterId,
            option.LoadoutId,
            ReadinessAssessments.Available(_raid, option.CharacterId),
            groupNumber,
            position);
    }
    #endregion When Steps

    #region Then Steps
    /// <summary>Asserts that the signup was rejected and not stored.</summary>
    [Then("the signup is rejected")]
    public void ThenTheSignupIsRejected()
    {
        _rejection.ShouldNotBeNull();
        _raid.Signups.ShouldBeEmpty();
    }

    /// <summary>Asserts the player's availability.</summary>
    /// <param name="player">The player name.</param>
    /// <param name="expectedAvailability">The expected availability name.</param>
    [Then("the availability of {string} is {string}")]
    public void ThenTheAvailabilityOfIs(string player, string expectedAvailability) =>
        SignupOf(player).Availability.ShouldBe(Enum.Parse<RaidAvailability>(expectedAvailability));

    /// <summary>Asserts the player's expected arrival time.</summary>
    /// <param name="player">The player name.</param>
    /// <param name="minutes">Expected minutes after raid start.</param>
    [Then("the signup of {string} expects arrival {int} minutes after raid start")]
    public void ThenTheSignupOfExpectsArrivalMinutesAfterRaidStart(string player, int minutes) =>
        SignupOf(player).LateArrivalUtc.ShouldBe(_raid.StartsAtUtc.AddMinutes(minutes));

    /// <summary>Asserts the number of roster selections held by the player.</summary>
    /// <param name="expectedCount">The expected selection count.</param>
    /// <param name="player">The player name.</param>
    [Then("the roster contains {int} selection for {string}")]
    public void ThenTheRosterContainsSelectionFor(int expectedCount, string player) =>
        _raid.RosterSelections.Count(selection => selection.UserId == PlayerId(player)).ShouldBe(expectedCount);
    #endregion Then Steps

    #region Private Helpers
    /// <summary>Creates a new offered character loadout.</summary>
    /// <returns>A signup option with fresh identifiers.</returns>
    private static SignupOption NewOption() => new(new CharacterId(Guid.NewGuid()), new LoadoutId(Guid.NewGuid()));

    /// <summary>Submits a signup, capturing a domain rejection instead of failing the step.</summary>
    /// <param name="player">The player name.</param>
    /// <param name="availability">The submitted availability.</param>
    /// <param name="lateArrivalUtc">The submitted arrival time, if any.</param>
    /// <param name="options">The offered loadouts.</param>
    private void Attempt(string player, RaidAvailability availability, DateTimeOffset? lateArrivalUtc, SignupOption[] options)
    {
        try
        {
            _raid.SubmitSignup(
                PlayerId(player),
                availability,
                lateArrivalUtc,
                options,
                ReadinessAssessments.AvailableFor(_raid, options),
                null,
                DateTimeOffset.UtcNow);
        }
        catch (DomainException exception)
        {
            _rejection = exception;
        }
    }

    /// <summary>Returns the signup of a player.</summary>
    /// <param name="player">The player name.</param>
    /// <returns>The player's signup.</returns>
    private RaidSignup SignupOf(string player) => _raid.Signups.Single(signup => signup.UserId == PlayerId(player));

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
