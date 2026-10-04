using Microsoft.Extensions.Time.Testing;
using RaidManager.Companion.Client.Features.Pairing;
using RaidManager.Companion.Client.Tests.Support;
using Reqnroll;
using Shouldly;

namespace RaidManager.Companion.Client.Tests.Features.Pairing;

/// <summary>Defines business-readable steps for pairing the companion on this computer.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: Verifies the companion's side of ADR-0030 through the pairing view model: the code and the website, the
/// polling interval and back-off, a confirmed or expired code, and the check of a stored pairing. The flow runs off
/// the test framework's synchronization context, so each second the clock advances runs it to its next wait.
/// </remarks>
[Binding]
[Scope(Feature = "Companion pairing on this computer")]
public sealed class CompanionPairingOnThisComputerStepDefinitions : IDisposable
{
    #region Constants
    /// <summary>Defines the code of the scenarios that don't name one.</summary>
    private const string DefaultCode = "K7M-4QX";
    #endregion Constants

    #region Fields
    /// <summary>Stores the API double.</summary>
    private readonly FakeCompanionApi _api = new();

    /// <summary>Stores the token store double.</summary>
    private readonly FakeTokenStore _store = new();

    /// <summary>Stores the browser double.</summary>
    private readonly FakeBrowserLauncher _browser = new();

    /// <summary>Stores the clock.</summary>
    private readonly FakeTimeProvider _time = new(PairingViewModels.Start);

    /// <summary>Stores the view model under test.</summary>
    private readonly PairingViewModel _viewModel;
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="CompanionPairingOnThisComputerStepDefinitions"/> class.</summary>
    public CompanionPairingOnThisComputerStepDefinitions()
    {
        _viewModel = PairingViewModels.Create(_api, _store, _browser, _time);
    }
    #endregion Constructors

    #region Public Methods
    /// <summary>Stops the view model's flow.</summary>
    public void Dispose() => _viewModel.Dispose();
    #endregion Public Methods

    #region Given Steps
    /// <summary>Leaves the token store empty.</summary>
    [Given("no pairing is stored")]
    public void GivenNoPairingIsStored() => _store.Stored = null;

    /// <summary>Makes RaidManager start a pairing with a code.</summary>
    /// <param name="code">The code.</param>
    /// <param name="minutes">The minutes it stays valid.</param>
    [Given("RaidManager answers with the code {string} valid for {int} minutes")]
    public void GivenRaidManagerAnswersWithTheCodeValidForMinutes(string code, int minutes) =>
        _api.Started = PairingViewModels.Pairing(code, minutes, 5);

    /// <summary>Makes RaidManager start a pairing with a code and a polling interval.</summary>
    /// <param name="code">The code.</param>
    /// <param name="minutes">The minutes it stays valid.</param>
    /// <param name="seconds">The polling interval it asks for.</param>
    [Given("RaidManager answers with the code {string} valid for {int} minutes and a polling interval of {int} seconds")]
    public void GivenRaidManagerAnswersWithTheCodeValidForMinutesAndAPollingIntervalOfSeconds(string code, int minutes, int seconds) =>
        _api.Started = PairingViewModels.Pairing(code, minutes, seconds);

    /// <summary>Makes the request for a code fail.</summary>
    [Given("RaidManager can't be reached")]
    public void GivenRaidManagerCantBeReached() => _api.Started = null;

    /// <summary>Scripts the answer to the first poll.</summary>
    /// <param name="answer">The answer, as a poll status.</param>
    [Given("RaidManager answers the first poll with {string}")]
    public void GivenRaidManagerAnswersTheFirstPollWith(string answer) =>
        _api.AnswerPoll(new TokenPoll(Enum.Parse<TokenPollStatus>(answer)));

    /// <summary>Makes the first poll collect the token of a player.</summary>
    /// <param name="player">The player's name.</param>
    [Given("{string} confirms the code before the first poll")]
    public void GivenConfirmsTheCodeBeforeTheFirstPoll(string player) =>
        _api.AnswerPoll(TokenPoll.Collected(new PairedCompanion(Guid.NewGuid(), player, "device-token")));

    /// <summary>Starts the companion without a stored pairing, so it shows the code.</summary>
    /// <returns>A task that completes once the code is shown.</returns>
    [Given("the companion shows the code")]
    public async Task GivenTheCompanionShowsTheCode()
    {
        await StartAsync();
        _viewModel.State.ShouldBe(PairingState.Waiting);
    }

    /// <summary>Stores a pairing.</summary>
    /// <param name="player">The player's name.</param>
    [Given("a pairing for {string} is stored")]
    public void GivenAPairingForIsStored(string player) =>
        _store.Stored = new PairedCompanion(Guid.NewGuid(), player, "stored-token");

    /// <summary>Scripts the answer to the token check.</summary>
    /// <param name="check">The answer, as a check status.</param>
    [Given("RaidManager answers the token check with {string}")]
    public void GivenRaidManagerAnswersTheTokenCheckWith(string check) => _api.Check = Enum.Parse<TokenCheckStatus>(check);
    #endregion Given Steps

    #region When Steps
    /// <summary>Starts the companion.</summary>
    /// <returns>A task that completes once the start is done.</returns>
    [When("the companion starts")]
    public Task WhenTheCompanionStarts() => StartAsync();

    /// <summary>Advances the clock one second at a time.</summary>
    /// <param name="seconds">The seconds.</param>
    [When("{int} seconds pass")]
    public void WhenSecondsPass(int seconds)
    {
        for (var second = 0; second < seconds; second++)
        {
            _time.Advance(TimeSpan.FromSeconds(1));
        }
    }
    #endregion When Steps

    #region Then Steps
    /// <summary>Asserts the window's state.</summary>
    /// <param name="state">The state.</param>
    [Then("the companion shows {string}")]
    public void ThenTheCompanionShows(string state) => _viewModel.State.ShouldBe(Enum.Parse<PairingState>(state));

    /// <summary>Asserts the shown code and countdown.</summary>
    /// <param name="code">The code.</param>
    /// <param name="expiry">The countdown.</param>
    [Then("the code {string} is shown with {string}")]
    public void ThenTheCodeIsShownWith(string code, string expiry)
    {
        _viewModel.PairingCode.ShouldBe(code);
        _viewModel.ExpiryText.ShouldBe(expiry);
    }

    /// <summary>Asserts the website opened with the code.</summary>
    /// <param name="code">The code.</param>
    [Then("the website opens with the code {string}")]
    public void ThenTheWebsiteOpensWithTheCode(string code) =>
        _browser.Opened.ShouldBe([new Uri(PairingViewModels.Website, $"companion/pair?code={code}")]);

    /// <summary>Asserts the number of polls.</summary>
    /// <param name="polls">The number of polls.</param>
    [Then("RaidManager was asked for the token {int} times")]
    public void ThenRaidManagerWasAskedForTheTokenTimes(int polls) => _api.PollCount.ShouldBe(polls);

    /// <summary>Asserts the stored pairing's player.</summary>
    /// <param name="player">The player's name.</param>
    [Then("the stored pairing belongs to {string}")]
    public void ThenTheStoredPairingBelongsTo(string player) => _store.Stored.ShouldNotBeNull().PlayerName.ShouldBe(player);

    /// <summary>Asserts the heading of the paired state.</summary>
    /// <param name="heading">The heading.</param>
    [Then("the heading reads {string}")]
    public void ThenTheHeadingReads(string heading) => _viewModel.PairedHeading.ShouldBe(heading);

    /// <summary>Asserts whether the stored pairing was kept or deleted.</summary>
    /// <param name="storage">"kept" or "deleted".</param>
    [Then("the stored pairing is {string}")]
    public void ThenTheStoredPairingIs(string storage) => _store.Deleted.ShouldBe(storage == "deleted");
    #endregion Then Steps

    #region Private Helpers
    /// <summary>Starts the companion off the test framework's synchronization context.</summary>
    /// <returns>A task that completes once the start is done.</returns>
    private Task StartAsync() => Task.Run(_viewModel.StartCommand.ExecuteAsync);
    #endregion Private Helpers
}
