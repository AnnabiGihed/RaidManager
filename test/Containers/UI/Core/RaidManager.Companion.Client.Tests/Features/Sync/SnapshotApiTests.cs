using System.Net;
using System.Net.Http.Headers;
using System.Text.Json.Nodes;
using RaidManager.Companion.Client.Features.Sync.Upload;
using RaidManager.Companion.Client.Tests.Support;
using Shouldly;
using Xunit;

namespace RaidManager.Companion.Client.Tests.Features.Sync;

/// <summary>Verifies the snapshot upload client against the API's real status codes and problem bodies.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-07<br/>
/// Purpose: Technical tests of <see cref="SnapshotApi"/>: the route, the per-request bearer token, the body in the
/// addon contract's shape, and how each answer of <c>POST /companion/snapshots</c> (#384) maps to an outcome (#550).
/// </remarks>
public sealed class SnapshotApiTests : IDisposable
{
    #region Fields
    /// <summary>Stores the HTTP handler double.</summary>
    private readonly StubHttpMessageHandler _handler = new();

    /// <summary>Stores the HTTP client.</summary>
    private readonly HttpClient _httpClient;

    /// <summary>Stores the client under test.</summary>
    private readonly SnapshotApi _api;
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="SnapshotApiTests"/> class.</summary>
    public SnapshotApiTests()
    {
        _httpClient = new HttpClient(_handler) { BaseAddress = new Uri("https://api.example.test/") };
        _api = new SnapshotApi(_httpClient);
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
    /// <summary>The snapshot is posted to the upload route with the device token and the contract's body.</summary>
    /// <returns>A task that completes when the test is done.</returns>
    [Fact]
    public async Task UploadPostsTheCharacterWithTheTokenAndVersions()
    {
        _handler.Answer(HttpStatusCode.Accepted, """{"outcome":"Imported"}""");

        await _api.UploadAsync("device-token", Snapshots.Queued("Arthasdk", 1791043200), CancellationToken.None);

        var (request, body) = _handler.Requests.ShouldHaveSingleItem();
        request.Method.ShouldBe(HttpMethod.Post);
        request.RequestUri!.AbsolutePath.ShouldBe("/companion/snapshots");
        request.Headers.Authorization.ShouldBe(new AuthenticationHeaderValue("Bearer", "device-token"));
        var json = JsonNode.Parse(body!)!;
        json["schemaVersion"]!.GetValue<int>().ShouldBe(1);
        json["addonVersion"]!.GetValue<string>().ShouldBe("0.1.0");
        json["character"]!["name"]!.GetValue<string>().ShouldBe("Arthasdk");
        json["character"]!["capturedAt"]!.GetValue<long>().ShouldBe(1791043200);
    }

    /// <summary>Each status maps to what the queue does next.</summary>
    /// <param name="status">The API's status.</param>
    /// <param name="expected">The outcome's name.</param>
    /// <returns>A task that completes when the test is done.</returns>
    [Theory]
    [InlineData(HttpStatusCode.Accepted, "Accepted")]
    [InlineData(HttpStatusCode.OK, "Accepted")]
    [InlineData(HttpStatusCode.Unauthorized, "Unauthorized")]
    [InlineData(HttpStatusCode.InternalServerError, "Unavailable")]
    [InlineData(HttpStatusCode.BadGateway, "Unavailable")]
    [InlineData(HttpStatusCode.NotFound, "Unavailable")]
    public async Task StatusesMapToOutcomes(HttpStatusCode status, string expected)
    {
        _handler.Answer(status);

        (await UploadAsync()).Outcome.ShouldBe(Enum.Parse<SnapshotUploadOutcome>(expected));
    }

    /// <summary>A refusal carries the problem's error code.</summary>
    /// <returns>A task that completes when the test is done.</returns>
    [Fact]
    public async Task ARefusalCarriesItsErrorCode()
    {
        _handler.AnswerProblem(HttpStatusCode.BadRequest, "Character.Snapshot.IdentityUnavailable");

        (await UploadAsync()).ShouldBe(new SnapshotUploadResult(SnapshotUploadOutcome.Refused, "Character.Snapshot.IdentityUnavailable"));
    }

    /// <summary>A refusal without a readable problem is still a refusal.</summary>
    /// <returns>A task that completes when the test is done.</returns>
    [Fact]
    public async Task ARefusalWithoutAProblemHasNoErrorCode()
    {
        _handler.Answer(HttpStatusCode.BadRequest, "not json");

        (await UploadAsync()).ShouldBe(new SnapshotUploadResult(SnapshotUploadOutcome.Refused));
    }

    /// <summary>A 429 carries the wait its <c>Retry-After</c> asks for.</summary>
    /// <returns>A task that completes when the test is done.</returns>
    [Fact]
    public async Task ASlowDownCarriesItsRetryAfter()
    {
        _handler.Answer(HttpStatusCode.TooManyRequests);
        _handler.OnResponse = response => response.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(30));

        (await UploadAsync()).ShouldBe(new SnapshotUploadResult(SnapshotUploadOutcome.SlowDown, RetryAfter: TimeSpan.FromSeconds(30)));
    }

    /// <summary>A network failure or a timeout means RaidManager couldn't be reached.</summary>
    /// <param name="timeout">Whether the failure is a timeout rather than a network failure.</param>
    /// <returns>A task that completes when the test is done.</returns>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task NetworkFailuresAndTimeoutsAreUnavailable(bool timeout)
    {
        _handler.Fail(timeout ? new TaskCanceledException("timeout") : new HttpRequestException("offline"));

        (await UploadAsync()).Outcome.ShouldBe(SnapshotUploadOutcome.Unavailable);
    }

    /// <summary>Stopping the companion cancels the upload instead of reading it as a failure.</summary>
    /// <returns>A task that completes when the test is done.</returns>
    [Fact]
    public async Task CancellingTheUploadThrows()
    {
        using var stop = new CancellationTokenSource();
        await stop.CancelAsync();
        _handler.Fail(new TaskCanceledException("stopped"));

        await Should.ThrowAsync<TaskCanceledException>(() => _api.UploadAsync("device-token", Snapshots.Queued("Arthasdk"), stop.Token));
    }
    #endregion Tests

    #region Private Helpers
    /// <summary>Uploads one snapshot.</summary>
    /// <returns>The result.</returns>
    private Task<SnapshotUploadResult> UploadAsync() =>
        _api.UploadAsync("device-token", Snapshots.Queued("Arthasdk"), CancellationToken.None);
    #endregion Private Helpers
}
