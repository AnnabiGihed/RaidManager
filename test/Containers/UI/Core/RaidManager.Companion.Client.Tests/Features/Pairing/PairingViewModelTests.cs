using System.ComponentModel;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using RaidManager.Companion.Client.Configuration;
using RaidManager.Companion.Client.Features.Pairing;
using RaidManager.Companion.Client.Tests.Support;
using Shouldly;
using Xunit;

namespace RaidManager.Companion.Client.Tests.Features.Pairing;

/// <summary>Verifies the pairing view model's wiring that the pairing scenarios don't describe.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: Technical tests of <see cref="PairingViewModel"/>: the properties the view binds to, a new code that
/// replaces the shown one, a browser that refuses or fails, a store that fails while pairing, and disposal. Flows run
/// off the test framework's synchronization context, as in the scenarios.
/// </remarks>
public sealed class PairingViewModelTests : IDisposable
{
    #region Fields
    /// <summary>Stores the API double.</summary>
    private readonly FakeCompanionApi _api = new() { Started = PairingViewModels.Pairing("K7M-4QX", 10, 5) };

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
    /// <summary>Initializes a new instance of the <see cref="PairingViewModelTests"/> class.</summary>
    public PairingViewModelTests()
    {
        _viewModel = PairingViewModels.Create(_api, _store, _browser, _time);
    }
    #endregion Constructors

    #region Public Methods
    /// <summary>Stops the view model's flow.</summary>
    public void Dispose() => _viewModel.Dispose();
    #endregion Public Methods

    #region Tests
    /// <summary>Before the start, the window shows the request for a code, with the computer's name ready.</summary>
    [Fact]
    public void NewViewModelShowsGettingACode()
    {
        _viewModel.State.ShouldBe(PairingState.GettingCode);
        _viewModel.IsGettingCode.ShouldBeTrue();
        _viewModel.IsShowingCode.ShouldBeTrue();
        _viewModel.ComputerLabel.ShouldBe(Environment.MachineName);
        _viewModel.RevokedMessage.ShouldBe($"{Environment.MachineName} isn't paired with your account anymore.");
    }

    /// <summary>Each state flag follows the state, and the view hears about every change.</summary>
    /// <returns>A task that completes when the test is done.</returns>
    [Fact]
    public async Task StartCommandWhenTheCodeIsShownRaisesTheStateFlags()
    {
        var changed = new List<string?>();
        _viewModel.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        await Task.Run(_viewModel.StartCommand.ExecuteAsync);

        _viewModel.IsWaiting.ShouldBeTrue();
        _viewModel.IsShowingCode.ShouldBeTrue();
        (_viewModel.IsGettingCode || _viewModel.IsPaired || _viewModel.IsExpired || _viewModel.IsRevoked || _viewModel.IsCodeRequestFailed)
            .ShouldBeFalse();
        changed.ShouldContain(nameof(PairingViewModel.IsWaiting));
        changed.ShouldContain(nameof(PairingViewModel.IsCodeRequestFailed));
        changed.ShouldContain(nameof(PairingViewModel.PairingCode));
        _api.StartedLabels.ShouldBe([Environment.MachineName]);
    }

    /// <summary>A new code stops waiting for the old one and shows the new one.</summary>
    /// <returns>A task that completes when the test is done.</returns>
    [Fact]
    public async Task RequestCodeCommandWhileWaitingReplacesTheCode()
    {
        await Task.Run(_viewModel.StartCommand.ExecuteAsync);
        _api.Started = PairingViewModels.Pairing("ABC-234", 10, 5);

        await Task.Run(_viewModel.RequestCodeCommand.ExecuteAsync);
        Advance(5);

        _viewModel.PairingCode.ShouldBe("ABC-234");
        _api.PollCount.ShouldBe(1);
        _browser.Opened.Count.ShouldBe(2);
    }

    /// <summary>The countdown follows the clock.</summary>
    /// <returns>A task that completes when the test is done.</returns>
    [Fact]
    public async Task ExpiryTextFollowsTheClock()
    {
        await Task.Run(_viewModel.StartCommand.ExecuteAsync);

        Advance(18);

        _viewModel.ExpiryText.ShouldBe("Expires in 9:42");
    }

    /// <summary>A code that arrives already expired counts down from zero, then shows as expired.</summary>
    /// <returns>A task that completes when the test is done.</returns>
    [Fact]
    public async Task CodeThatArrivesExpiredShowsZeroThenExpires()
    {
        _api.Started = new StartedPairing("device-code", "K7M-4QX", PairingViewModels.Start.AddMinutes(-1), TimeSpan.FromSeconds(5));

        await Task.Run(_viewModel.StartCommand.ExecuteAsync);
        _viewModel.ExpiryText.ShouldBe("Expires in 0:00");
        Advance(1);

        _viewModel.State.ShouldBe(PairingState.Expired);
    }

    /// <summary>A browser that refuses or fails leaves the code shown; the player can open the website again.</summary>
    /// <param name="fails">Whether the browser throws instead of refusing.</param>
    /// <returns>A task that completes when the test is done.</returns>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OpenWebsiteCommandWhenTheBrowserCantOpenKeepsWaiting(bool fails)
    {
        var viewModel = PairingViewModels.Create(_api, _store, fails ? new ThrowingBrowserLauncher() : new FakeBrowserLauncher { Accepts = false }, _time);
        using (viewModel)
        {
            await Task.Run(viewModel.StartCommand.ExecuteAsync);

            viewModel.State.ShouldBe(PairingState.Waiting);
            viewModel.OpenWebsiteCommand.CanExecute(null).ShouldBeTrue();
        }
    }

    /// <summary>Without a code there is nothing to open.</summary>
    /// <returns>A task that completes when the test is done.</returns>
    [Fact]
    public async Task OpenWebsiteCommandWithoutACodeOpensNothing()
    {
        await _viewModel.OpenWebsiteCommand.ExecuteAsync();

        _browser.Opened.ShouldBeEmpty();
    }

    /// <summary>A store that can't be read shows the state that offers to try again.</summary>
    /// <returns>A task that completes when the test is done.</returns>
    [Fact]
    public async Task StartCommandWhenTheStoreFailsOffersToTryAgain()
    {
        var viewModel = PairingViewModels.Create(_api, new FailingTokenStore(), _browser, _time);
        using (viewModel)
        {
            await Task.Run(viewModel.StartCommand.ExecuteAsync);

            viewModel.State.ShouldBe(PairingState.CodeRequestFailed);
        }
    }

    /// <summary>A token that can't be stored while pairing shows the state that offers to try again.</summary>
    /// <returns>A task that completes when the test is done.</returns>
    [Fact]
    public async Task PollingWhenTheTokenCantBeStoredOffersToTryAgain()
    {
        var store = new FailingTokenStore { FailsOnLoad = false };
        var viewModel = PairingViewModels.Create(_api, store, _browser, _time);
        using (viewModel)
        {
            _api.AnswerPoll(TokenPoll.Collected(new PairedCompanion(Guid.NewGuid(), "Bryn", "token")));
            await Task.Run(viewModel.StartCommand.ExecuteAsync);

            Advance(5);

            viewModel.State.ShouldBe(PairingState.CodeRequestFailed);
        }
    }

    /// <summary>After disposal the flow stops: time passing polls nothing.</summary>
    /// <returns>A task that completes when the test is done.</returns>
    [Fact]
    public async Task DisposeStopsPolling()
    {
        var viewModel = PairingViewModels.Create(_api, _store, _browser, _time);
        await Task.Run(viewModel.StartCommand.ExecuteAsync);

        viewModel.Dispose();
        Advance(10);

        _api.PollCount.ShouldBe(0);
        viewModel.State.ShouldBe(PairingState.Waiting);
    }

    /// <summary>The view model needs the website's address.</summary>
    [Fact]
    public void ConstructorWithoutAWebsiteThrows() => Should.Throw<ArgumentException>(() => new PairingViewModel(
        _api,
        _store,
        _browser,
        _time,
        Options.Create(new CompanionOptions()),
        NullLogger<PairingViewModel>.Instance));

    /// <summary>Records describe themselves without their secrets.</summary>
    [Fact]
    public void RecordsToStringLeaveOutTheSecrets()
    {
        var companionId = Guid.NewGuid();

        PairingViewModels.Pairing("K7M-4QX", 10, 5).ToString().ShouldBe("K7M-4QX");
        new PairedCompanion(companionId, "Bryn", "secret").ToString().ShouldBe($"{companionId} (Bryn)");
        new CollectTokenRequest("secret").ToString().ShouldBe(nameof(CollectTokenRequest));
        new CompanionTokenResponse(companionId, "secret", "Bryn").ToString().ShouldBe(companionId.ToString());
    }
    #endregion Tests

    #region Private Helpers
    /// <summary>Advances the clock one second at a time.</summary>
    /// <param name="seconds">The seconds.</param>
    private void Advance(int seconds)
    {
        for (var second = 0; second < seconds; second++)
        {
            _time.Advance(TimeSpan.FromSeconds(1));
        }
    }
    #endregion Private Helpers

    #region Nested Types
    /// <summary>A browser launcher that fails.</summary>
    private sealed class ThrowingBrowserLauncher : Client.Features.Shared.IBrowserLauncher
    {
        /// <inheritdoc />
        public Task<bool> OpenAsync(Uri address) => Task.FromException<bool>(new InvalidOperationException("No browser."));
    }

    /// <summary>A token store whose disk fails.</summary>
    private sealed class FailingTokenStore : Client.Features.Tokens.ITokenStore
    {
        /// <summary>Gets or sets a value indicating whether loading fails too.</summary>
        public bool FailsOnLoad { get; set; } = true;

        /// <inheritdoc />
        public Task<PairedCompanion?> LoadAsync(CancellationToken cancellationToken) => FailsOnLoad
            ? Task.FromException<PairedCompanion?>(new IOException("Disk unavailable."))
            : Task.FromResult<PairedCompanion?>(null);

        /// <inheritdoc />
        public Task SaveAsync(PairedCompanion companion, CancellationToken cancellationToken) =>
            Task.FromException(new UnauthorizedAccessException("Read-only profile."));

        /// <inheritdoc />
        public void Delete()
        {
        }
    }
    #endregion Nested Types
}
