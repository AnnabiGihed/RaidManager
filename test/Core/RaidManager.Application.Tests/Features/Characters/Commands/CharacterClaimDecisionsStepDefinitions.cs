using Moq;
using Pivot.Framework.Domain.Repositories;
using Pivot.Framework.Domain.Shared;
using Reqnroll;
using Shouldly;
using RaidManager.Application.Features.Characters.Commands.ApproveCharacterClaim;
using RaidManager.Application.Features.Characters.Commands.RejectCharacterClaim;
using RaidManager.Domain.Features.Characters.Aggregates;
using RaidManager.Domain.Features.Characters.Enums;
using RaidManager.Domain.Features.Characters.Repositories;
using RaidManager.Domain.Features.Characters.ValueObjects;
using RaidManager.Domain.Features.Shared.Enums;
using RaidManager.Domain.Features.Shared.Identifiers;

namespace RaidManager.Application.Tests.Features.Characters.Commands;

/// <summary>Defines business-readable steps for approving and rejecting character claims through the application layer.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-09-30<br/>
/// Purpose: Verifies that the claim commands load the character, apply the aggregate's decision, and commit only what must be kept.
/// </remarks>
[Binding]
[Scope(Feature = "Character claim decisions")]
public sealed class CharacterClaimDecisionsStepDefinitions
{
    #region Fields
    /// <summary>Stores one stable user identifier per player name used in the scenario.</summary>
    private readonly Dictionary<string, UserId> _players = [];

    /// <summary>Stores the repository double that returns the character under test.</summary>
    private readonly Mock<ICharacterRepository> _characters = new();

    /// <summary>Stores the unit-of-work double that records commits.</summary>
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    /// <summary>Stores the character under test.</summary>
    private Character _character = null!;

    /// <summary>Stores the result the next commit returns.</summary>
    private Result _commitResult = Result.Success();

    /// <summary>Stores the result of the latest command.</summary>
    private Result? _decision;

    /// <summary>Stores the result of the latest validation.</summary>
    private FluentValidation.Results.ValidationResult? _validation;
    #endregion Fields

    #region Properties
    /// <summary>Gets the captured decision, failing the scenario if no command ran.</summary>
    private Result Decision => _decision ?? throw new InvalidOperationException("No claim command was sent in this scenario.");
    #endregion Properties

    #region Given Steps
    /// <summary>Imports a character that the player's companion discovered, opening a pending claim.</summary>
    /// <param name="player">The player name.</param>
    [Given("a character discovered by the companion of {string}")]
    public void GivenACharacterDiscoveredByTheCompanionOf(string player)
    {
        _character = Character.Import(WarmaneRealm.Icecrown, CharacterName.Create("Claimtest"), WowClass.Paladin, WowRace.Human, Faction.Alliance, 80);
        _character.RequestClaim(PlayerId(player), DateTimeOffset.UtcNow).IsSuccess.ShouldBeTrue();
        _characters
            .Setup(repository => repository.FindByIdAsync(It.IsAny<CharacterId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CharacterId id, CancellationToken _) => id == _character.Id ? _character : null);
        _unitOfWork
            .Setup(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => _commitResult);
    }

    /// <summary>Rejects the player's claim directly on the aggregate as an arrangement step.</summary>
    /// <param name="player">The player name.</param>
    [Given("{string} rejected the claim")]
    public void GivenRejectedTheClaim(string player) =>
        _character.RejectClaim(PlayerId(player), DateTimeOffset.UtcNow).IsSuccess.ShouldBeTrue();

    /// <summary>Makes the player the character's owner through the real claim and approval flow.</summary>
    /// <param name="player">The player name.</param>
    [Given("{string} owns the character")]
    public void GivenOwnsTheCharacter(string player)
    {
        _character.RequestClaim(PlayerId(player), DateTimeOffset.UtcNow).IsSuccess.ShouldBeTrue();
        _character.ApproveClaim(PlayerId(player), DateTimeOffset.UtcNow).IsSuccess.ShouldBeTrue();
    }

    /// <summary>Makes the next commit fail.</summary>
    [Given("the commit will fail")]
    public void GivenTheCommitWillFail() =>
        _commitResult = Result.Failure(new Error("Commit.Failed", "The database rejected the change."));
    #endregion Given Steps

    #region When Steps
    /// <summary>Sends the approval command for the character under test.</summary>
    /// <param name="player">The player name.</param>
    /// <returns>A task that completes when the command has been handled.</returns>
    [When("{string} approves the claim")]
    public async Task WhenApprovesTheClaim(string player) =>
        _decision = await ApproveHandler().Handle(new ApproveCharacterClaimCommand(_character.Id.Value, PlayerId(player).Value), CancellationToken.None);

    /// <summary>Sends the approval command for a character that does not exist.</summary>
    /// <param name="player">The player name.</param>
    /// <returns>A task that completes when the command has been handled.</returns>
    [When("{string} approves the claim on an unknown character")]
    public async Task WhenApprovesTheClaimOnAnUnknownCharacter(string player) =>
        _decision = await ApproveHandler().Handle(new ApproveCharacterClaimCommand(Guid.NewGuid(), PlayerId(player).Value), CancellationToken.None);

    /// <summary>Sends the rejection command for the character under test.</summary>
    /// <param name="player">The player name.</param>
    /// <returns>A task that completes when the command has been handled.</returns>
    [When("{string} rejects the claim")]
    public async Task WhenRejectsTheClaim(string player) =>
        _decision = await RejectHandler().Handle(new RejectCharacterClaimCommand(_character.Id.Value, PlayerId(player).Value), CancellationToken.None);

    /// <summary>Validates a decision command with one identifier left empty.</summary>
    /// <param name="decision">Either <c>approval</c> or <c>rejection</c>.</param>
    /// <param name="identifier">Either <c>character id</c> or <c>user id</c>.</param>
    [When("a {word} is validated without a {word} id")]
    public void WhenAIsValidatedWithoutAnId(string decision, string identifier)
    {
        var characterId = identifier == "character" ? Guid.Empty : Guid.NewGuid();
        var userId = identifier == "user" ? Guid.Empty : Guid.NewGuid();
        _validation = decision == "approval"
            ? new ApproveCharacterClaimCommandValidator().Validate(new ApproveCharacterClaimCommand(characterId, userId))
            : new RejectCharacterClaimCommandValidator().Validate(new RejectCharacterClaimCommand(characterId, userId));
    }
    #endregion When Steps

    #region Then Steps
    /// <summary>Asserts that the latest command succeeded.</summary>
    [Then("the decision succeeds")]
    public void ThenTheDecisionSucceeds() => Decision.IsSuccess.ShouldBeTrue();

    /// <summary>Asserts the failure code and its HTTP-relevant type.</summary>
    /// <param name="code">The expected error code.</param>
    /// <param name="type">The expected <see cref="ResultExceptionType"/> name.</param>
    [Then("the decision fails with {string} as {word}")]
    public void ThenTheDecisionFailsWithAs(string code, string type)
    {
        Decision.IsFailure.ShouldBeTrue();
        Decision.Error.Code.ShouldBe(code);
        Decision.ResultExceptionType.ShouldBe(Enum.Parse<ResultExceptionType>(type));
    }

    /// <summary>Asserts which player owns the character.</summary>
    /// <param name="player">The player name.</param>
    [Then("{string} owns the character")]
    public void ThenOwnsTheCharacter(string player) => _character.OwnerId.ShouldBe(PlayerId(player));

    /// <summary>Asserts that nobody owns the character.</summary>
    [Then("the character has no owner")]
    public void ThenTheCharacterHasNoOwner() => _character.OwnerId.ShouldBeNull();

    /// <summary>Asserts that the character was updated and committed once.</summary>
    [Then("the decision is committed")]
    public void ThenTheDecisionIsCommitted()
    {
        _characters.Verify(repository => repository.UpdateAsync(_character, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>Asserts that nothing was updated or committed.</summary>
    [Then("nothing is committed")]
    public void ThenNothingIsCommitted()
    {
        _characters.Verify(repository => repository.UpdateAsync(It.IsAny<Character>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
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
    /// <summary>Creates the approval handler over the test doubles.</summary>
    /// <returns>The handler under test.</returns>
    private ApproveCharacterClaimCommandHandler ApproveHandler() => new(_characters.Object, _unitOfWork.Object, TimeProvider.System);

    /// <summary>Creates the rejection handler over the test doubles.</summary>
    /// <returns>The handler under test.</returns>
    private RejectCharacterClaimCommandHandler RejectHandler() => new(_characters.Object, _unitOfWork.Object, TimeProvider.System);

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
