using Pivot.Framework.Domain.Shared;
using Reqnroll;
using Shouldly;
using RaidManager.Domain.Features.Characters.Aggregates;
using RaidManager.Domain.Features.Characters.Enums;
using RaidManager.Domain.Features.Characters.ValueObjects;
using RaidManager.Domain.Features.Shared.Enums;
using RaidManager.Domain.Features.Shared.Identifiers;

namespace RaidManager.Domain.Tests.Features.Characters.Aggregates;

/// <summary>Defines business-readable step definitions for the character claim lifecycle.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-09-29<br/>
/// Purpose: Verifies that uploads only open claims, approval is explicit, rejection sticks and ownership never transfers.
/// </remarks>
[Binding]
[Scope(Feature = "Character claims")]
public sealed class CharacterClaimsStepDefinitions
{
    #region Fields
    /// <summary>Stores one stable user identifier per player name used in the scenario.</summary>
    private readonly Dictionary<string, UserId> _players = [];

    /// <summary>Stores the character under test.</summary>
    private Character _character = null!;

    /// <summary>Stores the result of the latest approve or reject decision.</summary>
    private Result? _decision;
    #endregion Fields

    #region Properties
    /// <summary>Gets the captured decision, failing the scenario if no decision step ran.</summary>
    private Result Decision => _decision ?? throw new InvalidOperationException("No claim decision was made in this scenario.");
    #endregion Properties

    #region Given Steps
    /// <summary>Imports a level 80 character on Icecrown with no claims.</summary>
    [Given("a level 80 character on Icecrown")]
    public void GivenALevel80CharacterOnIcecrown() =>
        _character = Character.Import(WarmaneRealm.Icecrown, CharacterName.Create("Claimtest"), WowClass.Paladin, WowRace.Human, Faction.Alliance, 80);

    /// <summary>Records a companion upload that discovered the character for the player.</summary>
    /// <param name="player">The player name.</param>
    [Given("the companion of {string} uploaded the character")]
    public void GivenTheCompanionOfUploadedTheCharacter(string player) =>
        _character.RequestClaim(PlayerId(player), DateTimeOffset.UtcNow).IsSuccess.ShouldBeTrue();

    /// <summary>Makes the player the character's owner through the real claim and approval flow.</summary>
    /// <param name="player">The player name.</param>
    [Given("{string} owns the character")]
    public void GivenOwnsTheCharacter(string player)
    {
        GivenTheCompanionOfUploadedTheCharacter(player);
        GivenApprovedTheClaim(player);
    }

    /// <summary>Approves the player's pending claim as an arrangement step.</summary>
    /// <param name="player">The player name.</param>
    [Given("{string} approved the claim")]
    public void GivenApprovedTheClaim(string player) =>
        _character.ApproveClaim(PlayerId(player), DateTimeOffset.UtcNow).IsSuccess.ShouldBeTrue();

    /// <summary>Rejects the player's pending claim as an arrangement step.</summary>
    /// <param name="player">The player name.</param>
    [Given("{string} rejected the claim")]
    public void GivenRejectedTheClaim(string player) =>
        _character.RejectClaim(PlayerId(player), DateTimeOffset.UtcNow).IsSuccess.ShouldBeTrue();
    #endregion Given Steps

    #region When Steps
    /// <summary>Uploads the character through the player's companion.</summary>
    /// <param name="player">The player name.</param>
    [When("the companion of {string} uploads the character")]
    public void WhenTheCompanionOfUploadsTheCharacter(string player) =>
        _character.RequestClaim(PlayerId(player), DateTimeOffset.UtcNow).IsSuccess.ShouldBeTrue();

    /// <summary>Approves the player's claim and captures the decision.</summary>
    /// <param name="player">The player name.</param>
    [When("{string} approves the claim")]
    public void WhenApprovesTheClaim(string player) =>
        _decision = _character.ApproveClaim(PlayerId(player), DateTimeOffset.UtcNow);
    #endregion When Steps

    #region Then Steps
    /// <summary>Asserts the state of the player's claim.</summary>
    /// <param name="player">The player name.</param>
    /// <param name="expectedState">The expected claim state name.</param>
    [Then("the claim of {string} is {string}")]
    public void ThenTheClaimOfIs(string player, string expectedState) =>
        _character.Claims.Single(claim => claim.RequestedByUserId == PlayerId(player)).State
            .ShouldBe(Enum.Parse<CharacterClaimState>(expectedState));

    /// <summary>Asserts that nobody owns the character.</summary>
    [Then("the character has no owner")]
    public void ThenTheCharacterHasNoOwner()
    {
        _character.OwnerId.ShouldBeNull();
        _character.IsOwnershipVerified.ShouldBeFalse();
    }

    /// <summary>Asserts that the player owns the character.</summary>
    /// <param name="player">The player name.</param>
    [Then("{string} owns the character")]
    public void ThenOwnsTheCharacter(string player)
    {
        _character.OwnerId.ShouldBe(PlayerId(player));
        _character.IsOwnershipVerified.ShouldBeTrue();
    }

    /// <summary>Asserts that the latest decision succeeded.</summary>
    [Then("the claim decision succeeds")]
    public void ThenTheClaimDecisionSucceeds() => Decision.IsSuccess.ShouldBeTrue();

    /// <summary>Asserts that the latest decision failed with the given error code.</summary>
    /// <param name="expectedCode">The expected error code.</param>
    [Then("the claim decision fails with {string}")]
    public void ThenTheClaimDecisionFailsWith(string expectedCode)
    {
        Decision.IsFailure.ShouldBeTrue();
        Decision.Error.Code.ShouldBe(expectedCode);
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
