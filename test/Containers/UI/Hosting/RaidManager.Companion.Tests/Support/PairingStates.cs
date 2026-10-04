using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using RaidManager.Companion.Client.Configuration;
using RaidManager.Companion.Client.Features.Pairing;
using RaidManager.Companion.Client.Features.Shared;
using RaidManager.Companion.Client.Features.Tokens;

namespace RaidManager.Companion.Tests.Support;

/// <summary>Brings a real pairing view model into each state of the mockup, over test doubles.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: Lets the view tests show each state as the player meets it, without a network or a token file. Call from
/// a headless test, on the UI thread.
/// </remarks>
internal sealed class PairingStates : IDisposable
{
    #region Constants
    /// <summary>Defines the code the doubles hand out.</summary>
    public const string Code = "K7M-4QX";

    /// <summary>Defines the player the doubles pair with.</summary>
    public const string Player = "Bryn";
    #endregion Constants

    #region Fields
    /// <summary>Stores the time the doubles start at.</summary>
    private static readonly DateTimeOffset Start = new(2026, 10, 4, 14, 0, 0, TimeSpan.Zero);

    /// <summary>Stores the API double.</summary>
    private readonly Mock<ICompanionApi> _api = new();

    /// <summary>Stores the token store double.</summary>
    private readonly Mock<ITokenStore> _store = new();

    /// <summary>Stores the clock.</summary>
    private readonly DispatcherFlowClock _time = new(Start);
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="PairingStates"/> class.</summary>
    public PairingStates()
    {
        var browser = new Mock<IBrowserLauncher>();
        browser.Setup(launcher => launcher.OpenAsync(It.IsAny<Uri>())).ReturnsAsync(true);
        _api.Setup(api => api.StartPairingAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StartedPairing("device-code", Code, Start.AddMinutes(10), TimeSpan.FromSeconds(5)));
        _api.Setup(api => api.CollectTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TokenPoll(TokenPollStatus.Pending));
        ViewModel = new PairingViewModel(
            _api.Object,
            _store.Object,
            browser.Object,
            _time,
            Options.Create(new CompanionOptions { ApiBaseUrl = new Uri("https://api.raidmanager.test/"), WebsiteBaseUrl = new Uri("https://raidmanager.test/") }),
            NullLogger<PairingViewModel>.Instance);
    }
    #endregion Constructors

    #region Public Properties
    /// <summary>Gets the API double, to verify the calls a view made.</summary>
    public Mock<ICompanionApi> Api => _api;

    /// <summary>Gets the view model.</summary>
    public PairingViewModel ViewModel { get; }
    #endregion Public Properties

    #region Public Methods
    /// <summary>Brings the view model into a state.</summary>
    /// <param name="state">The state.</param>
    /// <returns>A task that completes once the view model shows the state.</returns>
    public async Task ShowAsync(PairingState state)
    {
        switch (state)
        {
            case PairingState.GettingCode:
                return;
            case PairingState.Paired or PairingState.Revoked:
                _store.Setup(store => store.LoadAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new PairedCompanion(Guid.NewGuid(), Player, "token"));
                _api.Setup(api => api.CheckTokenAsync("token", It.IsAny<CancellationToken>()))
                    .ReturnsAsync(state == PairingState.Paired ? TokenCheckStatus.Valid : TokenCheckStatus.Refused);
                break;
            case PairingState.CodeRequestFailed:
                _api.Setup(api => api.StartPairingAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new HttpRequestException("RaidManager can't be reached."));
                break;
            case PairingState.Expired:
                _api.Setup(api => api.CollectTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new TokenPoll(TokenPollStatus.Expired));
                break;
        }

        await ViewModel.StartCommand.ExecuteAsync();
        if (state == PairingState.Expired)
        {
            _time.AdvanceSeconds(5, () => ViewModel.State is PairingState.Expired or PairingState.Revoked or PairingState.CodeRequestFailed);
        }
    }

    /// <summary>Stops the view model's flow.</summary>
    public void Dispose() => ViewModel.Dispose();
    #endregion Public Methods
}
