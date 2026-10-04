using Moq;
using Pivot.Framework.Domain.Repositories;
using Pivot.Framework.Domain.Shared;
using Reqnroll;
using Shouldly;
using RaidManager.Application.Features.Companions.Abstractions;
using RaidManager.Application.Features.Companions.Commands.AuthenticateCompanion;
using RaidManager.Application.Features.Companions.Commands.CollectCompanionToken;
using RaidManager.Application.Features.Companions.Commands.ConfirmCompanionPairing;
using RaidManager.Application.Features.Companions.Commands.RevokeCompanion;
using RaidManager.Application.Features.Companions.Commands.StartCompanionPairing;
using RaidManager.Application.Features.Companions.Queries.GetCompanionPairing;
using RaidManager.Application.Features.Companions.Queries.GetCompanions;
using RaidManager.Domain.Features.Companions.Aggregates;
using RaidManager.Domain.Features.Companions.Enums;
using RaidManager.Domain.Features.Companions.Repositories;
using RaidManager.Domain.Features.Companions.ValueObjects;
using RaidManager.Domain.Features.Identity.Aggregates;
using RaidManager.Domain.Features.Identity.Repositories;
using RaidManager.Domain.Features.Identity.ValueObjects;
using RaidManager.Domain.Features.Shared.Identifiers;

namespace RaidManager.Application.Tests.Features.Companions.Commands;

/// <summary>Defines business-readable steps for the companion pairing, token check, list and revocation use cases.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: Verifies that each handler loads what it needs, applies the aggregate's decision, and commits only a decision
/// that must be kept, including the branches the API tests can't reach, such as a failed commit or a broken code generator.
/// </remarks>
[Binding]
[Scope(Feature = "Companion pairing use cases")]
public sealed class CompanionPairingUseCasesStepDefinitions
{
    #region Constants
    /// <summary>Defines the device code the companion under test holds.</summary>
    private const string DeviceCode = "device-code";

    /// <summary>Defines the device token the companion under test holds.</summary>
    private const string DeviceToken = "device-token";
    #endregion Constants

    #region Fields
    /// <summary>Stores the fixed current time.</summary>
    private static readonly DateTimeOffset Now = new(2026, 10, 4, 14, 0, 0, TimeSpan.Zero);

    /// <summary>Stores the registered players by name.</summary>
    private readonly Dictionary<string, User> _players = [];

    /// <summary>Stores the pairings the repository double holds.</summary>
    private readonly List<CompanionPairing> _pairings = [];

    /// <summary>Stores the companions the repository double holds.</summary>
    private readonly List<Companion> _companions = [];

    /// <summary>Stores the codes the generator double draws, in order.</summary>
    private readonly Queue<string> _codes = new();

    /// <summary>Stores the codes in use.</summary>
    private readonly HashSet<string> _codesInUse = [];

    /// <summary>Stores the pairing repository double.</summary>
    private readonly Mock<ICompanionPairingRepository> _pairingRepository = new();

    /// <summary>Stores the companion repository double.</summary>
    private readonly Mock<ICompanionRepository> _companionRepository = new();

    /// <summary>Stores the user repository double.</summary>
    private readonly Mock<IUserRepository> _users = new();

    /// <summary>Stores the generator double.</summary>
    private readonly Mock<ICompanionCredentialGenerator> _credentials = new();

    /// <summary>Stores the unit-of-work double that records commits.</summary>
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    /// <summary>Stores the clock double.</summary>
    private readonly Mock<TimeProvider> _clock = new();

    /// <summary>Stores the result the next commit returns.</summary>
    private Result _commitResult = Result.Success();

    /// <summary>Stores the result of the latest use case.</summary>
    private Result? _result;

    /// <summary>Stores the value of the latest successful use case.</summary>
    private object? _value;

    /// <summary>Stores the fault of the latest use case.</summary>
    private Exception? _fault;

    /// <summary>Stores the result of the latest validation.</summary>
    private FluentValidation.Results.ValidationResult? _validation;
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="CompanionPairingUseCasesStepDefinitions"/> class.</summary>
    public CompanionPairingUseCasesStepDefinitions()
    {
        _clock.Setup(clock => clock.GetUtcNow()).Returns(Now);
        _unitOfWork.Setup(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(() => _commitResult);
        _credentials.Setup(credentials => credentials.NewSecret()).Returns(DeviceToken);
        _credentials.Setup(credentials => credentials.NewPairingCode()).Returns(() => PairingCode.Create(_codes.Dequeue()));
        _users.Setup(users => users.FindByIdAsync(It.IsAny<UserId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserId id, CancellationToken _) => _players.Values.FirstOrDefault(user => user.Id == id));
        _pairingRepository.Setup(pairings => pairings.IsCodeInUseAsync(It.IsAny<PairingCode>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PairingCode code, DateTimeOffset _, CancellationToken _) => _codesInUse.Contains(code.Value));
        _pairingRepository.Setup(pairings => pairings.FindLatestUncollectedByCodeAsync(It.IsAny<PairingCode>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PairingCode code, CancellationToken _) => _pairings.LastOrDefault(pairing => pairing.Code == code && pairing.State != CompanionPairingState.Completed));
        _pairingRepository.Setup(pairings => pairings.FindByDeviceCodeHashAsync(It.IsAny<CredentialHash>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CredentialHash hash, CancellationToken _) => _pairings.FirstOrDefault(pairing => pairing.DeviceCodeHash == hash));
        _companionRepository.Setup(companions => companions.FindByTokenHashAsync(It.IsAny<CredentialHash>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CredentialHash hash, CancellationToken _) => _companions.FirstOrDefault(companion => companion.TokenHash == hash));
        _companionRepository.Setup(companions => companions.FindByIdAsync(It.IsAny<CompanionId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CompanionId id, CancellationToken _) => _companions.FirstOrDefault(companion => companion.Id == id));
        _companionRepository.Setup(companions => companions.ListByUserAsync(It.IsAny<UserId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserId id, CancellationToken _) => _companions.Where(companion => companion.UserId == id).ToList());
    }
    #endregion Constructors

    #region Properties
    /// <summary>Gets the captured result, failing the scenario if no use case ran.</summary>
    private Result Outcome => _result ?? throw new InvalidOperationException("No use case ran in this scenario.");
    #endregion Properties

    #region Given Steps
    /// <summary>Registers a player.</summary>
    /// <param name="player">The player name.</param>
    [Given("{string} is a registered player")]
    public void GivenIsARegisteredPlayer(string player) =>
        _players[player] = User.Register(DiscordUserId.Create("80351110224678912"), player, avatarUrl: null);

    /// <summary>Queues the codes the generator draws.</summary>
    /// <param name="first">The first code.</param>
    /// <param name="second">The second code.</param>
    [Given("the next codes drawn are {string} then {string}")]
    public void GivenTheNextCodesDrawnAreThen(string first, string second)
    {
        _codes.Enqueue(first);
        _codes.Enqueue(second);
        for (var attempt = 2; attempt < CompanionPairingDefaults.CodeAttempts; attempt++)
        {
            _codes.Enqueue(second);
        }
    }

    /// <summary>Marks a code as shown by a live pairing.</summary>
    /// <param name="code">The code.</param>
    [Given("the code {string} is in use")]
    public void GivenTheCodeIsInUse(string code) => _codesInUse.Add(code);

    /// <summary>Makes the next commit fail.</summary>
    [Given("the commit will fail")]
    public void GivenTheCommitWillFail() => _commitResult = Result.Failure(new Error("Commit.Failed", "The database rejected the change."));

    /// <summary>Starts a pairing that waits for confirmation.</summary>
    /// <param name="code">The code it shows.</param>
    [Given("a companion waits with the code {string}")]
    public void GivenACompanionWaitsWithTheCode(string code) =>
        _pairings.Add(CompanionPairing.Start(CredentialHash.Of(DeviceCode), PairingCode.Create(code), "BRYN-DESKTOP", Now.AddMinutes(-1)));

    /// <summary>Confirms a pairing directly as an arrangement step.</summary>
    /// <param name="player">The player name.</param>
    /// <param name="code">The code.</param>
    [Given("{string} confirmed the code {string}")]
    public void GivenConfirmedTheCode(string player, string code) =>
        _pairings.Single(pairing => pairing.Code == PairingCode.Create(code)).Confirm(_players[player].Id, Now).IsSuccess.ShouldBeTrue();

    /// <summary>Removes a player after an arrangement.</summary>
    /// <param name="player">The player name.</param>
    [Given("{string} no longer exists")]
    public void GivenNoLongerExists(string player) => _players.Remove(player);

    /// <summary>Pairs a companion for a player through a confirmed pairing.</summary>
    /// <param name="player">The player name.</param>
    /// <param name="hours">How many hours ago.</param>
    [Given("a companion paired for {string} {int} hours ago")]
    public void GivenACompanionPairedForHoursAgo(string player, int hours)
    {
        var pairedAt = Now.AddHours(-hours);
        var pairing = CompanionPairing.Start(CredentialHash.Of(DeviceCode), PairingCode.Create("K7M4QX"), "BRYN-DESKTOP", pairedAt);
        pairing.Confirm(_players[player].Id, pairedAt).IsSuccess.ShouldBeTrue();
        _companions.Add(pairing.Complete(CredentialHash.Of(DeviceToken), pairedAt).Value);
    }

    /// <summary>Revokes the companion directly as an arrangement step.</summary>
    /// <param name="player">The player name.</param>
    [Given("{string} revoked the companion")]
    public void GivenRevokedTheCompanion(string player) => _companions.Single().Revoke(_players[player].Id, Now.AddHours(-1)).IsSuccess.ShouldBeTrue();
    #endregion Given Steps

    #region When Steps
    /// <summary>Sends the start command.</summary>
    /// <param name="label">The computer label.</param>
    /// <returns>A task that completes when the command has been handled.</returns>
    [When("a companion starts pairing as {string}")]
    public async Task WhenACompanionStartsPairingAs(string label) =>
        Capture(await StartHandler().Handle(new StartCompanionPairingCommand(label), CancellationToken.None));

    /// <summary>Sends the start command and captures its fault.</summary>
    /// <param name="label">The computer label.</param>
    /// <returns>A task that completes when the command has been handled.</returns>
    [When("a companion starts pairing as {string} expecting a fault")]
    public async Task WhenACompanionStartsPairingAsExpectingAFault(string label) =>
        _fault = await Should.ThrowAsync<InvalidOperationException>(() => StartHandler().Handle(new StartCompanionPairingCommand(label), CancellationToken.None));

    /// <summary>Sends the confirm command.</summary>
    /// <param name="player">The player name.</param>
    /// <param name="code">The code as typed.</param>
    /// <returns>A task that completes when the command has been handled.</returns>
    [When("{string} confirms the code {string}")]
    public async Task WhenConfirmsTheCode(string player, string code) =>
        _result = await new ConfirmCompanionPairingCommandHandler(_pairingRepository.Object, _users.Object, _unitOfWork.Object, _clock.Object)
            .Handle(new ConfirmCompanionPairingCommand(PlayerGuid(player), code), CancellationToken.None);

    /// <summary>Sends the collect command with an unknown device code.</summary>
    /// <returns>A task that completes when the command has been handled.</returns>
    [When("the companion collects its token with an unknown device code")]
    public async Task WhenTheCompanionCollectsItsTokenWithAnUnknownDeviceCode() => await CollectAsync("unknown-device-code");

    /// <summary>Sends the collect command with the companion's device code.</summary>
    /// <returns>A task that completes when the command has been handled.</returns>
    [When("the companion collects its token")]
    public async Task WhenTheCompanionCollectsItsToken() => await CollectAsync(DeviceCode);

    /// <summary>Sends the token check with a given token.</summary>
    /// <param name="token">The token.</param>
    /// <returns>A task that completes when the command has been handled.</returns>
    [When("a companion calls with the token {string}")]
    public async Task WhenACompanionCallsWithTheToken(string token) => await AuthenticateAsync(token);

    /// <summary>Sends the token check with the companion's token.</summary>
    /// <returns>A task that completes when the command has been handled.</returns>
    [When("the companion calls with its token")]
    public async Task WhenTheCompanionCallsWithItsToken() => await AuthenticateAsync(DeviceToken);

    /// <summary>Sends the list query.</summary>
    /// <param name="player">The player name.</param>
    /// <returns>A task that completes when the query has been handled.</returns>
    [When("{string} lists their companions")]
    public async Task WhenListsTheirCompanions(string player) =>
        Capture(await new GetCompanionsQueryHandler(_companionRepository.Object, _clock.Object)
            .Handle(new GetCompanionsQuery(PlayerGuid(player)), CancellationToken.None));

    /// <summary>Sends the revoke command for an unknown companion.</summary>
    /// <param name="player">The player name.</param>
    /// <returns>A task that completes when the command has been handled.</returns>
    [When("{string} revokes an unknown companion")]
    public async Task WhenRevokesAnUnknownCompanion(string player) => await RevokeAsync(player, Guid.NewGuid());

    /// <summary>Sends the revoke command for the companion under test.</summary>
    /// <param name="player">The player name.</param>
    /// <returns>A task that completes when the command has been handled.</returns>
    [When("{string} revokes the companion")]
    public async Task WhenRevokesTheCompanion(string player) => await RevokeAsync(player, _companions.Single().Id.Value);

    /// <summary>Validates a malformed request.</summary>
    /// <param name="request">The request description.</param>
    [When("the {} is validated")]
    public void WhenTheIsValidated(string request) => _validation = request switch
    {
        "confirmation without a player" => new ConfirmCompanionPairingCommandValidator().Validate(new ConfirmCompanionPairingCommand(Guid.Empty, "K7M4QX")),
        "confirmation with a bad code" => new ConfirmCompanionPairingCommandValidator().Validate(new ConfirmCompanionPairingCommand(Guid.NewGuid(), "K0M4QX")),
        "lookup with a bad code" => new GetCompanionPairingQueryValidator().Validate(new GetCompanionPairingQuery(Guid.NewGuid(), "K7M")),
        "collection without a code" => new CollectCompanionTokenCommandValidator().Validate(new CollectCompanionTokenCommand(string.Empty)),
        "start with a long label" => new StartCompanionPairingCommandValidator().Validate(new StartCompanionPairingCommand(new string('x', CompanionPairingDefaults.MaximumComputerLabelLength + 1))),
        "list without a player" => new GetCompanionsQueryValidator().Validate(new GetCompanionsQuery(Guid.Empty)),
        "revocation without a player" => new RevokeCompanionCommandValidator().Validate(new RevokeCompanionCommand(Guid.Empty, Guid.NewGuid())),
        "revocation without a companion" => new RevokeCompanionCommandValidator().Validate(new RevokeCompanionCommand(Guid.NewGuid(), Guid.Empty)),
        _ => throw new ArgumentOutOfRangeException(nameof(request), request, "Unknown request."),
    };
    #endregion When Steps

    #region Then Steps
    /// <summary>Asserts that the use case succeeded.</summary>
    [Then("the use case succeeds")]
    public void ThenTheUseCaseSucceeds() => Outcome.IsSuccess.ShouldBeTrue();

    /// <summary>Asserts the failure code and its HTTP-relevant type.</summary>
    /// <param name="code">The expected error code.</param>
    /// <param name="type">The expected <see cref="ResultExceptionType"/> name.</param>
    [Then("the use case fails with {string} as {word}")]
    public void ThenTheUseCaseFailsWithAs(string code, string type)
    {
        Outcome.IsFailure.ShouldBeTrue();
        Outcome.Error.Code.ShouldBe(code);
        Outcome.ResultExceptionType.ShouldBe(Enum.Parse<ResultExceptionType>(type));
    }

    /// <summary>Asserts that the start failed with a fault, as the generator is broken.</summary>
    [Then("the start fails with a fault")]
    public void ThenTheStartFailsWithAFault() => _fault.ShouldNotBeNull();

    /// <summary>Asserts the code shown by the started pairing.</summary>
    /// <param name="code">The expected code.</param>
    [Then("the pairing shows {string}")]
    public void ThenThePairingShows(string code)
    {
        var started = _value.ShouldBeOfType<StartedCompanionPairingResponse>();
        started.PairingCode.ShouldBe(code);
        started.DeviceCode.ShouldBe(DeviceToken);
        started.PollingIntervalSeconds.ShouldBe(CompanionPairingDefaults.PollingIntervalSeconds);
        started.ExpiresAtUtc.ShouldBe(Now + CompanionPairing.Lifetime);
    }

    /// <summary>Asserts that a pairing was saved.</summary>
    [Then("the pairing is committed")]
    public void ThenThePairingIsCommitted() => _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

    /// <summary>Asserts that a companion was saved.</summary>
    [Then("the companion is committed")]
    public void ThenTheCompanionIsCommitted() => _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

    /// <summary>Asserts that nothing was saved.</summary>
    [Then("nothing is committed")]
    public void ThenNothingIsCommitted() => _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);

    /// <summary>Asserts the token answer.</summary>
    /// <param name="player">The player name.</param>
    [Then("the companion receives a token for {string}")]
    public void ThenTheCompanionReceivesATokenFor(string player)
    {
        var token = _value.ShouldBeOfType<CompanionTokenResponse>();
        token.DeviceToken.ShouldBe(DeviceToken);
        token.PlayerName.ShouldBe(player);
        _companionRepository.Verify(companions => companions.AddAsync(It.Is<Companion>(companion => companion.Id.Value == token.CompanionId), It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>Asserts the admitted companion.</summary>
    /// <param name="player">The player name.</param>
    [Then("the companion is admitted for {string}")]
    public void ThenTheCompanionIsAdmittedFor(string player)
    {
        var companion = _value.ShouldBeOfType<AuthenticatedCompanionResponse>();
        companion.UserId.ShouldBe(_players[player].Id.Value);
        companion.Label.ShouldBe("BRYN-DESKTOP");
    }

    /// <summary>Asserts the listed status.</summary>
    /// <param name="status">The expected status.</param>
    [Then("the list shows one companion with the status {word}")]
    public void ThenTheListShowsOneCompanionWithTheStatus(string status)
    {
        var companion = _value.ShouldBeAssignableTo<IReadOnlyList<CompanionResponse>>().ShouldNotBeNull().ShouldHaveSingleItem();
        companion.Status.ShouldBe(Enum.Parse<CompanionStatus>(status));
        companion.RevokedAtUtc.ShouldBe(Now.AddHours(-1));
    }

    /// <summary>Asserts the failed property.</summary>
    /// <param name="property">The expected property name.</param>
    [Then("the validation fails on {string}")]
    public void ThenTheValidationFailsOn(string property)
    {
        var validation = _validation.ShouldNotBeNull();
        validation.IsValid.ShouldBeFalse();
        validation.Errors.ShouldContain(error => error.PropertyName == property);
    }
    #endregion Then Steps

    #region Private Helpers
    /// <summary>Builds the start handler.</summary>
    /// <returns>The handler.</returns>
    private StartCompanionPairingCommandHandler StartHandler() =>
        new(_pairingRepository.Object, _credentials.Object, _unitOfWork.Object, _clock.Object);

    /// <summary>Sends the collect command.</summary>
    /// <param name="deviceCode">The device code.</param>
    /// <returns>A task that completes when the command has been handled.</returns>
    private async Task CollectAsync(string deviceCode) =>
        Capture(await new CollectCompanionTokenCommandHandler(
                _pairingRepository.Object, _companionRepository.Object, _users.Object, _credentials.Object, _unitOfWork.Object, _clock.Object)
            .Handle(new CollectCompanionTokenCommand(deviceCode), CancellationToken.None));

    /// <summary>Sends the token check.</summary>
    /// <param name="token">The token.</param>
    /// <returns>A task that completes when the command has been handled.</returns>
    private async Task AuthenticateAsync(string token) =>
        Capture(await new AuthenticateCompanionCommandHandler(_companionRepository.Object, _unitOfWork.Object, _clock.Object)
            .Handle(new AuthenticateCompanionCommand(token), CancellationToken.None));

    /// <summary>Sends the revoke command.</summary>
    /// <param name="player">The player name.</param>
    /// <param name="companionId">The companion.</param>
    /// <returns>A task that completes when the command has been handled.</returns>
    private async Task RevokeAsync(string player, Guid companionId) =>
        _result = await new RevokeCompanionCommandHandler(_companionRepository.Object, _unitOfWork.Object, _clock.Object)
            .Handle(new RevokeCompanionCommand(PlayerGuid(player), companionId), CancellationToken.None);

    /// <summary>Stores a typed result and its value.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="result">The result.</param>
    private void Capture<T>(Result<T> result)
    {
        _result = result;
        _value = result.IsSuccess ? result.Value : null;
    }

    /// <summary>Gets a registered player's identifier, or a fresh one for an unknown player.</summary>
    /// <param name="player">The player name.</param>
    /// <returns>The identifier.</returns>
    private Guid PlayerGuid(string player) => _players.TryGetValue(player, out var user) ? user.Id.Value : Guid.NewGuid();
    #endregion Private Helpers
}
