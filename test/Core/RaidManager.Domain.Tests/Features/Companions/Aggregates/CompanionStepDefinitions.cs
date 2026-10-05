using Pivot.Framework.Domain.Shared;
using Reqnroll;
using Shouldly;
using RaidManager.Domain.Features.Companions.Aggregates;
using RaidManager.Domain.Features.Companions.Enums;
using RaidManager.Domain.Features.Companions.Events;
using RaidManager.Domain.Features.Companions.ValueObjects;
using RaidManager.Domain.Features.Shared.Identifiers;

namespace RaidManager.Domain.Tests.Features.Companions.Aggregates;

/// <summary>Defines business-readable steps for the companion's token rules and revocation.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: Verifies ADR-0030's token rules: hourly last use, expiry after 180 days unused, and final revocation by its player only.
/// </remarks>
[Binding]
[Scope(Feature = "Companion token")]
public sealed class CompanionStepDefinitions
{
    #region Fields
    /// <summary>Stores when the companion was paired.</summary>
    private static readonly DateTimeOffset PairedAt = new(2026, 10, 4, 14, 0, 0, TimeSpan.Zero);

    /// <summary>Stores one stable user identifier per player name used in the scenario.</summary>
    private readonly Dictionary<string, UserId> _players = [];

    /// <summary>Stores the companion under test.</summary>
    private Companion _companion = null!;

    /// <summary>Stores when the latest request was made.</summary>
    private DateTimeOffset _requestedAt;

    /// <summary>Stores the result of the latest request.</summary>
    private Result<bool>? _use;

    /// <summary>Stores the result of the latest revocation.</summary>
    private Result? _revocation;
    #endregion Fields

    #region Properties
    /// <summary>Gets the captured request, failing the scenario if none ran.</summary>
    private Result<bool> UseResult => _use ?? throw new InvalidOperationException("No request in this scenario.");

    /// <summary>Gets the captured revocation, failing the scenario if none ran.</summary>
    private Result Revocation => _revocation ?? throw new InvalidOperationException("No revocation in this scenario.");
    #endregion Properties

    #region Given Steps
    /// <summary>Pairs a companion for a player through a confirmed pairing.</summary>
    /// <param name="player">The player name.</param>
    [Given("a companion paired for {string}")]
    public void GivenACompanionPairedFor(string player)
    {
        var pairing = CompanionPairing.Start(CredentialHash.Of("device-code"), PairingCode.Create("K7M4QX"), "BRYN-DESKTOP", PairedAt);
        pairing.Confirm(PlayerId(player), PairedAt).IsSuccess.ShouldBeTrue();
        _companion = pairing.Complete(CredentialHash.Of("device-token"), PairedAt).Value;
        ((Pivot.Framework.Domain.Primitives.IAggregateRoot)_companion).ClearDomainEvents();
    }

    /// <summary>Revokes the companion as an arrangement step.</summary>
    /// <param name="player">The player name.</param>
    [Given("{string} revoked the companion")]
    public void GivenRevokedTheCompanion(string player) =>
        _companion.Revoke(PlayerId(player), PairedAt).IsSuccess.ShouldBeTrue();

    /// <summary>Records an upload some hours after pairing as an arrangement step.</summary>
    /// <param name="hours">The hours after pairing.</param>
    [Given("the companion uploaded {int} hours after its pairing")]
    public void GivenTheCompanionUploadedHoursAfterItsPairing(int hours) => _companion.RecordUpload(PairedAt.AddHours(hours));
    #endregion Given Steps

    #region When Steps
    /// <summary>Makes a request some minutes after the last use.</summary>
    /// <param name="minutes">The minutes after the last use.</param>
    [When("the companion calls {int} minutes after its last use")]
    public void WhenTheCompanionCallsMinutesAfterItsLastUse(int minutes) => Use(PairedAt.AddMinutes(minutes));

    /// <summary>Makes a request some days after the last use.</summary>
    /// <param name="days">The days after the last use.</param>
    [When("the companion calls {int} days after its last use")]
    public void WhenTheCompanionCallsDaysAfterItsLastUse(int days) => Use(PairedAt.AddDays(days));

    /// <summary>Revokes the companion an hour after pairing.</summary>
    /// <param name="player">The player name.</param>
    [When("{string} revokes the companion")]
    public void WhenRevokesTheCompanion(string player) => _revocation = _companion.Revoke(PlayerId(player), PairedAt.AddHours(1));

    /// <summary>Records an upload some hours after pairing.</summary>
    /// <param name="hours">The hours after pairing.</param>
    [When("the companion uploads {int} hours after its pairing")]
    public void WhenTheCompanionUploadsHoursAfterItsPairing(int hours) => _companion.RecordUpload(PairedAt.AddHours(hours));
    #endregion When Steps

    #region Then Steps
    /// <summary>Asserts that the request was admitted.</summary>
    [Then("the companion is admitted")]
    public void ThenTheCompanionIsAdmitted() => UseResult.IsSuccess.ShouldBeTrue();

    /// <summary>Asserts that the request needs no write.</summary>
    [Then("its last use is unchanged")]
    public void ThenItsLastUseIsUnchanged()
    {
        UseResult.Value.ShouldBeFalse();
        _companion.LastUsedAtUtc.ShouldBe(PairedAt);
    }

    /// <summary>Asserts that the request moved last use.</summary>
    [Then("its last use moves to the request")]
    public void ThenItsLastUseMovesToTheRequest()
    {
        UseResult.Value.ShouldBeTrue();
        _companion.LastUsedAtUtc.ShouldBe(_requestedAt);
    }

    /// <summary>Asserts that the request was refused with the reason the companion shows.</summary>
    /// <param name="code">The expected error code.</param>
    [Then("the companion is refused with {string}")]
    public void ThenTheCompanionIsRefusedWith(string code)
    {
        UseResult.IsFailure.ShouldBeTrue();
        UseResult.Error.Code.ShouldBe(code);
        UseResult.ResultExceptionType.ShouldBe(ResultExceptionType.AuthenticationRequired);
    }

    /// <summary>Asserts the status at the latest request.</summary>
    /// <param name="status">The expected status.</param>
    [Then("its status is {word}")]
    public void ThenItsStatusIs(string status) =>
        _companion.StatusAt(_requestedAt == default ? PairedAt.AddHours(2) : _requestedAt).ShouldBe(Enum.Parse<CompanionStatus>(status));

    /// <summary>Asserts that the revocation succeeded.</summary>
    [Then("the revocation succeeds")]
    public void ThenTheRevocationSucceeds()
    {
        Revocation.IsSuccess.ShouldBeTrue();
        _companion.RevokedAtUtc.ShouldBe(PairedAt.AddHours(1));
        _companion.GetDomainEvents().OfType<CompanionRevoked>().ShouldHaveSingleItem();
    }

    /// <summary>Asserts the revocation failure.</summary>
    /// <param name="code">The expected error code.</param>
    /// <param name="type">The expected <see cref="ResultExceptionType"/> name.</param>
    [Then("the revocation fails with {string} as {word}")]
    public void ThenTheRevocationFailsWithAs(string code, string type)
    {
        Revocation.IsFailure.ShouldBeTrue();
        Revocation.Error.Code.ShouldBe(code);
        Revocation.ResultExceptionType.ShouldBe(Enum.Parse<ResultExceptionType>(type));
    }

    /// <summary>Asserts that the companion never uploaded.</summary>
    [Then("the companion has no last upload")]
    public void ThenTheCompanionHasNoLastUpload() => _companion.LastUploadAtUtc.ShouldBeNull();

    /// <summary>Asserts the companion's last upload.</summary>
    /// <param name="hours">The expected hours after pairing.</param>
    [Then("its last upload is {int} hours after its pairing")]
    public void ThenItsLastUploadIsHoursAfterItsPairing(int hours) => _companion.LastUploadAtUtc.ShouldBe(PairedAt.AddHours(hours));
    #endregion Then Steps

    #region Private Helpers
    /// <summary>Makes a request at a time.</summary>
    /// <param name="at">The request time.</param>
    private void Use(DateTimeOffset at)
    {
        _requestedAt = at;
        _use = _companion.Use(at);
    }

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
