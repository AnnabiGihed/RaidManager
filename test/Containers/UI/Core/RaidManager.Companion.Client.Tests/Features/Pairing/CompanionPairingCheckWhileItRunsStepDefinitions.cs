using RaidManager.Companion.Client.Features.Pairing;
using RaidManager.Companion.Client.Tests.Support;
using Reqnroll;
using Shouldly;

namespace RaidManager.Companion.Client.Tests.Features.Pairing;

/// <summary>Defines business-readable steps for checking the pairing while the companion runs.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: Verifies the fix of #524: a paired companion checks its token every five minutes and when the player opens
/// it, shows state 8 when RaidManager refuses the token, and keeps the pairing when the check can't be made (ADR-0030).
/// Each second the clock advances, the steps wait until the flow has reached its next wait or ended.
/// </remarks>
[Binding]
[Scope(Feature = "Companion pairing check while it runs")]
public sealed class CompanionPairingCheckWhileItRunsStepDefinitions : IDisposable
{
    #region Fields
    /// <summary>Stores the API double.</summary>
    private readonly FakeCompanionApi _api = new();

    /// <summary>Stores the token store double.</summary>
    private readonly FakeTokenStore _store = new();

    /// <summary>Stores the clock.</summary>
    private readonly FlowClock _time = new(PairingViewModels.Start);

    /// <summary>Stores the view model under test.</summary>
    private readonly PairingViewModel _viewModel;

    /// <summary>Stores the number of checks made before the action under test.</summary>
    private int _checksBefore;
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="CompanionPairingCheckWhileItRunsStepDefinitions"/> class.</summary>
    public CompanionPairingCheckWhileItRunsStepDefinitions()
    {
        _viewModel = PairingViewModels.Create(_api, _store, new FakeBrowserLauncher(), _time);
    }
    #endregion Constructors

    #region Public Methods
    /// <summary>Stops the view model's flow.</summary>
    public void Dispose() => _viewModel.Dispose();
    #endregion Public Methods

    #region Given Steps
    /// <summary>Stores a pairing.</summary>
    /// <param name="player">The player's name.</param>
    [Given("a pairing for {string} is stored")]
    public void GivenAPairingForIsStored(string player) =>
        _store.Stored = new PairedCompanion(Guid.NewGuid(), player, "stored-token");

    /// <summary>Scripts the answers to the next two token checks.</summary>
    /// <param name="first">The first answer.</param>
    /// <param name="second">The second answer.</param>
    [Given("RaidManager answers the token checks with {string} then {string}")]
    public void GivenRaidManagerAnswersTheTokenChecksWithThen(string first, string second) =>
        _api.AnswerChecks(Enum.Parse<TokenCheckStatus>(first), Enum.Parse<TokenCheckStatus>(second));

    /// <summary>Makes RaidManager start a pairing with a code and a polling interval.</summary>
    /// <param name="code">The code.</param>
    /// <param name="minutes">The minutes it stays valid.</param>
    /// <param name="seconds">The polling interval it asks for.</param>
    [Given("RaidManager answers with the code {string} valid for {int} minutes and a polling interval of {int} seconds")]
    public void GivenRaidManagerAnswersWithTheCodeValidForMinutesAndAPollingIntervalOfSeconds(string code, int minutes, int seconds) =>
        _api.Started = PairingViewModels.Pairing(code, minutes, seconds);

    /// <summary>Makes the first poll collect the token of a player.</summary>
    /// <param name="player">The player's name.</param>
    [Given("{string} confirms the code before the first poll")]
    public void GivenConfirmsTheCodeBeforeTheFirstPoll(string player) =>
        _api.AnswerPoll(TokenPoll.Collected(new PairedCompanion(Guid.NewGuid(), player, "device-token")));

    /// <summary>Starts the companion over its stored pairing.</summary>
    /// <returns>A task that completes once the start is done.</returns>
    [Given("the companion has started")]
    public async Task GivenTheCompanionHasStarted()
    {
        await StartAsync();
        _viewModel.State.ShouldBe(PairingState.Paired);
    }

    /// <summary>Starts the companion without a stored pairing, so it shows the code.</summary>
    /// <returns>A task that completes once the code is shown.</returns>
    [Given("the companion shows the code")]
    public async Task GivenTheCompanionShowsTheCode()
    {
        await StartAsync();
        _viewModel.State.ShouldBe(PairingState.Waiting);
    }
    #endregion Given Steps

    #region When Steps
    /// <summary>Advances the clock one second at a time.</summary>
    /// <param name="seconds">The seconds.</param>
    [When("{int} seconds pass")]
    public void WhenSecondsPass(int seconds) => _time.AdvanceSeconds(
        seconds,
        () => _viewModel.State is PairingState.Expired or PairingState.Revoked or PairingState.CodeRequestFailed);

    /// <summary>Opens the companion, as the tray icon, its Open entry or a second start do.</summary>
    /// <returns>A task that completes once the check is done.</returns>
    [When("the player opens the companion")]
    public Task WhenThePlayerOpensTheCompanion()
    {
        _checksBefore = _api.CheckedTokens.Count;
        return Task.Run(_viewModel.CheckPairingCommand.ExecuteAsync);
    }
    #endregion When Steps

    #region Then Steps
    /// <summary>Asserts the window's state.</summary>
    /// <param name="state">The state.</param>
    [Then("the companion shows {string}")]
    public void ThenTheCompanionShows(string state) => _viewModel.State.ShouldBe(Enum.Parse<PairingState>(state));

    /// <summary>Asserts whether the stored pairing was kept or deleted.</summary>
    /// <param name="storage">"kept" or "deleted".</param>
    [Then("the stored pairing is {string}")]
    public void ThenTheStoredPairingIs(string storage) => _store.Deleted.ShouldBe(storage == "deleted");

    /// <summary>Asserts the number of checks the action made.</summary>
    /// <param name="checks">The number of checks.</param>
    [Then("RaidManager was asked to check the token {int} times")]
    public void ThenRaidManagerWasAskedToCheckTheTokenTimes(int checks) => (_api.CheckedTokens.Count - _checksBefore).ShouldBe(checks);
    #endregion Then Steps

    #region Private Helpers
    /// <summary>Starts the companion off the test framework's synchronization context.</summary>
    /// <returns>A task that completes once the start is done.</returns>
    private Task StartAsync() => Task.Run(_viewModel.StartCommand.ExecuteAsync);
    #endregion Private Helpers
}
