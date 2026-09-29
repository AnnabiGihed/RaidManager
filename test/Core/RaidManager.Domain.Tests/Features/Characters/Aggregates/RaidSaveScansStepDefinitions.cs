using Pivot.Framework.Domain.Shared;
using Reqnroll;
using Shouldly;
using RaidManager.Domain.Features.Characters.Aggregates;
using RaidManager.Domain.Features.Characters.Enums;
using RaidManager.Domain.Features.Characters.Errors;
using RaidManager.Domain.Features.Characters.ValueObjects;
using RaidManager.Domain.Features.Shared.Enums;

namespace RaidManager.Domain.Tests.Features.Characters.Aggregates;

/// <summary>Defines business-readable steps for complete and incomplete raid-save scans.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-09-29<br/>
/// Purpose: Verifies that only an in-order complete scan replaces raid saves and that incomplete scans never erase them.
/// </remarks>
[Binding]
[Scope(Feature = "Raid-save scans")]
public sealed class RaidSaveScansStepDefinitions
{
    #region Fields
    /// <summary>Stores the fixed UTC instant treated as now throughout the scenario.</summary>
    private readonly DateTimeOffset _nowUtc = DateTimeOffset.UtcNow;

    /// <summary>Stores the character under test.</summary>
    private Character _character = null!;

    /// <summary>Stores the result of the latest complete scan.</summary>
    private Result? _result;
    #endregion Fields

    #region Given Steps
    /// <summary>Imports a character and records a complete scan holding one save.</summary>
    /// <param name="instance">The saved instance name.</param>
    /// <param name="difficulty">The saved difficulty name.</param>
    /// <param name="hours">Hours since the scan was observed.</param>
    [Given("a character saved to {word} {word} by a complete scan {int} hours ago")]
    public void GivenACharacterSavedToByACompleteScanHoursAgo(string instance, string difficulty, int hours)
    {
        _character = Character.Import(
            WarmaneRealm.Icecrown,
            CharacterName.Create("Frostmourne"),
            WowClass.DeathKnight,
            WowRace.Human,
            Faction.Alliance,
            80);
        var save = new RaidLockout(Enum.Parse<RaidInstance>(instance), Enum.Parse<RaidDifficulty>(difficulty), null, _nowUtc.AddDays(3), false);
        _character.RecordCompleteRaidSaveScan([save], _nowUtc.AddHours(-hours)).IsSuccess.ShouldBeTrue();
    }
    #endregion Given Steps

    #region When Steps
    /// <summary>Records a complete scan that found no save.</summary>
    /// <param name="hours">Hours since the scan was observed.</param>
    [When("a complete scan without saves is recorded {int} hours ago")]
    public void WhenACompleteScanWithoutSavesIsRecordedHoursAgo(int hours) =>
        _result = _character.RecordCompleteRaidSaveScan([], _nowUtc.AddHours(-hours));

    /// <summary>Records a scan that could not read every save.</summary>
    /// <param name="hours">Hours since the scan was attempted.</param>
    [When("an incomplete scan is recorded {int} hours ago")]
    public void WhenAnIncompleteScanIsRecordedHoursAgo(int hours) =>
        _character.RecordIncompleteRaidSaveScan(_nowUtc.AddHours(-hours));
    #endregion When Steps

    #region Then Steps
    /// <summary>Asserts that the latest complete scan was accepted.</summary>
    [Then("the scan is accepted")]
    public void ThenTheScanIsAccepted() => _result.ShouldNotBeNull().IsSuccess.ShouldBeTrue();

    /// <summary>Asserts that the latest complete scan was rejected because a newer one exists.</summary>
    [Then("the scan is rejected as outdated")]
    public void ThenTheScanIsRejectedAsOutdated() =>
        _result.ShouldNotBeNull().Error.ShouldBe(CharacterErrors.RaidSaveScanOutdated);

    /// <summary>Asserts that the character holds no raid save.</summary>
    [Then("the character has no raid saves")]
    public void ThenTheCharacterHasNoRaidSaves() => _character.RaidLockouts.ShouldBeEmpty();

    /// <summary>Asserts that the character holds a save for the target.</summary>
    /// <param name="instance">The instance name.</param>
    /// <param name="difficulty">The difficulty name.</param>
    [Then("the character is saved to {word} {word}")]
    public void ThenTheCharacterIsSavedTo(string instance, string difficulty) =>
        _character.RaidLockouts.ShouldHaveSingleItem().ShouldSatisfyAllConditions(
            save => save.Instance.ShouldBe(Enum.Parse<RaidInstance>(instance)),
            save => save.Difficulty.ShouldBe(Enum.Parse<RaidDifficulty>(difficulty)));

    /// <summary>Asserts the observation time of the latest accepted complete scan.</summary>
    /// <param name="hours">Expected hours before now.</param>
    [Then("the last complete scan was {int} hours ago")]
    public void ThenTheLastCompleteScanWasHoursAgo(int hours) =>
        _character.LastCompleteRaidSaveScanAtUtc.ShouldBe(_nowUtc.AddHours(-hours));

    /// <summary>Asserts the time of the latest incomplete scan.</summary>
    /// <param name="hours">Expected hours before now.</param>
    [Then("the last incomplete scan was {int} hours ago")]
    public void ThenTheLastIncompleteScanWasHoursAgo(int hours) =>
        _character.LastIncompleteRaidSaveScanAtUtc.ShouldBe(_nowUtc.AddHours(-hours));
    #endregion Then Steps
}
