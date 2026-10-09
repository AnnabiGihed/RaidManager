using Moq;
using Pivot.Framework.Domain.Repositories;
using Pivot.Framework.Domain.Shared;
using Reqnroll;
using Shouldly;
using RaidManager.Application.Features.Characters.Commands.RemoveMyCharacters;
using RaidManager.Domain.Features.Characters.Aggregates;
using RaidManager.Domain.Features.Characters.Enums;
using RaidManager.Domain.Features.Characters.Repositories;
using RaidManager.Domain.Features.Characters.ValueObjects;
using RaidManager.Domain.Features.Companions.Aggregates;
using RaidManager.Domain.Features.Companions.Repositories;
using RaidManager.Domain.Features.Companions.ValueObjects;
using RaidManager.Domain.Features.Shared.Enums;
using RaidManager.Domain.Features.Shared.Identifiers;

namespace RaidManager.Application.Tests.Features.Characters.Commands;

/// <summary>Defines business-readable steps for removing a player's characters on dev and test.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-09<br/>
/// Purpose: Verifies that the removal deletes what is only the player's, withdraws their other claims, and commits once
/// (#613).
/// </remarks>
[Binding]
[Scope(Feature = "Remove my characters")]
public sealed class RemoveMyCharactersStepDefinitions
{
    #region Fields
    /// <summary>Stores one stable user identifier per player name used in the scenario.</summary>
    private readonly Dictionary<string, UserId> _players = [];

    /// <summary>Stores the characters by name.</summary>
    private readonly Dictionary<string, Character> _characters = [];

    /// <summary>Stores the repository double.</summary>
    private readonly Mock<ICharacterRepository> _repository = new();

    /// <summary>Stores the companion repository double.</summary>
    private readonly Mock<ICompanionRepository> _companionRepository = new();

    /// <summary>Stores the player's companions.</summary>
    private readonly List<Companion> _companions = [];

    /// <summary>Stores the unit-of-work double that records commits.</summary>
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    /// <summary>Stores the result the next commit returns.</summary>
    private Result _commitResult = Result.Success();

    /// <summary>Stores the result of the removal.</summary>
    private Result<int>? _removal;

    /// <summary>Stores the result of the latest validation.</summary>
    private FluentValidation.Results.ValidationResult? _validation;
    #endregion Fields

    #region Properties
    /// <summary>Gets the captured removal, failing the scenario if none ran.</summary>
    private Result<int> Removal => _removal ?? throw new InvalidOperationException("No removal was sent in this scenario.");
    #endregion Properties

    #region Given Steps
    /// <summary>Makes the player the owner of a character.</summary>
    /// <param name="player">The player name.</param>
    /// <param name="name">The character name.</param>
    [Given("{string} owns a character {string}")]
    public void GivenOwnsACharacter(string player, string name) => Owned(player, name);

    /// <summary>Gives the player a pending claim on a new character.</summary>
    /// <param name="player">The player name.</param>
    /// <param name="name">The character name.</param>
    [Given("{string} claimed a new character {string}")]
    public void GivenClaimedANewCharacter(string player, string name) =>
        NewCharacter(name).RequestClaim(PlayerId(player), DateTimeOffset.UtcNow).IsSuccess.ShouldBeTrue();

    /// <summary>Makes another player the owner of a character the player claimed too.</summary>
    /// <param name="owner">The owner's name.</param>
    /// <param name="name">The character name.</param>
    /// <param name="player">The player who claimed it too.</param>
    [Given("{string} owns a character {string} that {string} also claimed")]
    public void GivenOwnsACharacterThatAlsoClaimed(string owner, string name, string player) =>
        Owned(owner, name).RequestClaim(PlayerId(player), DateTimeOffset.UtcNow).IsSuccess.ShouldBeTrue();

    /// <summary>Pairs two companions for the player and revokes the second.</summary>
    /// <param name="player">The player name.</param>
    [Given("{string} has an active companion and a revoked one")]
    public void GivenHasAnActiveCompanionAndARevokedOne(string player)
    {
        _companions.Add(Paired(player, "active"));
        var revoked = Paired(player, "revoked");
        revoked.Revoke(PlayerId(player), DateTimeOffset.UtcNow).IsSuccess.ShouldBeTrue();
        _companions.Add(revoked);
    }

    /// <summary>Makes the next commit fail.</summary>
    [Given("the commit will fail")]
    public void GivenTheCommitWillFail() =>
        _commitResult = Result.Failure(new Error("Commit.Failed", "The database rejected the change."));
    #endregion Given Steps

    #region When Steps
    /// <summary>Removes the player's characters through the handler.</summary>
    /// <param name="player">The player name.</param>
    /// <returns>A task that completes when the removal ran.</returns>
    [When("{string} removes all their characters")]
    public async Task WhenRemovesAllTheirCharacters(string player)
    {
        var userId = PlayerId(player);
        _repository.Setup(repository => repository.ListOwnedOrClaimedByAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([.. _characters.Values]);
        _companionRepository.Setup(repository => repository.ListByUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([.. _companions]);
        _unitOfWork.Setup(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(_commitResult);
        _removal = await new RemoveMyCharactersCommandHandler(_repository.Object, _companionRepository.Object, _unitOfWork.Object, TimeProvider.System)
            .Handle(new RemoveMyCharactersCommand(userId.Value), CancellationToken.None);
    }

    /// <summary>Validates a removal without a user id.</summary>
    [When("a removal is validated without a user id")]
    public void WhenARemovalIsValidatedWithoutAUserId() =>
        _validation = new RemoveMyCharactersCommandValidator().Validate(new RemoveMyCharactersCommand(Guid.Empty));
    #endregion When Steps

    #region Then Steps
    /// <summary>Asserts the removal succeeded with the number of characters the player no longer has.</summary>
    /// <param name="count">The expected number.</param>
    [Then("the removal succeeds with {int} characters")]
    public void ThenTheRemovalSucceedsWith(int count)
    {
        Removal.IsSuccess.ShouldBeTrue();
        Removal.Value.ShouldBe(count);
    }

    /// <summary>Asserts the removal failed with the given error code.</summary>
    /// <param name="code">The expected error code.</param>
    [Then("the removal fails with {string}")]
    public void ThenTheRemovalFailsWith(string code)
    {
        Removal.IsFailure.ShouldBeTrue();
        Removal.Error.Code.ShouldBe(code);
    }

    /// <summary>Asserts both characters were deleted.</summary>
    /// <param name="first">The first character.</param>
    /// <param name="second">The second character.</param>
    [Then("{string} and {string} are deleted")]
    public void ThenAreDeleted(string first, string second)
    {
        foreach (var name in new[] { first, second })
        {
            _repository.Verify(repository => repository.DeleteAsync(_characters[name], It.IsAny<CancellationToken>()), Times.Once);
        }
    }

    /// <summary>Asserts a character stays with its owner, without the player's claim, and is updated.</summary>
    /// <param name="name">The character name.</param>
    /// <param name="owner">The owner's name.</param>
    /// <param name="player">The player whose claim went.</param>
    [Then("{string} stays with {string}, without the claim of {string}")]
    public void ThenStaysWithWithoutTheClaimOf(string name, string owner, string player)
    {
        var character = _characters[name];
        character.OwnerId.ShouldBe(PlayerId(owner));
        character.Claims.ShouldNotContain(claim => claim.RequestedByUserId == PlayerId(player));
        _repository.Verify(repository => repository.DeleteAsync(character, It.IsAny<CancellationToken>()), Times.Never);
        _repository.Verify(repository => repository.UpdateAsync(character, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>Asserts that only the active companion was asked to sync again, and updated.</summary>
    [Then("only the active companion is asked to sync again")]
    public void ThenOnlyTheActiveCompanionIsAskedToSyncAgain()
    {
        _companions[0].SyncAgainRequestedAtUtc.ShouldNotBeNull();
        _companions[1].SyncAgainRequestedAtUtc.ShouldBeNull();
        _companionRepository.Verify(repository => repository.UpdateAsync(_companions[0], It.IsAny<CancellationToken>()), Times.Once);
        _companionRepository.Verify(repository => repository.UpdateAsync(_companions[1], It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>Asserts the removal was committed exactly once.</summary>
    [Then("the removal is committed once")]
    public void ThenTheRemovalIsCommittedOnce() =>
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

    /// <summary>Asserts the validation failed on one property.</summary>
    /// <param name="property">The property name.</param>
    [Then("the validation fails on {string}")]
    public void ThenTheValidationFailsOn(string property)
    {
        _validation.ShouldNotBeNull().IsValid.ShouldBeFalse();
        _validation.Errors.ShouldHaveSingleItem().PropertyName.ShouldBe(property);
    }
    #endregion Then Steps

    #region Private Helpers
    /// <summary>Imports a new character and remembers it by name.</summary>
    /// <param name="name">The character name.</param>
    /// <returns>The character.</returns>
    private Character NewCharacter(string name)
    {
        var character = Character.Import(WarmaneRealm.Icecrown, CharacterName.Create(name), WowClass.Paladin, WowRace.Human, Faction.Alliance, 80);
        _characters[name] = character;
        return character;
    }

    /// <summary>Imports a character the player owns through an approved claim.</summary>
    /// <param name="player">The owner's name.</param>
    /// <param name="name">The character name.</param>
    /// <returns>The character.</returns>
    private Character Owned(string player, string name)
    {
        var character = NewCharacter(name);
        character.RequestClaim(PlayerId(player), DateTimeOffset.UtcNow).IsSuccess.ShouldBeTrue();
        character.ApproveClaim(PlayerId(player), DateTimeOffset.UtcNow).IsSuccess.ShouldBeTrue();
        return character;
    }

    /// <summary>Pairs a companion for the player.</summary>
    /// <param name="player">The player name.</param>
    /// <param name="token">A token that tells the companions apart.</param>
    /// <returns>The companion.</returns>
    private Companion Paired(string player, string token)
    {
        var code = PairingCode.Create(token == "active" ? "K7M4QX" : "P3R8TW");
        var pairing = CompanionPairing.Start(CredentialHash.Of(token + "-device-code"), code, $"PC-{token}", DateTimeOffset.UtcNow);
        pairing.Confirm(PlayerId(player), DateTimeOffset.UtcNow).IsSuccess.ShouldBeTrue();
        return pairing.Complete(CredentialHash.Of(token), DateTimeOffset.UtcNow).Value;
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
