using System.Net;
using RaidManager.Companion.Client.Features.Pairing;
using RaidManager.Companion.Client.Tests.Support;
using Shouldly;
using Xunit;

namespace RaidManager.Companion.Client.Tests.Features.Pairing;

/// <summary>Verifies the companion's HTTP client against the API's answers.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: Technical tests of <see cref="CompanionApi"/>: the routes and bodies it sends, how it reads each answer of
/// the API of #382 from its status and problem title, and that a network failure never escapes a poll or a check.
/// </remarks>
public sealed class CompanionApiTests : IDisposable
{
    #region Fields
    /// <summary>Stores the HTTP stub.</summary>
    private readonly StubHttpMessageHandler _handler = new();

    /// <summary>Stores the HTTP client over the stub.</summary>
    private readonly HttpClient _httpClient;

    /// <summary>Stores the client under test.</summary>
    private readonly CompanionApi _api;
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="CompanionApiTests"/> class.</summary>
    public CompanionApiTests()
    {
        _httpClient = new HttpClient(_handler) { BaseAddress = new Uri("https://api.raidmanager.test/") };
        _api = new CompanionApi(_httpClient);
    }
    #endregion Constructors

    #region Public Methods
    /// <summary>Releases the HTTP client.</summary>
    public void Dispose()
    {
        _httpClient.Dispose();
        _handler.Dispose();
    }
    #endregion Public Methods

    #region Tests
    /// <summary>Starting a pairing posts the label and reads the code, expiry and interval.</summary>
    /// <returns>A task that completes when the test is done.</returns>
    [Fact]
    public async Task StartPairingAsyncWhenTheApiAnswersReturnsThePairing()
    {
        _handler.Answer(HttpStatusCode.OK, """{"deviceCode":"device","pairingCode":"K7M-4QX","expiresAtUtc":"2026-10-04T14:10:00+00:00","pollingIntervalSeconds":5}""");

        var started = await _api.StartPairingAsync("BRYN-DESKTOP", CancellationToken.None);

        started.ShouldBe(new StartedPairing("device", "K7M-4QX", new DateTimeOffset(2026, 10, 4, 14, 10, 0, TimeSpan.Zero), TimeSpan.FromSeconds(5)));
        var (request, body) = _handler.Requests.ShouldHaveSingleItem();
        request.Method.ShouldBe(HttpMethod.Post);
        request.RequestUri.ShouldBe(new Uri("https://api.raidmanager.test/companion/pairings"));
        body.ShouldBe("""{"computerLabel":"BRYN-DESKTOP"}""");
    }

    /// <summary>A refused start is an HTTP failure the view model turns into its failed state.</summary>
    /// <returns>A task that completes when the test is done.</returns>
    [Fact]
    public async Task StartPairingAsyncWhenRateLimitedThrows()
    {
        _handler.AnswerProblem(HttpStatusCode.TooManyRequests, "RateLimited");

        await Should.ThrowAsync<HttpRequestException>(() => _api.StartPairingAsync("BRYN-DESKTOP", CancellationToken.None));
    }

    /// <summary>An empty answer to a start is an HTTP failure.</summary>
    /// <returns>A task that completes when the test is done.</returns>
    [Fact]
    public async Task StartPairingAsyncWhenTheAnswerIsEmptyThrows()
    {
        _handler.Answer(HttpStatusCode.OK, "null");

        await Should.ThrowAsync<HttpRequestException>(() => _api.StartPairingAsync("BRYN-DESKTOP", CancellationToken.None));
    }

    /// <summary>A collected token carries the companion, the player and the token.</summary>
    /// <returns>A task that completes when the test is done.</returns>
    [Fact]
    public async Task CollectTokenAsyncWhenConfirmedReturnsTheCompanion()
    {
        var companionId = Guid.NewGuid();
        _handler.Answer(HttpStatusCode.OK, $$"""{"companionId":"{{companionId}}","deviceToken":"token","playerName":"Bryn"}""");

        var poll = await _api.CollectTokenAsync("device", CancellationToken.None);

        poll.ShouldBe(TokenPoll.Collected(new PairedCompanion(companionId, "Bryn", "token")));
        var (request, body) = _handler.Requests.ShouldHaveSingleItem();
        request.RequestUri.ShouldBe(new Uri("https://api.raidmanager.test/companion/pairings/token"));
        body.ShouldBe("""{"deviceCode":"device"}""");
    }

    /// <summary>Each refusal of a poll is read from its problem title.</summary>
    /// <param name="code">The API's error code.</param>
    /// <param name="expected">The poll status.</param>
    /// <returns>A task that completes when the test is done.</returns>
    [Theory]
    [InlineData("CompanionPairing.Pending", TokenPollStatus.Pending)]
    [InlineData("CompanionPairing.Expired", TokenPollStatus.Expired)]
    [InlineData("CompanionPairing.Invalid", TokenPollStatus.Invalid)]
    [InlineData("Something.Else", TokenPollStatus.Unavailable)]
    public async Task CollectTokenAsyncWhenRefusedReadsTheErrorCode(string code, TokenPollStatus expected)
    {
        _handler.AnswerProblem(HttpStatusCode.BadRequest, code);

        (await _api.CollectTokenAsync("device", CancellationToken.None)).Status.ShouldBe(expected);
    }

    /// <summary>Answers other than a token or a refusal are read by their status.</summary>
    /// <param name="status">The status.</param>
    /// <param name="body">The body.</param>
    /// <param name="expected">The poll status.</param>
    /// <returns>A task that completes when the test is done.</returns>
    [Theory]
    [InlineData(HttpStatusCode.TooManyRequests, null, TokenPollStatus.SlowDown)]
    [InlineData(HttpStatusCode.InternalServerError, null, TokenPollStatus.Unavailable)]
    [InlineData(HttpStatusCode.BadRequest, "not json", TokenPollStatus.Unavailable)]
    [InlineData(HttpStatusCode.OK, "null", TokenPollStatus.Unavailable)]
    public async Task CollectTokenAsyncWhenTheAnswerIsNotARefusalReadsTheStatus(HttpStatusCode status, string? body, TokenPollStatus expected)
    {
        _handler.Answer(status, body);

        (await _api.CollectTokenAsync("device", CancellationToken.None)).Status.ShouldBe(expected);
    }

    /// <summary>A network failure or a timeout during a poll is unavailable, not an exception.</summary>
    /// <param name="timeout">Whether the failure is a timeout.</param>
    /// <returns>A task that completes when the test is done.</returns>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CollectTokenAsyncWhenTheNetworkFailsIsUnavailable(bool timeout)
    {
        _handler.Fail(timeout ? new TaskCanceledException("Timed out.") : new HttpRequestException("No route."));

        (await _api.CollectTokenAsync("device", CancellationToken.None)).Status.ShouldBe(TokenPollStatus.Unavailable);
    }

    /// <summary>A poll canceled by the companion is canceled, not unavailable.</summary>
    /// <returns>A task that completes when the test is done.</returns>
    [Fact]
    public async Task CollectTokenAsyncWhenCanceledThrows()
    {
        using var source = new CancellationTokenSource();
        await source.CancelAsync();
        _handler.Fail(new TaskCanceledException("Canceled."));

        await Should.ThrowAsync<TaskCanceledException>(() => _api.CollectTokenAsync("device", source.Token));
    }

    /// <summary>A check sends the token as a bearer token on its own request.</summary>
    /// <returns>A task that completes when the test is done.</returns>
    [Fact]
    public async Task CheckTokenAsyncWhenAcceptedIsValid()
    {
        _handler.Answer(HttpStatusCode.OK, """{"companionId":"6d1f8d5e-3e8e-4a33-9f2c-6f0b7d2d8e11","label":"BRYN-DESKTOP"}""");

        (await _api.CheckTokenAsync("token", CancellationToken.None)).ShouldBe(TokenCheckStatus.Valid);

        var (request, _) = _handler.Requests.ShouldHaveSingleItem();
        request.Method.ShouldBe(HttpMethod.Get);
        request.RequestUri.ShouldBe(new Uri("https://api.raidmanager.test/companion/me"));
        request.Headers.Authorization.ShouldNotBeNull().ToString().ShouldBe("Bearer token");
        _httpClient.DefaultRequestHeaders.Authorization.ShouldBeNull();
    }

    /// <summary>Only the three refusals of a token make the companion forget it.</summary>
    /// <param name="status">The status.</param>
    /// <param name="code">The error code.</param>
    /// <param name="expected">The check status.</param>
    /// <returns>A task that completes when the test is done.</returns>
    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, "Companion.Revoked", TokenCheckStatus.Refused)]
    [InlineData(HttpStatusCode.Unauthorized, "Companion.Expired", TokenCheckStatus.Refused)]
    [InlineData(HttpStatusCode.Unauthorized, "Companion.TokenUnknown", TokenCheckStatus.Refused)]
    [InlineData(HttpStatusCode.Unauthorized, "Something.Else", TokenCheckStatus.Unavailable)]
    [InlineData(HttpStatusCode.InternalServerError, "DatabaseError", TokenCheckStatus.Unavailable)]
    public async Task CheckTokenAsyncWhenRefusedReadsTheErrorCode(HttpStatusCode status, string code, TokenCheckStatus expected)
    {
        _handler.AnswerProblem(status, code);

        (await _api.CheckTokenAsync("token", CancellationToken.None)).ShouldBe(expected);
    }

    /// <summary>A check that can't reach RaidManager is unavailable, so the token is kept.</summary>
    /// <returns>A task that completes when the test is done.</returns>
    [Fact]
    public async Task CheckTokenAsyncWhenTheNetworkFailsIsUnavailable()
    {
        _handler.Fail(new HttpRequestException("No route."));

        (await _api.CheckTokenAsync("token", CancellationToken.None)).ShouldBe(TokenCheckStatus.Unavailable);
    }
    #endregion Tests
}
