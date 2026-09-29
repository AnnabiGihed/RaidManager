using Pivot.Framework.Domain.Exceptions;
using Reqnroll;
using Shouldly;
using RaidManager.Domain.Features.Raids.Aggregates;
using RaidManager.Domain.Features.Raids.Events;
using RaidManager.Domain.Features.Raids.ValueObjects;
using RaidManager.Domain.Features.Shared.Enums;
using RaidManager.Domain.Features.Shared.Identifiers;

namespace RaidManager.Domain.Tests.Features.Raids.Aggregates;

/// <summary>Defines business-readable steps for the targets a raid requires.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-09-29<br/>
/// Purpose: Verifies that a raid keeps every required target of a combined raid and rejects empty or repeated targets.
/// </remarks>
[Binding]
[Scope(Feature = "Raid targets")]
public sealed class RaidTargetsStepDefinitions
{
    #region Fields
    /// <summary>Stores the raid under test when creation succeeded.</summary>
    private Raid? _raid;

    /// <summary>Stores the domain exception raised when creation was rejected.</summary>
    private DomainException? _rejection;
    #endregion Fields

    #region Properties
    /// <summary>Gets the created raid, failing the scenario when creation did not succeed.</summary>
    private Raid Raid => _raid ?? throw new InvalidOperationException("No raid was created in this scenario.");
    #endregion Properties

    #region When Steps
    /// <summary>Attempts to create a raid requiring the targets listed in the table.</summary>
    /// <param name="targets">Table with the columns <c>instance</c> and <c>difficulty</c>.</param>
    [When("the officer creates a raid requiring these targets")]
    public void WhenTheOfficerCreatesARaidRequiringTheseTargets(DataTable targets) =>
        AttemptCreate(targets.Rows.Select(row =>
            new RaidTarget(Enum.Parse<RaidInstance>(row["instance"]), Enum.Parse<RaidDifficulty>(row["difficulty"]))));

    /// <summary>Attempts to create a raid with no target.</summary>
    [When("the officer creates a raid without targets")]
    public void WhenTheOfficerCreatesARaidWithoutTargets() => AttemptCreate([]);
    #endregion When Steps

    #region Then Steps
    /// <summary>Asserts the number of required targets.</summary>
    /// <param name="expectedCount">The expected target count.</param>
    [Then("the raid requires {int} targets")]
    public void ThenTheRaidRequiresTargets(int expectedCount) => Raid.Targets.Count.ShouldBe(expectedCount);

    /// <summary>Asserts that the creation event carries every required target.</summary>
    /// <param name="expectedCount">The expected target count in the event.</param>
    [Then("the raid created event lists {int} targets")]
    public void ThenTheRaidCreatedEventListsTargets(int expectedCount) =>
        Raid.GetDomainEvents().OfType<RaidCreated>().ShouldHaveSingleItem().Targets.Count.ShouldBe(expectedCount);

    /// <summary>Asserts that raid creation was rejected.</summary>
    [Then("the raid creation is rejected")]
    public void ThenTheRaidCreationIsRejected()
    {
        _rejection.ShouldNotBeNull();
        _raid.ShouldBeNull();
    }
    #endregion Then Steps

    #region Private Helpers
    /// <summary>Creates a draft raid starting in two days, capturing a domain rejection instead of failing the step.</summary>
    /// <param name="targets">The required raid targets.</param>
    private void AttemptCreate(IEnumerable<RaidTarget> targets)
    {
        var startsAtUtc = DateTimeOffset.UtcNow.AddDays(2);
        try
        {
            _raid = Raid.Create(
                new CommunityId(Guid.NewGuid()),
                new UserId(Guid.NewGuid()),
                targets,
                startsAtUtc,
                startsAtUtc.AddHours(-4),
                new RaidRequirements(null, true, TimeSpan.FromDays(2)),
                null);
        }
        catch (DomainException exception)
        {
            _rejection = exception;
        }
    }
    #endregion Private Helpers
}
