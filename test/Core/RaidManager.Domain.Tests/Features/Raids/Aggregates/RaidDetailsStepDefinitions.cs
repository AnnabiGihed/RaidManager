using Pivot.Framework.Domain.Exceptions;
using Reqnroll;
using Shouldly;
using RaidManager.Domain.Features.Raids.Aggregates;
using RaidManager.Domain.Features.Raids.Events;
using RaidManager.Domain.Features.Raids.ValueObjects;
using RaidManager.Domain.Features.Shared.Enums;
using RaidManager.Domain.Features.Shared.Identifiers;

namespace RaidManager.Domain.Tests.Features.Raids.Aggregates;

/// <summary>Defines business-readable steps for a raid's title, size and editable details.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Verifies the title and size rules and that an edit reports whether the start or the targets changed.
/// </remarks>
[Binding]
[Scope(Feature = "Raid details")]
public sealed class RaidDetailsStepDefinitions
{
    #region Fields
    /// <summary>Stores the start of the raids the scenarios create.</summary>
    private static readonly DateTimeOffset StartsAtUtc = new(2026, 10, 9, 18, 0, 0, TimeSpan.Zero);

    /// <summary>Stores the requirements of the raids the scenarios create.</summary>
    private static readonly RaidRequirements Requirements = new(null, true, TimeSpan.FromDays(3));

    /// <summary>Stores the Icecrown Citadel 25-player target.</summary>
    private static readonly RaidTarget IcecrownCitadel = new(RaidInstance.IcecrownCitadel, RaidDifficulty.TwentyFivePlayer);

    /// <summary>Stores the raid under test when creation succeeded.</summary>
    private Raid? _raid;

    /// <summary>Stores the domain exception raised when a creation or an edit was rejected.</summary>
    private DomainException? _rejection;
    #endregion Fields

    #region Properties
    /// <summary>Gets the created raid, failing the scenario when creation did not succeed.</summary>
    private Raid Raid => _raid ?? throw new InvalidOperationException("No raid was created in this scenario.");
    #endregion Properties

    #region Given Steps
    /// <summary>Creates a draft Icecrown Citadel 25-player raid.</summary>
    [Given("a draft Icecrown Citadel raid for 25 players")]
    public void GivenADraftIcecrownCitadelRaidFor25Players() => AttemptCreate("Icecrown Citadel", [IcecrownCitadel]);

    /// <summary>Opens the raid for signups.</summary>
    [Given("the raid is open for signups")]
    public void GivenTheRaidIsOpenForSignups() => Raid.OpenForSignups();
    #endregion Given Steps

    #region When Steps
    /// <summary>Attempts to create a raid with a title.</summary>
    /// <param name="title">The requested title.</param>
    [When("the officer creates a raid titled {string}")]
    public void WhenTheOfficerCreatesARaidTitled(string title) => AttemptCreate(title, [IcecrownCitadel]);

    /// <summary>Attempts to create a raid whose title has a given length.</summary>
    /// <param name="length">The title length.</param>
    [When("the officer creates a raid with a title of {int} characters")]
    public void WhenTheOfficerCreatesARaidWithATitleOfCharacters(int length) => AttemptCreate(new string('a', length), [IcecrownCitadel]);

    /// <summary>Attempts to create a raid requiring the targets listed in the table.</summary>
    /// <param name="targets">Table with the columns <c>instance</c> and <c>difficulty</c>.</param>
    [When("the officer creates a raid requiring these targets")]
    public void WhenTheOfficerCreatesARaidRequiringTheseTargets(DataTable targets) =>
        AttemptCreate("Combined raid", targets.Rows.Select(row =>
            new RaidTarget(Enum.Parse<RaidInstance>(row["instance"]), Enum.Parse<RaidDifficulty>(row["difficulty"]))));

    /// <summary>Moves the start one hour later, keeping the deadline.</summary>
    [When("the officer moves the start one hour later")]
    public void WhenTheOfficerMovesTheStartOneHourLater() =>
        AttemptEdit(Raid.Title, Raid.Targets, Raid.StartsAtUtc.AddHours(1), Raid.SignupDeadlineUtc);

    /// <summary>Adds Ruby Sanctum for 25 players to the targets.</summary>
    [When("the officer adds Ruby Sanctum for 25 players")]
    public void WhenTheOfficerAddsRubySanctumFor25Players() =>
        AttemptEdit(Raid.Title, [.. Raid.Targets, new RaidTarget(RaidInstance.RubySanctum, RaidDifficulty.TwentyFivePlayer)], Raid.StartsAtUtc, Raid.SignupDeadlineUtc);

    /// <summary>Renames the raid.</summary>
    /// <param name="title">The new title.</param>
    [When("the officer renames the raid {string}")]
    public void WhenTheOfficerRenamesTheRaid(string title) => AttemptEdit(title, Raid.Targets, Raid.StartsAtUtc, Raid.SignupDeadlineUtc);

    /// <summary>Saves the raid's current details again.</summary>
    [When("the officer saves the raid without changes")]
    public void WhenTheOfficerSavesTheRaidWithoutChanges() => AttemptEdit(Raid.Title, Raid.Targets, Raid.StartsAtUtc, Raid.SignupDeadlineUtc);

    /// <summary>Moves the signup deadline to one hour after the start.</summary>
    [When("the officer moves the signup deadline after the start")]
    public void WhenTheOfficerMovesTheSignupDeadlineAfterTheStart() =>
        AttemptEdit(Raid.Title, Raid.Targets, Raid.StartsAtUtc, Raid.StartsAtUtc.AddHours(1));
    #endregion When Steps

    #region Then Steps
    /// <summary>Asserts the raid title.</summary>
    /// <param name="expected">The expected title.</param>
    [Then("the raid title is {string}")]
    public void ThenTheRaidTitleIs(string expected) => Raid.Title.ShouldBe(expected);

    /// <summary>Asserts the raid size.</summary>
    /// <param name="expected">The expected number of players.</param>
    [Then("the raid is for {int} players")]
    public void ThenTheRaidIsForPlayers(int expected) => Raid.Size.ShouldBe(expected);

    /// <summary>Asserts the number of required targets.</summary>
    /// <param name="expectedCount">The expected target count.</param>
    [Then("the raid requires {int} targets")]
    public void ThenTheRaidRequiresTargets(int expectedCount) => Raid.Targets.Count.ShouldBe(expectedCount);

    /// <summary>Asserts the raid starts one hour after the original start.</summary>
    [Then("the raid starts one hour later")]
    public void ThenTheRaidStartsOneHourLater() => Raid.StartsAtUtc.ShouldBe(StartsAtUtc.AddHours(1));

    /// <summary>Asserts that a creation or an edit was rejected.</summary>
    [Then("the raid change is rejected")]
    public void ThenTheRaidChangeIsRejected() => _rejection.ShouldNotBeNull();

    /// <summary>Asserts a change event that reports a new start only.</summary>
    [Then("the raid reports a change to its start but not its targets")]
    public void ThenTheRaidReportsAChangeToItsStartButNotItsTargets() => ShouldReportChange(startChanged: true, targetsChanged: false);

    /// <summary>Asserts a change event that reports new targets only.</summary>
    [Then("the raid reports a change to its targets but not its start")]
    public void ThenTheRaidReportsAChangeToItsTargetsButNotItsStart() => ShouldReportChange(startChanged: false, targetsChanged: true);

    /// <summary>Asserts a change event that reports neither a new start nor new targets.</summary>
    [Then("the raid reports a change to neither its start nor its targets")]
    public void ThenTheRaidReportsAChangeToNeitherItsStartNorItsTargets() => ShouldReportChange(startChanged: false, targetsChanged: false);

    /// <summary>Asserts that no change event was raised.</summary>
    [Then("the raid reports no change")]
    public void ThenTheRaidReportsNoChange() => Raid.GetDomainEvents().OfType<RaidDetailsChanged>().ShouldBeEmpty();
    #endregion Then Steps

    #region Private Helpers
    /// <summary>Asserts the raid raised one change event with the given flags.</summary>
    /// <param name="startChanged">Whether the event should report a new start.</param>
    /// <param name="targetsChanged">Whether the event should report new targets.</param>
    private void ShouldReportChange(bool startChanged, bool targetsChanged)
    {
        var change = Raid.GetDomainEvents().OfType<RaidDetailsChanged>().ShouldHaveSingleItem();
        change.RaidId.ShouldBe(Raid.Id);
        change.StartChanged.ShouldBe(startChanged);
        change.TargetsChanged.ShouldBe(targetsChanged);
    }

    /// <summary>Creates a raid, capturing a domain rejection instead of failing the step.</summary>
    /// <param name="title">The title.</param>
    /// <param name="targets">The required targets.</param>
    private void AttemptCreate(string title, IEnumerable<RaidTarget> targets)
    {
        try
        {
            _raid = Raid.Create(
                new CommunityId(Guid.NewGuid()),
                new UserId(Guid.NewGuid()),
                title,
                targets,
                StartsAtUtc,
                StartsAtUtc.AddHours(-4),
                Requirements,
                "Bring flasks.");
        }
        catch (DomainException exception)
        {
            _rejection = exception;
        }
    }

    /// <summary>Edits the raid, keeping its description and requirements, and capturing a domain rejection.</summary>
    /// <param name="title">The new title.</param>
    /// <param name="targets">The new targets.</param>
    /// <param name="startsAtUtc">The new start.</param>
    /// <param name="signupDeadlineUtc">The new signup deadline.</param>
    private void AttemptEdit(string title, IEnumerable<RaidTarget> targets, DateTimeOffset startsAtUtc, DateTimeOffset signupDeadlineUtc)
    {
        try
        {
            Raid.UpdateDetails(title, Raid.Description, [.. targets], startsAtUtc, signupDeadlineUtc, Raid.Requirements);
        }
        catch (DomainException exception)
        {
            _rejection = exception;
        }
    }
    #endregion Private Helpers
}
