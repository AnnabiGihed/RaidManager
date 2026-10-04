using Pivot.Framework.Domain.Shared;
using Reqnroll;
using Shouldly;
using RaidManager.Domain.Features.Companions.Aggregates;
using RaidManager.Domain.Features.Companions.Enums;
using RaidManager.Domain.Features.Companions.Events;
using RaidManager.Domain.Features.Companions.ValueObjects;
using RaidManager.Domain.Features.Shared.Identifiers;

namespace RaidManager.Domain.Tests.Features.Companions.Aggregates;

/// <summary>Defines business-readable steps for confirming a companion pairing and collecting its token.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: Verifies the single-use, ten-minute pairing of ADR-0030: one confirmation by the player, one token for the companion.
/// </remarks>
[Binding]
[Scope(Feature = "Companion pairing")]
public sealed class CompanionPairingStepDefinitions
{
    #region Fields
    /// <summary>Stores when the companion asked to be paired.</summary>
    private static readonly DateTimeOffset RequestedAt = new(2026, 10, 4, 14, 0, 0, TimeSpan.Zero);

    /// <summary>Stores one stable user identifier per player name used in the scenario.</summary>
    private readonly Dictionary<string, UserId> _players = [];

    /// <summary>Stores the pairing under test.</summary>
    private CompanionPairing _pairing = null!;

    /// <summary>Stores the result of the latest confirmation.</summary>
    private Result? _confirmation;

    /// <summary>Stores the result of the latest collection.</summary>
    private Result<Companion>? _collection;
    #endregion Fields

    #region Properties
    /// <summary>Gets the captured confirmation, failing the scenario if none ran.</summary>
    private Result Confirmation => _confirmation ?? throw new InvalidOperationException("No confirmation in this scenario.");

    /// <summary>Gets the captured collection, failing the scenario if none ran.</summary>
    private Result<Companion> Collection => _collection ?? throw new InvalidOperationException("No collection in this scenario.");
    #endregion Properties

    #region Given Steps
    /// <summary>Starts a pairing for a companion with a fixed code.</summary>
    /// <param name="label">The computer label.</param>
    [Given("a companion asked to be paired as {string}")]
    public void GivenACompanionAskedToBePairedAs(string label) => _pairing = Start(label);

    /// <summary>Confirms the pairing a minute after the request as an arrangement step.</summary>
    /// <param name="player">The player name.</param>
    [Given("{string} confirmed the pairing")]
    public void GivenConfirmedThePairing(string player) =>
        _pairing.Confirm(PlayerId(player), RequestedAt.AddMinutes(1)).IsSuccess.ShouldBeTrue();

    /// <summary>Collects the token a minute after the request as an arrangement step.</summary>
    [Given("the companion collected its token")]
    public void GivenTheCompanionCollectedItsToken() =>
        _pairing.Complete(CredentialHash.Of("first-token"), RequestedAt.AddMinutes(1)).IsSuccess.ShouldBeTrue();
    #endregion Given Steps

    #region When Steps
    /// <summary>Starts another pairing with the given label.</summary>
    /// <param name="label">The computer label.</param>
    [When("a companion asks to be paired as {string}")]
    public void WhenACompanionAsksToBePairedAs(string label) => _pairing = Start(label);

    /// <summary>Confirms the pairing some minutes after the request.</summary>
    /// <param name="player">The player name.</param>
    /// <param name="minutes">The minutes after the request.</param>
    [When("{string} confirms the pairing {int} minutes after the request")]
    public void WhenConfirmsThePairingMinutesAfterTheRequest(string player, int minutes) =>
        _confirmation = _pairing.Confirm(PlayerId(player), RequestedAt.AddMinutes(minutes));

    /// <summary>Collects the token some minutes after the request.</summary>
    /// <param name="minutes">The minutes after the request.</param>
    [When("the companion collects its token {int} minutes after the request")]
    public void WhenTheCompanionCollectsItsTokenMinutesAfterTheRequest(int minutes) =>
        _collection = _pairing.Complete(CredentialHash.Of("device-token"), RequestedAt.AddMinutes(minutes));
    #endregion When Steps

    #region Then Steps
    /// <summary>Asserts the expiry.</summary>
    /// <param name="minutes">The expected lifetime in minutes.</param>
    [Then("the pairing expires {int} minutes after the request")]
    public void ThenThePairingExpiresMinutesAfterTheRequest(int minutes)
    {
        _pairing.ExpiresAtUtc.ShouldBe(RequestedAt.AddMinutes(minutes));
        _pairing.IsExpired(RequestedAt.AddMinutes(minutes).AddTicks(-1)).ShouldBeFalse();
        _pairing.IsExpired(RequestedAt.AddMinutes(minutes)).ShouldBeTrue();
    }

    /// <summary>Asserts the code as shown.</summary>
    /// <param name="code">The expected code.</param>
    [Then("the pairing shows the code {string}")]
    public void ThenThePairingShowsTheCode(string code) => _pairing.Code.ToString().ShouldBe(code);

    /// <summary>Asserts that the confirmation succeeded and raised its event.</summary>
    [Then("the confirmation succeeds")]
    public void ThenTheConfirmationSucceeds()
    {
        Confirmation.IsSuccess.ShouldBeTrue();
        _pairing.GetDomainEvents().OfType<CompanionPairingConfirmed>().ShouldHaveSingleItem();
    }

    /// <summary>Asserts the confirmation failure.</summary>
    /// <param name="code">The expected error code.</param>
    /// <param name="type">The expected <see cref="ResultExceptionType"/> name.</param>
    [Then("the confirmation fails with {string} as {word}")]
    public void ThenTheConfirmationFailsWithAs(string code, string type)
    {
        Confirmation.IsFailure.ShouldBeTrue();
        Confirmation.Error.Code.ShouldBe(code);
        Confirmation.ResultExceptionType.ShouldBe(Enum.Parse<ResultExceptionType>(type));
    }

    /// <summary>Asserts who confirmed the pairing.</summary>
    /// <param name="player">The player name.</param>
    [Then("the pairing is confirmed by {string}")]
    public void ThenThePairingIsConfirmedBy(string player)
    {
        _pairing.State.ShouldBe(CompanionPairingState.Confirmed);
        _pairing.ConfirmedByUserId.ShouldBe(PlayerId(player));
    }

    /// <summary>Asserts the collection failure, which the device grant answers as a bad request.</summary>
    /// <param name="code">The expected error code.</param>
    [Then("the collection fails with {string}")]
    public void ThenTheCollectionFailsWith(string code)
    {
        Collection.IsFailure.ShouldBeTrue();
        Collection.Error.Code.ShouldBe(code);
        Collection.ResultExceptionType.ShouldBe(ResultExceptionType.ValidationError);
    }

    /// <summary>Asserts the paired companion.</summary>
    /// <param name="player">The player name.</param>
    /// <param name="label">The expected label.</param>
    [Then("the companion is paired for {string} as {string}")]
    public void ThenTheCompanionIsPairedForAs(string player, string label)
    {
        var companion = Collection.Value;
        companion.UserId.ShouldBe(PlayerId(player));
        companion.Label.ShouldBe(label);
        companion.TokenHash.ShouldBe(CredentialHash.Of("device-token"));
        companion.StatusAt(RequestedAt.AddMinutes(1)).ShouldBe(CompanionStatus.Active);
        companion.GetDomainEvents().OfType<CompanionPaired>().ShouldHaveSingleItem();
    }

    /// <summary>Asserts that the pairing can't be used again.</summary>
    [Then("the pairing is completed")]
    public void ThenThePairingIsCompleted()
    {
        _pairing.State.ShouldBe(CompanionPairingState.Completed);
        _pairing.CompanionId.ShouldBe(Collection.Value.Id);
    }

    /// <summary>Asserts the kept label.</summary>
    /// <param name="label">The expected label.</param>
    [Then("the pairing's computer label is {string}")]
    public void ThenThePairingsComputerLabelIs(string label) => _pairing.ComputerLabel.ShouldBe(label);
    #endregion Then Steps

    #region Private Helpers
    /// <summary>Starts a pairing with a fixed code and device code.</summary>
    /// <param name="label">The computer label.</param>
    /// <returns>The pairing.</returns>
    private static CompanionPairing Start(string label) =>
        CompanionPairing.Start(CredentialHash.Of("device-code"), PairingCode.Create("K7M4QX"), label, RequestedAt);

    /// <summary>Gets or creates the identifier of a named player.</summary>
    /// <param name="player">The player name.</param>
    /// <returns>The player's identifier.</returns>
    private UserId PlayerId(string player)
    {
        if (!_players.TryGetValue(player, out var id))
        {
            id = new UserId(Guid.NewGuid());
            _players[player] = id;
        }

        return id;
    }
    #endregion Private Helpers
}
