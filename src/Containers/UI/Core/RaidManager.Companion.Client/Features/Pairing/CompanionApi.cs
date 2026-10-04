using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace RaidManager.Companion.Client.Features.Pairing;

/// <summary>Calls the API's public <c>/companion</c> routes over HTTPS.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: Implements the companion side of ADR-0030: starting a pairing, polling for the device token and checking a
/// stored token. Failures are read from the problem title, never from the message; the token is sent per request.
/// </remarks>
internal sealed class CompanionApi : ICompanionApi
{
    #region Constants
    /// <summary>Defines the route that starts a pairing.</summary>
    private const string PairingsRoute = "companion/pairings";

    /// <summary>Defines the route the companion polls for its token.</summary>
    private const string TokenRoute = "companion/pairings/token";

    /// <summary>Defines the route that checks a device token.</summary>
    private const string CurrentCompanionRoute = "companion/me";
    #endregion Constants

    #region Fields
    /// <summary>Maps the API's pairing error codes to poll answers.</summary>
    private static readonly Dictionary<string, TokenPollStatus> PollRefusals = new(StringComparer.Ordinal)
    {
        ["CompanionPairing.Pending"] = TokenPollStatus.Pending,
        ["CompanionPairing.Expired"] = TokenPollStatus.Expired,
        ["CompanionPairing.Invalid"] = TokenPollStatus.Invalid,
    };

    /// <summary>Lists the error codes of a token RaidManager refuses for good.</summary>
    private static readonly HashSet<string> TokenRefusals = new(StringComparer.Ordinal)
    {
        "Companion.Revoked",
        "Companion.Expired",
        "Companion.TokenUnknown",
    };

    /// <summary>Stores the HTTP client, addressed to the environment's public API host.</summary>
    private readonly HttpClient _httpClient;
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="CompanionApi"/> class.</summary>
    /// <param name="httpClient">The HTTP client, addressed to the API host.</param>
    public CompanionApi(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }
    #endregion Constructors

    #region Public Methods
    /// <inheritdoc />
    public async Task<StartedPairing> StartPairingAsync(string computerLabel, CancellationToken cancellationToken)
    {
        using var content = JsonContent.Create(new StartPairingRequest(computerLabel), CompanionApiJsonContext.Default.StartPairingRequest);
        using var response = await _httpClient.PostAsync(PairingsRoute, content, cancellationToken);
        response.EnsureSuccessStatusCode();
        var started = await response.Content.ReadFromJsonAsync(CompanionApiJsonContext.Default.StartedPairingResponse, cancellationToken)
            ?? throw new HttpRequestException("The API returned an empty pairing.");
        return new StartedPairing(started.DeviceCode, started.PairingCode, started.ExpiresAtUtc, TimeSpan.FromSeconds(started.PollingIntervalSeconds));
    }

    /// <inheritdoc />
    public async Task<TokenPoll> CollectTokenAsync(string deviceCode, CancellationToken cancellationToken)
    {
        try
        {
            using var content = JsonContent.Create(new CollectTokenRequest(deviceCode), CompanionApiJsonContext.Default.CollectTokenRequest);
            using var response = await _httpClient.PostAsync(TokenRoute, content, cancellationToken);
            return await PollAnswerAsync(response, cancellationToken);
        }
        catch (Exception exception) when (IsUnavailable(exception, cancellationToken))
        {
            return new TokenPoll(TokenPollStatus.Unavailable);
        }
    }

    /// <inheritdoc />
    public async Task<TokenCheckStatus> CheckTokenAsync(string deviceToken, CancellationToken cancellationToken)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, CurrentCompanionRoute);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", deviceToken);
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return TokenCheckStatus.Valid;
            }

            if (response.StatusCode == HttpStatusCode.Unauthorized && TokenRefusals.Contains(await ErrorCodeAsync(response, cancellationToken)))
            {
                return TokenCheckStatus.Refused;
            }

            return TokenCheckStatus.Unavailable;
        }
        catch (Exception exception) when (IsUnavailable(exception, cancellationToken))
        {
            return TokenCheckStatus.Unavailable;
        }
    }
    #endregion Public Methods

    #region Private Helpers
    /// <summary>Reads a poll's answer: the token, a refusal from its error code, a request to slow down, or a failure.</summary>
    /// <param name="response">The API's answer.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>The poll's answer.</returns>
    private static async Task<TokenPoll> PollAnswerAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            var token = await response.Content.ReadFromJsonAsync(CompanionApiJsonContext.Default.CompanionTokenResponse, cancellationToken);
            return token is null
                ? new TokenPoll(TokenPollStatus.Unavailable)
                : TokenPoll.Collected(new PairedCompanion(token.CompanionId, token.PlayerName, token.DeviceToken));
        }

        if (response.StatusCode == HttpStatusCode.TooManyRequests)
        {
            return new TokenPoll(TokenPollStatus.SlowDown);
        }

        if (response.StatusCode == HttpStatusCode.BadRequest
            && PollRefusals.TryGetValue(await ErrorCodeAsync(response, cancellationToken), out var refusal))
        {
            return new TokenPoll(refusal);
        }

        return new TokenPoll(TokenPollStatus.Unavailable);
    }

    /// <summary>Reads the error code from a problem answer.</summary>
    /// <param name="response">The API's answer.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>The error code, or an empty string when the answer has none.</returns>
    private static async Task<string> ErrorCodeAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            var problem = await response.Content.ReadFromJsonAsync(CompanionApiJsonContext.Default.ProblemResponse, cancellationToken);
            return problem?.Title ?? string.Empty;
        }
        catch (System.Text.Json.JsonException)
        {
            return string.Empty;
        }
    }

    /// <summary>Determines whether a failure means RaidManager couldn't be reached, as opposed to the companion stopping.</summary>
    /// <param name="exception">The failure.</param>
    /// <param name="cancellationToken">The caller's token.</param>
    /// <returns><see langword="true"/> for a network failure or a timeout.</returns>
    private static bool IsUnavailable(Exception exception, CancellationToken cancellationToken) =>
        exception is HttpRequestException || (exception is TaskCanceledException && !cancellationToken.IsCancellationRequested);
    #endregion Private Helpers
}
