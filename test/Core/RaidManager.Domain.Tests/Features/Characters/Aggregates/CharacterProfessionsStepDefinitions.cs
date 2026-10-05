using Pivot.Framework.Domain.Exceptions;
using Reqnroll;
using Shouldly;
using RaidManager.Domain.Features.Characters.Aggregates;
using RaidManager.Domain.Features.Characters.Enums;
using RaidManager.Domain.Features.Characters.ValueObjects;
using RaidManager.Domain.Features.Shared.Enums;

namespace RaidManager.Domain.Tests.Features.Characters.Aggregates;

/// <summary>Defines business-readable steps for the professions and the visibility of a character's profile.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-05<br/>
/// Purpose: Verifies that only an in-order, valid skill-list read replaces a character's professions (story #19), and
/// that a profile starts visible to the community (owner decision on #19, 2026-10-05).
/// </remarks>
[Binding]
[Scope(Feature = "Character professions")]
public sealed class CharacterProfessionsStepDefinitions
{
    #region Fields
    /// <summary>Stores the fixed UTC instant treated as now throughout the scenario.</summary>
    private readonly DateTimeOffset _nowUtc = DateTimeOffset.UtcNow;

    /// <summary>Stores the character under test.</summary>
    private Character _character = null!;

    /// <summary>Stores whether the latest read was recorded.</summary>
    private bool? _recorded;

    /// <summary>Stores the domain error raised by the latest read, if any.</summary>
    private DomainException? _failure;
    #endregion Fields

    #region Given Steps
    /// <summary>Imports a character and records a read of the listed professions.</summary>
    /// <param name="hours">Hours since the read was observed.</param>
    /// <param name="professions">Table with the columns <c>name</c>, <c>rank</c> and <c>maxRank</c>.</param>
    [Given("a character whose professions were read {int} hours ago")]
    public void GivenACharacterWhoseProfessionsWereReadHoursAgo(int hours, DataTable professions)
    {
        _character = Character.Import(
            WarmaneRealm.Icecrown,
            CharacterName.Create("Arthasdk"),
            WowClass.DeathKnight,
            WowRace.Human,
            Faction.Alliance,
            80);
        _character.SynchronizeProfessions(Read(professions), _nowUtc.AddHours(-hours)).ShouldBeTrue();
    }
    #endregion Given Steps

    #region When Steps
    /// <summary>Records a read of the listed professions.</summary>
    /// <param name="hours">Hours since the read was observed.</param>
    /// <param name="professions">Table with the columns <c>name</c>, <c>rank</c> and <c>maxRank</c>.</param>
    [When("the professions are read {int} hours ago")]
    public void WhenTheProfessionsAreReadHoursAgo(int hours, DataTable professions) =>
        _recorded = _character.SynchronizeProfessions(Read(professions), _nowUtc.AddHours(-hours));

    /// <summary>Records a read of one profession, capturing the domain error it may raise.</summary>
    /// <param name="hours">Hours since the read was observed.</param>
    /// <param name="name">The profession name.</param>
    /// <param name="rank">The current skill.</param>
    /// <param name="maxRank">The maximum skill.</param>
    [When("the professions are read {int} hours ago with {string} at {int} of {int}")]
    public void WhenTheProfessionsAreReadHoursAgoWith(int hours, string name, int rank, int maxRank)
    {
        try
        {
            _character.SynchronizeProfessions([new Profession(name, rank, maxRank)], _nowUtc.AddHours(-hours));
        }
        catch (DomainException exception)
        {
            _failure = exception;
        }
    }
    #endregion When Steps

    #region Then Steps
    /// <summary>Asserts that the latest read was recorded.</summary>
    [Then("the read is recorded")]
    public void ThenTheReadIsRecorded() => _recorded.ShouldBe(true);

    /// <summary>Asserts that the latest read was ignored because a newer one exists.</summary>
    [Then("the read is ignored")]
    public void ThenTheReadIsIgnored() => _recorded.ShouldBe(false);

    /// <summary>Asserts that the latest read raised a domain error.</summary>
    [Then("the read fails with a domain error")]
    public void ThenTheReadFailsWithADomainError() => _failure.ShouldNotBeNull();

    /// <summary>Asserts the character's professions, in order.</summary>
    /// <param name="professions">Table with the columns <c>name</c>, <c>rank</c> and <c>maxRank</c>.</param>
    [Then("the character has these professions")]
    public void ThenTheCharacterHasTheseProfessions(DataTable professions) =>
        _character.Professions.ShouldBe(Read(professions));

    /// <summary>Asserts that the character has no profession.</summary>
    [Then("the character has no professions")]
    public void ThenTheCharacterHasNoProfessions() => _character.Professions.ShouldBeEmpty();

    /// <summary>Asserts the observation time of the latest recorded read.</summary>
    /// <param name="hours">Expected hours before now.</param>
    [Then("the professions were last read {int} hours ago")]
    public void ThenTheProfessionsWereLastReadHoursAgo(int hours) =>
        _character.LastProfessionsSynchronizedAtUtc.ShouldBe(_nowUtc.AddHours(-hours));

    /// <summary>Asserts the time of the latest addon sync.</summary>
    /// <param name="hours">Expected hours before now.</param>
    [Then("the last addon sync was {int} hours ago")]
    public void ThenTheLastAddonSyncWasHoursAgo(int hours) =>
        _character.LastAddonSynchronizedAtUtc.ShouldBe(_nowUtc.AddHours(-hours));

    /// <summary>Asserts who may see the character's profile.</summary>
    /// <param name="visibility">The expected visibility name.</param>
    [Then("the character's visibility is {word}")]
    public void ThenTheCharactersVisibilityIs(string visibility) =>
        _character.Visibility.ShouldBe(Enum.Parse<CharacterVisibility>(visibility));
    #endregion Then Steps

    #region Private Helpers
    /// <summary>Reads professions from a table.</summary>
    /// <param name="professions">Table with the columns <c>name</c>, <c>rank</c> and <c>maxRank</c>.</param>
    /// <returns>The professions, in the table's order.</returns>
    private static List<Profession> Read(DataTable professions) =>
        [.. professions.Rows.Select(row => new Profession(
            row["name"],
            int.Parse(row["rank"], System.Globalization.CultureInfo.InvariantCulture),
            int.Parse(row["maxRank"], System.Globalization.CultureInfo.InvariantCulture)))];
    #endregion Private Helpers
}
