using System.Net;
using Shouldly;
using Xunit;
using RaidManager.ViewModels.Features.Companions;
using RaidManager.Web.Features.Companions;
using RaidManager.Web.Tests.Support;

namespace RaidManager.Web.Tests.Features.Companions;

/// <summary>Verifies how the website reads the API's companion routes.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: Proves the routes the website calls and how each answer maps to what the pages show, including 409 reasons and failures.
/// </remarks>
public sealed class CompanionsApiClientTests
{
    #region Fields
    /// <summary>Stores the player.</summary>
    private static readonly Guid UserId = Guid.Parse("6d1f7a9e-0000-4000-8000-000000000011");

    /// <summary>Stores the companion.</summary>
    private static readonly Guid CompanionId = Guid.Parse("6d1f7a9e-0000-4000-8000-000000000012");

    /// <summary>Stores the route of the test code.</summary>
    private static readonly string PairingPath = $"/internal/users/{UserId}/companion-pairings/K7M-4QX";
    #endregion Fields

    #region Tests
    /// <summary>Reads a waiting pairing.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task WaitingPairingIsRead()
    {
        var handler = new RecordingHandler().Answer(
            PairingPath,
            HttpStatusCode.OK,
            """{"pairingCode":"K7M-4QX","computerLabel":"BRYN-DESKTOP","requestedAtUtc":"2026-10-04T14:00:00+00:00","expiresAtUtc":"2026-10-04T14:10:00+00:00"}""");

        var lookup = await Client(handler).GetPairingAsync(UserId, "K7M-4QX", CancellationToken.None);

        lookup.Status.ShouldBe(PairingCodeStatus.Waiting);
        lookup.Pairing.ShouldBe(new PendingPairing("K7M-4QX", "BRYN-DESKTOP", new DateTimeOffset(2026, 10, 4, 14, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 10, 4, 14, 10, 0, TimeSpan.Zero)));
    }

    /// <summary>Maps each refusal of a code.</summary>
    /// <param name="status">The API status.</param>
    /// <param name="title">The ProblemDetails title.</param>
    /// <param name="expected">The expected code status.</param>
    /// <returns>A task that completes when the test has run.</returns>
    [Theory]
    [InlineData(HttpStatusCode.NotFound, "CompanionPairing.NotFound", PairingCodeStatus.Unknown)]
    [InlineData(HttpStatusCode.BadRequest, "One or more validation errors occurred.", PairingCodeStatus.Unknown)]
    [InlineData(HttpStatusCode.Conflict, "CompanionPairing.Expired", PairingCodeStatus.Expired)]
    [InlineData(HttpStatusCode.Conflict, "CompanionPairing.AlreadyConfirmed", PairingCodeStatus.AlreadyConfirmed)]
    public async Task RefusedCodesMapToTheirReason(HttpStatusCode status, string title, PairingCodeStatus expected)
    {
        var json = $$"""{"title":"{{title}}"}""";
        var handler = new RecordingHandler().Answer(PairingPath, status, json).Answer(PairingPath + "/confirm", status, json);
        var client = Client(handler);

        (await client.GetPairingAsync(UserId, "K7M-4QX", CancellationToken.None)).ShouldBe(new PairingLookup(expected, null));
        (await client.ConfirmAsync(UserId, "K7M-4QX", CancellationToken.None)).ShouldBe(expected);

        handler.Requests.Select(request => $"{request.Request.Method} {request.Request.RequestUri!.AbsolutePath}").ShouldBe([$"GET {PairingPath}", $"POST {PairingPath}/confirm"]);
    }

    /// <summary>Confirms a code.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task ConfirmedCodePairs()
    {
        var handler = new RecordingHandler().Answer(PairingPath + "/confirm", HttpStatusCode.NoContent, string.Empty);

        (await Client(handler).ConfirmAsync(UserId, "K7M-4QX", CancellationToken.None)).ShouldBe(PairingCodeStatus.Paired);
    }

    /// <summary>Gets limited or a server error.</summary>
    /// <param name="status">The API status.</param>
    /// <returns>A task that completes when the test has run.</returns>
    [Theory]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task OtherAnswersAreFailures(HttpStatusCode status)
    {
        var handler = new RecordingHandler().Answer(PairingPath, status).Answer(PairingPath + "/confirm", status);
        var client = Client(handler);

        await Should.ThrowAsync<HttpRequestException>(() => client.GetPairingAsync(UserId, "K7M-4QX", CancellationToken.None));
        await Should.ThrowAsync<HttpRequestException>(() => client.ConfirmAsync(UserId, "K7M-4QX", CancellationToken.None));
    }

    /// <summary>Reads an empty pairing body as a failure.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task EmptyPairingIsAFailure()
    {
        var handler = new RecordingHandler().Answer(PairingPath, HttpStatusCode.OK, "null");

        await Should.ThrowAsync<HttpRequestException>(() => Client(handler).GetPairingAsync(UserId, "K7M-4QX", CancellationToken.None));
    }

    /// <summary>Lists the companions.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task CompanionsAreRead()
    {
        var handler = new RecordingHandler().Answer(
            $"/internal/users/{UserId}/companions",
            HttpStatusCode.OK,
            $$"""[{"companionId":"{{CompanionId}}","label":"BRYN-LAPTOP","pairedAtUtc":"2026-09-12T21:40:00+00:00","lastUsedAtUtc":"2026-09-28T21:40:00+00:00","status":"Revoked","revokedAtUtc":"2026-10-04T17:20:00+00:00"}]""");

        var companions = await Client(handler).GetCompanionsAsync(UserId, CancellationToken.None);

        companions.ShouldBe([new PairedCompanion(CompanionId, "BRYN-LAPTOP", new DateTimeOffset(2026, 9, 12, 21, 40, 0, TimeSpan.Zero), "Revoked", new DateTimeOffset(2026, 10, 4, 17, 20, 0, TimeSpan.Zero))]);
    }

    /// <summary>Reads an empty list body as a failure.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task EmptyCompanionsBodyIsAFailure()
    {
        var handler = new RecordingHandler().Answer($"/internal/users/{UserId}/companions", HttpStatusCode.OK, "null");

        await Should.ThrowAsync<HttpRequestException>(() => Client(handler).GetCompanionsAsync(UserId, CancellationToken.None));
    }

    /// <summary>Maps each revocation answer.</summary>
    /// <param name="status">The API status.</param>
    /// <param name="expected">The expected outcome.</param>
    /// <returns>A task that completes when the test has run.</returns>
    [Theory]
    [InlineData(HttpStatusCode.NoContent, RevokeOutcome.Revoked)]
    [InlineData(HttpStatusCode.NotFound, RevokeOutcome.Refused)]
    [InlineData(HttpStatusCode.Conflict, RevokeOutcome.Refused)]
    public async Task RevocationAnswersMapToOutcomes(HttpStatusCode status, RevokeOutcome expected)
    {
        var path = $"/internal/users/{UserId}/companions/{CompanionId}/revoke";
        var handler = new RecordingHandler().Answer(path, status, string.Empty);

        (await Client(handler).RevokeAsync(UserId, CompanionId, CancellationToken.None)).ShouldBe(expected);
        handler.Requests.ShouldHaveSingleItem().Request.Method.ShouldBe(HttpMethod.Post);
    }

    /// <summary>Gets a server error on revocation.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task RevocationServerErrorIsAFailure()
    {
        var handler = new RecordingHandler().Answer($"/internal/users/{UserId}/companions/{CompanionId}/revoke", HttpStatusCode.InternalServerError);

        await Should.ThrowAsync<HttpRequestException>(() => Client(handler).RevokeAsync(UserId, CompanionId, CancellationToken.None));
    }

    /// <summary>Builds the confirm page and list routes.</summary>
    [Fact]
    public void RoutesEscapeTheirValues()
    {
        CompanionRoutes.PairFor("K7M 4QX").ShouldBe("/companion/pair?code=K7M%204QX");
        CompanionRoutes.ListAfterPairing("BRYN DESKTOP").ShouldBe("/companion?paired=BRYN%20DESKTOP");
    }
    #endregion Tests

    #region Private Helpers
    /// <summary>Builds a client on a handler.</summary>
    /// <param name="handler">The handler.</param>
    /// <returns>The client.</returns>
    private static CompanionsApiClient Client(RecordingHandler handler) =>
        new(new HttpClient(handler) { BaseAddress = new Uri("https://api.test/") });
    #endregion Private Helpers
}
