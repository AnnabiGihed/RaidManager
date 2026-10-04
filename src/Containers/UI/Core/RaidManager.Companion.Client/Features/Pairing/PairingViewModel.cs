using System.Globalization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RaidManager.Companion.Client.Configuration;
using RaidManager.Companion.Client.Features.Shared;
using RaidManager.Companion.Client.Features.Tokens;

namespace RaidManager.Companion.Client.Features.Pairing;

/// <summary>Pairs this computer with the player's account and shows where the pairing stands.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: The companion side of ADR-0030 and the states of the companion pairing mockup (boards 5 to 8, 19 and 20):
/// request a code, open the website with it, poll every five seconds or more until the player confirms it or it
/// expires, store the token, and check a stored token at start, forgetting it only when RaidManager refuses it.
/// </remarks>
public sealed partial class PairingViewModel : ViewModelBase, IDisposable
{
    #region Fields
    /// <summary>Stores the shortest interval between two polls, whatever the API asks (ADR-0030).</summary>
    private static readonly TimeSpan MinimumPollingInterval = TimeSpan.FromSeconds(5);

    /// <summary>Stores how much longer to wait after a request to slow down or a failed poll (RFC 8628).</summary>
    private static readonly TimeSpan BackOffStep = TimeSpan.FromSeconds(5);

    /// <summary>Stores how often the countdown is updated.</summary>
    private static readonly TimeSpan Tick = TimeSpan.FromSeconds(1);

    /// <summary>Stores the API client.</summary>
    private readonly ICompanionApi _api;

    /// <summary>Stores the token store.</summary>
    private readonly ITokenStore _store;

    /// <summary>Stores the browser launcher.</summary>
    private readonly IBrowserLauncher _browser;

    /// <summary>Stores the clock.</summary>
    private readonly TimeProvider _time;

    /// <summary>Stores the website's address.</summary>
    private readonly Uri _website;

    /// <summary>Stores the logger, which never receives a token or a device code.</summary>
    private readonly ILogger<PairingViewModel> _logger;

    /// <summary>Stores the source that stops the current pairing flow.</summary>
    private CancellationTokenSource _flow = new();

    /// <summary>Stores the current state.</summary>
    private PairingState _state = PairingState.GettingCode;

    /// <summary>Stores the code shown to the player.</summary>
    private string _pairingCode = string.Empty;

    /// <summary>Stores when the shown code expires.</summary>
    private DateTimeOffset _expiresAt;

    /// <summary>Stores the countdown text.</summary>
    private string _expiryText = string.Empty;

    /// <summary>Stores the name of the player this computer is paired with.</summary>
    private string _playerName = string.Empty;
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="PairingViewModel"/> class.</summary>
    /// <param name="api">The API client.</param>
    /// <param name="store">The token store.</param>
    /// <param name="browser">The browser launcher.</param>
    /// <param name="time">The clock.</param>
    /// <param name="options">The environment's addresses.</param>
    /// <param name="logger">The logger.</param>
    public PairingViewModel(
        ICompanionApi api,
        ITokenStore store,
        IBrowserLauncher browser,
        TimeProvider time,
        IOptions<CompanionOptions> options,
        ILogger<PairingViewModel> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        _api = api;
        _store = store;
        _browser = browser;
        _time = time;
        _website = options.Value.WebsiteBaseUrl ?? throw new ArgumentException("The website address is missing.", nameof(options));
        _logger = logger;
        ComputerLabel = Environment.MachineName;
        StartCommand = new AsyncRelayCommand(StartAsync, OnFlowFailed);
        RequestCodeCommand = new AsyncRelayCommand(RequestCodeAsync, OnFlowFailed);
        OpenWebsiteCommand = new AsyncRelayCommand(OpenWebsiteAsync, OnOpenWebsiteFailed);
    }
    #endregion Constructors

    #region Public Properties
    /// <summary>Gets the command that loads a stored pairing, or requests a code when there is none.</summary>
    public AsyncRelayCommand StartCommand { get; }

    /// <summary>Gets the command behind "Get a new code", "Pair again" and "Try again".</summary>
    public AsyncRelayCommand RequestCodeCommand { get; }

    /// <summary>Gets the command behind "Open the website".</summary>
    public AsyncRelayCommand OpenWebsiteCommand { get; }

    /// <summary>Gets this computer's name, as the website shows it.</summary>
    public string ComputerLabel { get; }

    /// <summary>Gets the current state.</summary>
    public PairingState State
    {
        get => _state;
        private set
        {
            if (SetProperty(ref _state, value))
            {
                OnPropertyChanged(nameof(IsGettingCode));
                OnPropertyChanged(nameof(IsWaiting));
                OnPropertyChanged(nameof(IsShowingCode));
                OnPropertyChanged(nameof(IsPaired));
                OnPropertyChanged(nameof(IsExpired));
                OnPropertyChanged(nameof(IsRevoked));
                OnPropertyChanged(nameof(IsCodeRequestFailed));
            }
        }
    }

    /// <summary>Gets a value indicating whether the companion is asking for a code (board 19).</summary>
    public bool IsGettingCode => State == PairingState.GettingCode;

    /// <summary>Gets a value indicating whether the code is shown and waits for confirmation (board 5).</summary>
    public bool IsWaiting => State == PairingState.Waiting;

    /// <summary>Gets a value indicating whether the window shows the code layout of boards 5 and 19.</summary>
    public bool IsShowingCode => IsGettingCode || IsWaiting;

    /// <summary>Gets a value indicating whether this computer is paired (board 6).</summary>
    public bool IsPaired => State == PairingState.Paired;

    /// <summary>Gets a value indicating whether the code expired (board 7).</summary>
    public bool IsExpired => State == PairingState.Expired;

    /// <summary>Gets a value indicating whether RaidManager refused the stored token (board 8).</summary>
    public bool IsRevoked => State == PairingState.Revoked;

    /// <summary>Gets a value indicating whether the request for a code failed (board 20).</summary>
    public bool IsCodeRequestFailed => State == PairingState.CodeRequestFailed;

    /// <summary>Gets the code shown to the player, such as <c>K7M-4QX</c>.</summary>
    public string PairingCode
    {
        get => _pairingCode;
        private set => SetProperty(ref _pairingCode, value);
    }

    /// <summary>Gets the countdown, such as "Expires in 9:42".</summary>
    public string ExpiryText
    {
        get => _expiryText;
        private set => SetProperty(ref _expiryText, value);
    }

    /// <summary>Gets the name of the player this computer is paired with.</summary>
    public string PlayerName
    {
        get => _playerName;
        private set
        {
            if (SetProperty(ref _playerName, value))
            {
                OnPropertyChanged(nameof(PairedHeading));
            }
        }
    }

    /// <summary>Gets the heading of board 6.</summary>
    public string PairedHeading => $"Paired with {PlayerName}";

    /// <summary>Gets the explanation of board 8.</summary>
    public string RevokedMessage => $"{ComputerLabel} isn't paired with your account anymore.";
    #endregion Public Properties

    #region Public Methods
    /// <summary>Stops the current pairing flow.</summary>
    public void Dispose()
    {
        _flow.Cancel();
        _flow.Dispose();
    }
    #endregion Public Methods

    #region Private Helpers
    /// <summary>Writes that the request for a code failed.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="exception">The failure.</param>
    [LoggerMessage(Level = LogLevel.Warning, Message = "The companion couldn't get a pairing code from RaidManager.")]
    private static partial void LogCodeRequestFailed(ILogger logger, Exception exception);

    /// <summary>Writes that the pairing flow failed.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="exception">The failure.</param>
    [LoggerMessage(Level = LogLevel.Error, Message = "The companion's pairing flow failed.")]
    private static partial void LogFlowFailed(ILogger logger, Exception exception);

    /// <summary>Writes that the browser couldn't be opened.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="exception">The failure, if any.</param>
    [LoggerMessage(Level = LogLevel.Warning, Message = "The companion couldn't open the website in the default browser.")]
    private static partial void LogBrowserFailed(ILogger logger, Exception? exception);

    /// <summary>Loads the stored pairing and checks it, or requests a code when there is none.</summary>
    /// <returns>A task that completes when the start is done.</returns>
    private async Task StartAsync()
    {
        var stored = await _store.LoadAsync(_flow.Token);
        if (stored is null)
        {
            await RequestCodeAsync();
            return;
        }

        PlayerName = stored.PlayerName;
        State = PairingState.Paired;
        if (await _api.CheckTokenAsync(stored.DeviceToken, _flow.Token) == TokenCheckStatus.Refused)
        {
            _store.Delete();
            State = PairingState.Revoked;
        }
    }

    /// <summary>Requests a new code, shows it, opens the website, and starts waiting for its confirmation.</summary>
    /// <returns>A task that completes once the code is shown; the waiting goes on in the background.</returns>
    private async Task RequestCodeAsync()
    {
        var cancellationToken = RestartFlow();
        PairingCode = string.Empty;
        ExpiryText = string.Empty;
        State = PairingState.GettingCode;

        StartedPairing started;
        try
        {
            started = await _api.StartPairingAsync(ComputerLabel, cancellationToken);
        }
        catch (Exception exception) when (exception is HttpRequestException
                                         || (exception is TaskCanceledException && !cancellationToken.IsCancellationRequested))
        {
            LogCodeRequestFailed(_logger, exception);
            State = PairingState.CodeRequestFailed;
            return;
        }

        PairingCode = started.PairingCode;
        _expiresAt = started.ExpiresAtUtc;
        UpdateExpiry();
        State = PairingState.Waiting;
        await OpenWebsiteCommand.ExecuteAsync();
        _ = WaitForConfirmationAsync(started, cancellationToken);
    }

    /// <summary>Counts down and polls for the token until the code is confirmed or expires, or the flow restarts.</summary>
    /// <param name="started">The pairing.</param>
    /// <param name="cancellationToken">The flow's token.</param>
    /// <returns>A task that completes when the waiting ends.</returns>
    private async Task WaitForConfirmationAsync(StartedPairing started, CancellationToken cancellationToken)
    {
        try
        {
            await PollUntilDoneAsync(started, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // A new code or the end of the companion stopped this flow; nothing is left to show.
        }
        catch (Exception exception)
        {
            // Nothing awaits this background flow, so its failure must become a state here.
            OnFlowFailed(exception);
        }
    }

    /// <summary>Runs the countdown and the polls of one pairing.</summary>
    /// <param name="started">The pairing.</param>
    /// <param name="cancellationToken">The flow's token.</param>
    /// <returns>A task that completes when the code is confirmed or expires.</returns>
    private async Task PollUntilDoneAsync(StartedPairing started, CancellationToken cancellationToken)
    {
        var interval = started.PollingInterval > MinimumPollingInterval ? started.PollingInterval : MinimumPollingInterval;
        var nextPoll = _time.GetUtcNow() + interval;
        while (true)
        {
            await Task.Delay(Tick, _time, cancellationToken);
            UpdateExpiry();
            var now = _time.GetUtcNow();
            if (now >= _expiresAt)
            {
                State = PairingState.Expired;
                return;
            }

            if (now < nextPoll)
            {
                continue;
            }

            var poll = await _api.CollectTokenAsync(started.DeviceCode, cancellationToken);
            switch (poll.Status)
            {
                case TokenPollStatus.Collected:
                    await _store.SaveAsync(poll.Companion!, cancellationToken);
                    PlayerName = poll.Companion!.PlayerName;
                    State = PairingState.Paired;
                    return;
                case TokenPollStatus.Expired or TokenPollStatus.Invalid:
                    State = PairingState.Expired;
                    return;
                case TokenPollStatus.SlowDown or TokenPollStatus.Unavailable:
                    interval += BackOffStep;
                    break;
            }

            nextPoll = now + interval;
        }
    }

    /// <summary>Opens the website's pairing page with the shown code.</summary>
    /// <returns>A task that completes when the browser was asked.</returns>
    private async Task OpenWebsiteAsync()
    {
        if (PairingCode.Length == 0)
        {
            return;
        }

        var address = new Uri(_website, $"companion/pair?code={Uri.EscapeDataString(PairingCode)}");
        if (!await _browser.OpenAsync(address))
        {
            LogBrowserFailed(_logger, null);
        }
    }

    /// <summary>Shows a failed flow as a failed request for a code, the state that offers to try again.</summary>
    /// <param name="exception">The failure.</param>
    private void OnFlowFailed(Exception exception)
    {
        LogFlowFailed(_logger, exception);
        State = PairingState.CodeRequestFailed;
    }

    /// <summary>Records that the browser couldn't be opened; the player can still open the website.</summary>
    /// <param name="exception">The failure.</param>
    private void OnOpenWebsiteFailed(Exception exception) => LogBrowserFailed(_logger, exception);

    /// <summary>Stops the current flow and starts a new one.</summary>
    /// <returns>The new flow's token.</returns>
    private CancellationToken RestartFlow()
    {
        _flow.Cancel();
        _flow.Dispose();
        _flow = new CancellationTokenSource();
        return _flow.Token;
    }

    /// <summary>Updates the countdown from the clock.</summary>
    private void UpdateExpiry()
    {
        var remaining = _expiresAt - _time.GetUtcNow();
        if (remaining < TimeSpan.Zero)
        {
            remaining = TimeSpan.Zero;
        }

        ExpiryText = string.Create(CultureInfo.InvariantCulture, $"Expires in {(int)remaining.TotalMinutes}:{remaining.Seconds:00}");
    }
    #endregion Private Helpers
}
