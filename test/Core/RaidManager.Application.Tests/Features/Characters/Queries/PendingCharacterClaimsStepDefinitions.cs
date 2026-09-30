using Moq;
using Pivot.Framework.Domain.Shared;
using Reqnroll;
using Shouldly;
using RaidManager.Application.Features.Characters.Abstractions;
using RaidManager.Application.Features.Characters.Queries.GetPendingCharacterClaims;
using RaidManager.Domain.Features.Characters.Enums;
using RaidManager.Domain.Features.Shared.Enums;
using RaidManager.Domain.Features.Shared.Identifiers;

namespace RaidManager.Application.Tests.Features.Characters.Queries;

/// <summary>Defines business-readable steps for the pending character claims query.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-09-30<br/>
/// Purpose: Verifies that the query asks the read-only claim reader for the player's claims and validates its input.
/// </remarks>
[Binding]
[Scope(Feature = "Pending character claims")]
public sealed class PendingCharacterClaimsStepDefinitions
{
    #region Fields
    /// <summary>Stores one stable user identifier per player name used in the scenario.</summary>
    private readonly Dictionary<string, UserId> _players = [];

    /// <summary>Stores the claim reader double.</summary>
    private readonly Mock<ICharacterClaimReader> _reader = new();

    /// <summary>Stores the result of the latest query.</summary>
    private Result<IReadOnlyList<PendingCharacterClaimResponse>>? _result;

    /// <summary>Stores the result of the latest validation.</summary>
    private FluentValidation.Results.ValidationResult? _validation;
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="PendingCharacterClaimsStepDefinitions"/> class.</summary>
    public PendingCharacterClaimsStepDefinitions()
    {
        // A player the reader knows nothing about has no claims; Given steps add claims for specific players.
        _reader
            .Setup(reader => reader.ListAwaitingDecisionAsync(It.IsAny<UserId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<PendingCharacterClaimResponse>());
    }
    #endregion Constructors

    #region Properties
    /// <summary>Gets the captured query result, failing the scenario if no query ran.</summary>
    private Result<IReadOnlyList<PendingCharacterClaimResponse>> QueryResult =>
        _result ?? throw new InvalidOperationException("No query was sent in this scenario.");
    #endregion Properties

    #region Given Steps
    /// <summary>Makes the reader return the listed claims for the player.</summary>
    /// <param name="player">The player name.</param>
    /// <param name="claims">Table with the columns <c>name</c> and <c>state</c>.</param>
    [Given("the claim reader has these claims for {string}")]
    public void GivenTheClaimReaderHasTheseClaimsFor(string player, DataTable claims)
    {
        IReadOnlyList<PendingCharacterClaimResponse> responses = [.. claims.Rows.Select(row => new PendingCharacterClaimResponse(
            Guid.NewGuid(),
            WarmaneRealm.Icecrown,
            row["name"],
            WowClass.Mage,
            WowRace.Human,
            80,
            Enum.Parse<CharacterClaimState>(row["state"]),
            DateTimeOffset.UtcNow))];
        _reader.Setup(reader => reader.ListAwaitingDecisionAsync(PlayerId(player), It.IsAny<CancellationToken>())).ReturnsAsync(responses);
    }
    #endregion Given Steps

    #region When Steps
    /// <summary>Sends the query for the player.</summary>
    /// <param name="player">The player name.</param>
    /// <returns>A task that completes when the query has been handled.</returns>
    [When("{string} asks for her pending character claims")]
    public async Task WhenAsksForHerPendingCharacterClaims(string player)
    {
        _result = await new GetPendingCharacterClaimsQueryHandler(_reader.Object)
            .Handle(new GetPendingCharacterClaimsQuery(PlayerId(player).Value), CancellationToken.None);
    }

    /// <summary>Validates the query with an empty user identifier.</summary>
    [When("the query is validated without a user id")]
    public void WhenTheQueryIsValidatedWithoutAUserId() =>
        _validation = new GetPendingCharacterClaimsQueryValidator().Validate(new GetPendingCharacterClaimsQuery(Guid.Empty));
    #endregion When Steps

    #region Then Steps
    /// <summary>Asserts that the query succeeded with the listed claims, in order.</summary>
    /// <param name="claims">Table with the columns <c>name</c> and <c>state</c>.</param>
    [Then("the query succeeds with these claims")]
    public void ThenTheQuerySucceedsWithTheseClaims(DataTable claims)
    {
        QueryResult.IsSuccess.ShouldBeTrue();
        QueryResult.Value.Select(claim => (claim.Name, claim.ClaimState.ToString()))
            .ShouldBe(claims.Rows.Select(row => (row["name"], row["state"])));
    }

    /// <summary>Asserts that the query succeeded with an empty list.</summary>
    [Then("the query succeeds with no claims")]
    public void ThenTheQuerySucceedsWithNoClaims()
    {
        QueryResult.IsSuccess.ShouldBeTrue();
        QueryResult.Value.ShouldBeEmpty();
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
    #endregion Private Helpers
}
