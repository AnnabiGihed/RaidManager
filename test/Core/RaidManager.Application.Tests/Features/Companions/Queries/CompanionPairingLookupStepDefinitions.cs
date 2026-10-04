using Moq;
using Pivot.Framework.Domain.Shared;
using Reqnroll;
using Shouldly;
using RaidManager.Application.Features.Companions.Queries.GetCompanionPairing;
using RaidManager.Domain.Features.Companions.Aggregates;
using RaidManager.Domain.Features.Companions.Enums;
using RaidManager.Domain.Features.Companions.Repositories;
using RaidManager.Domain.Features.Companions.ValueObjects;
using RaidManager.Domain.Features.Shared.Identifiers;

namespace RaidManager.Application.Tests.Features.Companions.Queries;

/// <summary>Defines business-readable steps for describing a pairing before the player confirms it.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: Verifies that the lookup answers the same failures as the confirmation, so the page can say a code expired or was used.
/// </remarks>
[Binding]
[Scope(Feature = "Companion pairing lookup")]
public sealed class CompanionPairingLookupStepDefinitions
{
    #region Fields
    /// <summary>Stores the time the companion asked.</summary>
    private static readonly DateTimeOffset RequestedAt = new(2026, 10, 4, 14, 0, 0, TimeSpan.Zero);

    /// <summary>Stores the pairing repository double.</summary>
    private readonly Mock<ICompanionPairingRepository> _pairings = new();

    /// <summary>Stores the clock double.</summary>
    private readonly Mock<TimeProvider> _clock = new();

    /// <summary>Stores the pairing under test.</summary>
    private CompanionPairing _pairing = null!;

    /// <summary>Stores the current time.</summary>
    private DateTimeOffset _now;

    /// <summary>Stores the result of the lookup.</summary>
    private Result<CompanionPairingResponse>? _lookup;
    #endregion Fields

    #region Properties
    /// <summary>Gets the captured lookup, failing the scenario if none ran.</summary>
    private Result<CompanionPairingResponse> Lookup => _lookup ?? throw new InvalidOperationException("No lookup ran in this scenario.");
    #endregion Properties

    #region Given Steps
    /// <summary>Starts a pairing some minutes ago.</summary>
    /// <param name="code">The code it shows.</param>
    /// <param name="minutes">How many minutes ago.</param>
    [Given("a companion asked to be paired with the code {string} {int} minutes ago")]
    public void GivenACompanionAskedToBePairedWithTheCodeMinutesAgo(string code, int minutes)
    {
        _now = RequestedAt.AddMinutes(minutes);
        _pairing = CompanionPairing.Start(CredentialHash.Of("device-code"), PairingCode.Create(code), "BRYN-DESKTOP", RequestedAt);
        _clock.Setup(clock => clock.GetUtcNow()).Returns(() => _now);
        _pairings.Setup(pairings => pairings.FindLatestUncollectedByCodeAsync(It.IsAny<PairingCode>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PairingCode asked, CancellationToken _) => asked == _pairing.Code && _pairing.State != CompanionPairingState.Completed ? _pairing : null);
    }

    /// <summary>Confirms the pairing as an arrangement step.</summary>
    [Given("the pairing was confirmed")]
    public void GivenThePairingWasConfirmed() => _pairing.Confirm(new UserId(Guid.NewGuid()), _now).IsSuccess.ShouldBeTrue();

    /// <summary>Moves the clock forward.</summary>
    /// <param name="minutes">The minutes that passed.</param>
    [Given("{int} minutes passed")]
    public void GivenMinutesPassed(int minutes) => _now = _now.AddMinutes(minutes);
    #endregion Given Steps

    #region When Steps
    /// <summary>Sends the lookup query.</summary>
    /// <param name="code">The code as typed.</param>
    /// <returns>A task that completes when the query has been handled.</returns>
    [When("the player looks up the code {string}")]
    public async Task WhenThePlayerLooksUpTheCode(string code) =>
        _lookup = await new GetCompanionPairingQueryHandler(_pairings.Object, _clock.Object)
            .Handle(new GetCompanionPairingQuery(Guid.NewGuid(), code), CancellationToken.None);
    #endregion When Steps

    #region Then Steps
    /// <summary>Asserts the described pairing.</summary>
    /// <param name="label">The expected computer label.</param>
    /// <param name="minutes">The expected minutes until expiry.</param>
    [Then("the lookup shows {string} expiring {int} minutes from now")]
    public void ThenTheLookupShowsExpiringMinutesFromNow(string label, int minutes)
    {
        Lookup.IsSuccess.ShouldBeTrue();
        Lookup.Value.ComputerLabel.ShouldBe(label);
        Lookup.Value.PairingCode.ShouldBe(_pairing.Code.ToString());
        Lookup.Value.RequestedAtUtc.ShouldBe(RequestedAt);
        Lookup.Value.ExpiresAtUtc.ShouldBe(_now.AddMinutes(minutes));
    }

    /// <summary>Asserts the failure code and its HTTP-relevant type.</summary>
    /// <param name="code">The expected error code.</param>
    /// <param name="type">The expected <see cref="ResultExceptionType"/> name.</param>
    [Then("the lookup fails with {string} as {word}")]
    public void ThenTheLookupFailsWithAs(string code, string type)
    {
        Lookup.IsFailure.ShouldBeTrue();
        Lookup.Error.Code.ShouldBe(code);
        Lookup.ResultExceptionType.ShouldBe(Enum.Parse<ResultExceptionType>(type));
    }
    #endregion Then Steps
}
