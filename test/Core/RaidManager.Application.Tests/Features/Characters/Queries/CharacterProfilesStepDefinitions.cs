using Moq;
using Pivot.Framework.Domain.Shared;
using Reqnroll;
using Shouldly;
using RaidManager.Application.Features.Characters.Abstractions;
using RaidManager.Application.Features.Characters.Queries.GetCharacterProfile;
using RaidManager.Application.Features.Characters.Queries.GetMyCharacters;
using RaidManager.Domain.Features.Characters.Enums;
using RaidManager.Domain.Features.Characters.Errors;
using RaidManager.Domain.Features.Shared.Enums;
using RaidManager.Domain.Features.Shared.Identifiers;

namespace RaidManager.Application.Tests.Features.Characters.Queries;

/// <summary>Defines business-readable steps for the characters list and the character profile queries.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-05<br/>
/// Purpose: Verifies that both queries ask the read-only profile reader with the clock's time, that only the owner gets
/// a profile (owner decision on #19, 2026-10-05), and that the queries validate their input.
/// </remarks>
[Binding]
[Scope(Feature = "Character profiles")]
public sealed class CharacterProfilesStepDefinitions
{
    #region Fields
    /// <summary>Stores the instant the clock gives throughout the scenario.</summary>
    private static readonly DateTimeOffset Now = new(2026, 10, 5, 14, 5, 0, TimeSpan.Zero);

    /// <summary>Stores one stable user identifier per player name used in the scenario.</summary>
    private readonly Dictionary<string, UserId> _players = [];

    /// <summary>Stores one stable character identifier per character name used in the scenario.</summary>
    private readonly Dictionary<string, CharacterId> _characters = [];

    /// <summary>Stores the profile reader double.</summary>
    private readonly Mock<ICharacterProfileReader> _reader = new();

    /// <summary>Stores the clock double, fixed at <see cref="Now"/>.</summary>
    private readonly Mock<TimeProvider> _clock = new();

    /// <summary>Stores the result of the latest list query.</summary>
    private Result<IReadOnlyList<CharacterSummaryResponse>>? _listResult;

    /// <summary>Stores the result of the latest profile query.</summary>
    private Result<CharacterProfileResponse>? _profileResult;

    /// <summary>Stores the result of the latest validation.</summary>
    private FluentValidation.Results.ValidationResult? _validation;
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="CharacterProfilesStepDefinitions"/> class.</summary>
    public CharacterProfilesStepDefinitions()
    {
        _clock.Setup(clock => clock.GetUtcNow()).Returns(Now);

        // A player the reader knows nothing about owns no character; Given steps add data for specific players.
        _reader
            .Setup(reader => reader.ListOwnedAsync(It.IsAny<UserId>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<CharacterSummaryResponse>());
        _reader
            .Setup(reader => reader.FindOwnedProfileAsync(It.IsAny<UserId>(), It.IsAny<CharacterId>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CharacterProfileResponse?)null);
    }
    #endregion Constructors

    #region Given Steps
    /// <summary>Makes the reader return the listed characters for the player.</summary>
    /// <param name="player">The player name.</param>
    /// <param name="characters">Table with the column <c>name</c>.</param>
    [Given("the profile reader has these characters for {string}")]
    public void GivenTheProfileReaderHasTheseCharactersFor(string player, DataTable characters)
    {
        IReadOnlyList<CharacterSummaryResponse> responses = [.. characters.Rows.Select(row => new CharacterSummaryResponse(
            CharacterIdOf(row["name"]).Value,
            WarmaneRealm.Icecrown,
            row["name"],
            WowClass.DeathKnight,
            80,
            new LoadoutSummaryResponse("Frost DPS", CharacterRole.MeleeDamage, 5712),
            2,
            Now))];
        _reader
            .Setup(reader => reader.ListOwnedAsync(PlayerId(player), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(responses);
    }

    /// <summary>Makes the reader return a character's profile for its owner only.</summary>
    /// <param name="character">The character name.</param>
    /// <param name="player">The owner's name.</param>
    [Given("the profile reader has the profile of {string} for {string}")]
    public void GivenTheProfileReaderHasTheProfileOfFor(string character, string player)
    {
        var profile = new CharacterProfileResponse(
            CharacterIdOf(character).Value,
            WarmaneRealm.Icecrown,
            character,
            WowClass.DeathKnight,
            WowRace.Human,
            Faction.Alliance,
            80,
            "Citadel Vanguard",
            CharacterVisibility.Community,
            new ProfileSyncResponse(Now, null, null),
            [],
            [],
            []);
        _reader
            .Setup(reader => reader.FindOwnedProfileAsync(PlayerId(player), CharacterIdOf(character), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);
    }
    #endregion Given Steps

    #region When Steps
    /// <summary>Sends the list query for the player.</summary>
    /// <param name="player">The player name.</param>
    /// <returns>A task that completes when the query has been handled.</returns>
    [When("{string} asks for her characters")]
    public async Task WhenAsksForHerCharacters(string player) =>
        _listResult = await new GetMyCharactersQueryHandler(_reader.Object, _clock.Object)
            .Handle(new GetMyCharactersQuery(PlayerId(player).Value), CancellationToken.None);

    /// <summary>Sends the profile query for the player and character.</summary>
    /// <param name="player">The player name.</param>
    /// <param name="character">The character name.</param>
    /// <returns>A task that completes when the query has been handled.</returns>
    [When("{string} asks for the profile of {string}")]
    public async Task WhenAsksForTheProfileOf(string player, string character) =>
        _profileResult = await new GetCharacterProfileQueryHandler(_reader.Object, _clock.Object)
            .Handle(new GetCharacterProfileQuery(PlayerId(player).Value, CharacterIdOf(character).Value), CancellationToken.None);

    /// <summary>Validates the list query with an empty user identifier.</summary>
    [When("the characters list is validated without a user id")]
    public void WhenTheCharactersListIsValidatedWithoutAUserId() =>
        _validation = new GetMyCharactersQueryValidator().Validate(new GetMyCharactersQuery(Guid.Empty));

    /// <summary>Validates the profile query with one identifier left empty.</summary>
    /// <param name="identifier"><c>user id</c> or <c>character id</c>.</param>
    [When("the profile query is validated without a {string}")]
    public void WhenTheProfileQueryIsValidatedWithoutA(string identifier) =>
        _validation = new GetCharacterProfileQueryValidator().Validate(identifier == "user id"
            ? new GetCharacterProfileQuery(Guid.Empty, Guid.NewGuid())
            : new GetCharacterProfileQuery(Guid.NewGuid(), Guid.Empty));
    #endregion When Steps

    #region Then Steps
    /// <summary>Asserts that the list query succeeded with the listed characters, in order.</summary>
    /// <param name="characters">Table with the column <c>name</c>.</param>
    [Then("the list succeeds with these characters")]
    public void ThenTheListSucceedsWithTheseCharacters(DataTable characters)
    {
        var result = _listResult.ShouldNotBeNull();
        result.IsSuccess.ShouldBeTrue();
        result.Value.Select(character => character.Name).ShouldBe(characters.Rows.Select(row => row["name"]));
    }

    /// <summary>Asserts that the reader was asked for the player's characters with the clock's time.</summary>
    /// <param name="player">The player name.</param>
    [Then("the reader was asked for the characters of {string} at the clock's time")]
    public void ThenTheReaderWasAskedForTheCharactersOfAtTheClocksTime(string player)
    {
        _listResult.ShouldNotBeNull().Value.ShouldBeEmpty();
        _reader.Verify(reader => reader.ListOwnedAsync(PlayerId(player), Now, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>Asserts that the profile query returned the character's profile.</summary>
    /// <param name="character">The character name.</param>
    [Then("the profile of {string} is returned")]
    public void ThenTheProfileOfIsReturned(string character)
    {
        var result = _profileResult.ShouldNotBeNull();
        result.IsSuccess.ShouldBeTrue();
        result.Value.CharacterId.ShouldBe(CharacterIdOf(character).Value);
        result.Value.Name.ShouldBe(character);
    }

    /// <summary>Asserts that the profile query failed as not found.</summary>
    [Then("the profile query fails as not found")]
    public void ThenTheProfileQueryFailsAsNotFound()
    {
        var result = _profileResult.ShouldNotBeNull();
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(CharacterErrors.NotFound);
        result.ResultExceptionType.ShouldBe(ResultExceptionType.NotFound);
    }

    /// <summary>Asserts that validation failed on the named property.</summary>
    /// <param name="property">The expected property name.</param>
    [Then("the validation fails on {string}")]
    public void ThenTheValidationFailsOn(string property)
    {
        _validation.ShouldNotBeNull().IsValid.ShouldBeFalse();
        _validation.Errors.ShouldHaveSingleItem().PropertyName.ShouldBe(property);
    }
    #endregion Then Steps

    #region Private Helpers
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

    /// <summary>Returns the stable character identifier for a character name.</summary>
    /// <param name="character">The character name.</param>
    /// <returns>The character's identifier.</returns>
    private CharacterId CharacterIdOf(string character)
    {
        if (!_characters.TryGetValue(character, out var characterId))
        {
            characterId = new CharacterId(Guid.NewGuid());
            _characters[character] = characterId;
        }

        return characterId;
    }
    #endregion Private Helpers
}
