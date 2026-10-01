using System.Net;
using System.Text;
using Shouldly;
using Xunit;
using RaidManager.ViewModels.Features.Characters;
using RaidManager.Web.Features.Characters;

namespace RaidManager.Web.Tests.Features.Characters;

/// <summary>Verifies how the website calls the claims API and reads its answers.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Checks the routes, the transport record, and the mapping of 204, 404, 409 and failures.
/// </remarks>
public sealed class CharacterClaimsApiClientTests
{
    #region Fields
    /// <summary>Stores the player.</summary>
    private static readonly Guid UserId = Guid.Parse("6d1f7a9e-0000-4000-8000-000000000001");

    /// <summary>Stores the character.</summary>
    private static readonly Guid CharacterId = Guid.Parse("6d1f7a9e-0000-4000-8000-000000000002");
    #endregion Fields

    #region Tests
    /// <summary>Lists the claims from the API's JSON.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task PendingClaimsAreReadFromTheApi()
    {
        var json = $$"""
            [{"characterId":"{{CharacterId}}","realm":"Icecrown","name":"Arthasdk","class":"DeathKnight","race":"Human","level":80,"claimState":"Pending","requestedAtUtc":"2026-10-01T14:05:00+00:00"}]
            """;
        var handler = new StubHandler(HttpStatusCode.OK, json);
        var client = Client(handler);

        var claims = await client.GetPendingAsync(UserId, CancellationToken.None);

        handler.Requests.ShouldBe([$"GET /internal/users/{UserId}/character-claims/pending"]);
        claims.ShouldBe([new CharacterClaim(CharacterId, "Icecrown", "Arthasdk", "DeathKnight", "Human", 80, "Pending", new DateTimeOffset(2026, 10, 1, 14, 5, 0, TimeSpan.Zero))]);
        claims[0].IsPending.ShouldBeTrue();
    }

    /// <summary>Reads an empty body as a failure.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task NullBodyIsAFailure()
    {
        var client = Client(new StubHandler(HttpStatusCode.OK, "null"));

        await Should.ThrowAsync<HttpRequestException>(() => client.GetPendingAsync(UserId, CancellationToken.None));
    }

    /// <summary>Maps each decision status to its outcome.</summary>
    /// <param name="status">The API status.</param>
    /// <param name="expected">The expected outcome.</param>
    /// <returns>A task that completes when the test has run.</returns>
    [Theory]
    [InlineData(HttpStatusCode.NoContent, ClaimDecisionOutcome.Recorded)]
    [InlineData(HttpStatusCode.NotFound, ClaimDecisionOutcome.Refused)]
    [InlineData(HttpStatusCode.Conflict, ClaimDecisionOutcome.Refused)]
    public async Task DecisionStatusesMapToOutcomes(HttpStatusCode status, ClaimDecisionOutcome expected)
    {
        var handler = new StubHandler(status, string.Empty);
        var client = Client(handler);

        (await client.ApproveAsync(UserId, CharacterId, CancellationToken.None)).ShouldBe(expected);
        (await client.RejectAsync(UserId, CharacterId, CancellationToken.None)).ShouldBe(expected);

        handler.Requests.ShouldBe([
            $"POST /internal/users/{UserId}/character-claims/{CharacterId}/approve",
            $"POST /internal/users/{UserId}/character-claims/{CharacterId}/reject",
        ]);
    }

    /// <summary>Fails a decision the API couldn't handle.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task ServerErrorIsAFailure()
    {
        var client = Client(new StubHandler(HttpStatusCode.InternalServerError, string.Empty));

        await Should.ThrowAsync<HttpRequestException>(() => client.ApproveAsync(UserId, CharacterId, CancellationToken.None));
    }
    #endregion Tests

    #region Private Helpers
    /// <summary>Builds a client addressed to a stubbed API.</summary>
    /// <param name="handler">The stub.</param>
    /// <returns>The client.</returns>
    private static CharacterClaimsApiClient Client(StubHandler handler) =>
        new(new HttpClient(handler) { BaseAddress = new Uri("https://api.test/") });
    #endregion Private Helpers

    #region Nested Types
    /// <summary>Answers every request with one status and body, and records the requests.</summary>
    /// <param name="status">The status to answer.</param>
    /// <param name="json">The JSON body to answer.</param>
    private sealed class StubHandler(HttpStatusCode status, string json) : HttpMessageHandler
    {
        /// <summary>Gets the requests received, as method and path.</summary>
        public List<string> Requests { get; } = [];

        /// <inheritdoc />
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add($"{request.Method} {request.RequestUri?.AbsolutePath}");
            return Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") });
        }
    }
    #endregion Nested Types
}
