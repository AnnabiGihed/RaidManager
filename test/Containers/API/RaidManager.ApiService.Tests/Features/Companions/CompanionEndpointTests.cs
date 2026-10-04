using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;
using RaidManager.ApiService.Features.Companions;
using RaidManager.ApiService.Features.Shared.Authentication;
using RaidManager.ApiService.Tests.Support;
using RaidManager.Domain.Features.Identity.Aggregates;
using RaidManager.Domain.Features.Identity.Repositories;
using RaidManager.Domain.Features.Identity.ValueObjects;
using DomainUnitOfWork = Pivot.Framework.Domain.Repositories.IUnitOfWork;

namespace RaidManager.ApiService.Tests.Features.Companions;

/// <summary>Verifies companion pairing, the device token and revocation through HTTP and PostgreSQL.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: Proves each criterion of #15 on the API side: pairing binds one companion to the player who confirmed its
/// code, within ten minutes and once; the website lists and revokes companions; and a missing, unknown, revoked or
/// expired token is refused on companion routes, with the reason the companion shows. Also proves the rate limits.
/// </remarks>
[Collection(ApiTestGroup.Name)]
public sealed class CompanionEndpointTests : IDisposable
{
    #region Constants
    /// <summary>Defines the computer label the tests pair.</summary>
    private const string Label = "BRYN-DESKTOP";
    #endregion Constants

    #region Fields
    /// <summary>Stores the API factory.</summary>
    private readonly ApiFactory _api;

    /// <summary>Stores the client address this test's companion calls from.</summary>
    private readonly string _address = $"10.{Random.Shared.Next(256)}.{Random.Shared.Next(256)}.{Random.Shared.Next(1, 255)}";
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="CompanionEndpointTests"/> class.</summary>
    /// <param name="api">The API factory.</param>
    public CompanionEndpointTests(ApiFactory api)
    {
        _api = api;
    }
    #endregion Constructors

    #region Public Methods
    /// <summary>Puts the clock back for the next test.</summary>
    public void Dispose() => _api.Clock.Offset = TimeSpan.Zero;
    #endregion Public Methods

    #region Tests
    /// <summary>Pairs a companion, uses it, lists it, revokes it and uses it again.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task PairedCompanionIsAdmittedUntilItsPlayerRevokesIt()
    {
        var bryn = await RegisterAsync("Bryn Valewood");
        using var companion = CompanionClient();
        using var website = WebsiteClient();
        var started = await StartAsync(companion, "  " + Label + "  ");

        var pending = await companion.PostAsJsonAsync("/companion/pairings/token", new CollectCompanionTokenRequest(started.DeviceCode));
        var details = await website.GetFromJsonAsync<CompanionPairingDetails>(PairingRoute(bryn, started.PairingCode.ToLowerInvariant()));
        var confirmed = await website.PostAsync(PairingRoute(bryn, started.PairingCode) + "/confirm", null);
        var collected = await companion.PostAsJsonAsync("/companion/pairings/token", new CollectCompanionTokenRequest(started.DeviceCode));
        var again = await companion.PostAsJsonAsync("/companion/pairings/token", new CollectCompanionTokenRequest(started.DeviceCode));

        (await ProblemAsync(pending, HttpStatusCode.BadRequest)).Title.ShouldBe("CompanionPairing.Pending");
        details.ShouldNotBeNull().ComputerLabel.ShouldBe(Label);
        details.PairingCode.ShouldBe(started.PairingCode);
        confirmed.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        collected.StatusCode.ShouldBe(HttpStatusCode.OK);
        var token = (await collected.Content.ReadFromJsonAsync<CompanionToken>()).ShouldNotBeNull();
        token.PlayerName.ShouldBe("Bryn Valewood");
        token.DeviceToken.Length.ShouldBe(43);
        (await ProblemAsync(again, HttpStatusCode.BadRequest)).Title.ShouldBe("CompanionPairing.Invalid");

        var me = await MeAsync(token.DeviceToken);
        me.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await me.Content.ReadFromJsonAsync<CurrentCompanion>()).ShouldBe(new CurrentCompanion(token.CompanionId, Label));
        var listed = (await ListAsync(website, bryn)).ShouldHaveSingleItem();
        (listed.CompanionId, listed.Label, listed.Status, listed.RevokedAtUtc).ShouldBe((token.CompanionId, Label, "Active", (DateTimeOffset?)null));

        var revoked = await website.PostAsync(CompanionRoute(bryn, token.CompanionId) + "/revoke", null);
        var refused = await MeAsync(token.DeviceToken);
        var revokedTwice = await website.PostAsync(CompanionRoute(bryn, token.CompanionId) + "/revoke", null);

        revoked.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await ProblemAsync(refused, HttpStatusCode.Unauthorized)).Title.ShouldBe("Companion.Revoked");
        refused.Headers.WwwAuthenticate.ToString().ShouldContain("invalid_token");
        (await ProblemAsync(revokedTwice, HttpStatusCode.Conflict)).Title.ShouldBe("Companion.AlreadyRevoked");
        var afterRevoking = (await ListAsync(website, bryn)).ShouldHaveSingleItem();
        afterRevoking.Status.ShouldBe("Revoked");
        afterRevoking.RevokedAtUtc.ShouldNotBeNull();
    }

    /// <summary>Calls a companion route without a token and with a token nobody holds.</summary>
    /// <param name="token">The token sent, or an empty value for none.</param>
    /// <returns>A task that completes when the test has run.</returns>
    [Theory]
    [InlineData("")]
    [InlineData("not-a-device-token")]
    public async Task UnpairedCompanionIsRefused(string token)
    {
        using var client = CompanionClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/companion/me");
        if (token.Length > 0)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        var response = await client.SendAsync(request);

        (await ProblemAsync(response, HttpStatusCode.Unauthorized)).Title.ShouldBe("Companion.TokenUnknown");
    }

    /// <summary>Uses a code after its ten minutes.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task ExpiredCodeCanNeitherBeConfirmedNorCollected()
    {
        var bryn = await RegisterAsync("Bryn Valewood");
        using var companion = CompanionClient();
        using var website = WebsiteClient();
        var started = await StartAsync(companion, Label);
        _api.Clock.Offset = TimeSpan.FromMinutes(11);

        var details = await website.GetAsync(PairingRoute(bryn, started.PairingCode));
        var confirmed = await website.PostAsync(PairingRoute(bryn, started.PairingCode) + "/confirm", null);
        var collected = await companion.PostAsJsonAsync("/companion/pairings/token", new CollectCompanionTokenRequest(started.DeviceCode));

        (await ProblemAsync(details, HttpStatusCode.Conflict)).Title.ShouldBe("CompanionPairing.Expired");
        (await ProblemAsync(confirmed, HttpStatusCode.Conflict)).Title.ShouldBe("CompanionPairing.Expired");
        (await ProblemAsync(collected, HttpStatusCode.BadRequest)).Title.ShouldBe("CompanionPairing.Expired");
    }

    /// <summary>Uses a companion after 180 days without use.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task CompanionUnusedForMoreThan180DaysIsRefused()
    {
        var bryn = await RegisterAsync("Bryn Valewood");
        var token = await PairAsync(bryn);
        _api.Clock.Offset = TimeSpan.FromDays(181);
        using var website = WebsiteClient();

        var refused = await MeAsync(token.DeviceToken);

        (await ProblemAsync(refused, HttpStatusCode.Unauthorized)).Title.ShouldBe("Companion.Expired");
        (await ListAsync(website, bryn)).ShouldHaveSingleItem().Status.ShouldBe("Expired");
    }

    /// <summary>Confirms a code twice, an unknown code, a malformed code and as an unknown player.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task ConfirmationNeedsAKnownPlayerAndAWaitingCode()
    {
        var bryn = await RegisterAsync("Bryn Valewood");
        using var companion = CompanionClient();
        using var website = WebsiteClient();
        var started = await StartAsync(companion, Label);

        var unknownPlayer = await website.PostAsync(PairingRoute(Guid.NewGuid(), started.PairingCode) + "/confirm", null);
        var first = await website.PostAsync(PairingRoute(bryn, started.PairingCode) + "/confirm", null);
        var second = await website.PostAsync(PairingRoute(bryn, started.PairingCode) + "/confirm", null);
        var lookup = await website.GetAsync(PairingRoute(bryn, started.PairingCode));
        var unknownCode = await website.PostAsync(PairingRoute(bryn, "ZZZ-ZZ2") + "/confirm", null);
        var malformed = await website.PostAsync(PairingRoute(bryn, "K0M-4QX") + "/confirm", null);

        (await ProblemAsync(unknownPlayer, HttpStatusCode.NotFound)).Title.ShouldBe("User.NotFound");
        first.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await ProblemAsync(second, HttpStatusCode.Conflict)).Title.ShouldBe("CompanionPairing.AlreadyConfirmed");
        (await ProblemAsync(lookup, HttpStatusCode.Conflict)).Title.ShouldBe("CompanionPairing.AlreadyConfirmed");
        (await ProblemAsync(unknownCode, HttpStatusCode.NotFound)).Title.ShouldBe("CompanionPairing.NotFound");
        malformed.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await malformed.Content.ReadFromJsonAsync<ValidationProblemDetails>()).ShouldNotBeNull().Errors.Keys.ShouldContain("PairingCode");
    }

    /// <summary>Revokes another player's companion.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task PlayerCannotRevokeAnotherPlayersCompanion()
    {
        var bryn = await RegisterAsync("Bryn Valewood");
        var mallory = await RegisterAsync("Mallory");
        var token = await PairAsync(bryn);
        using var website = WebsiteClient();

        var response = await website.PostAsync(CompanionRoute(mallory, token.CompanionId) + "/revoke", null);

        (await ProblemAsync(response, HttpStatusCode.NotFound)).Title.ShouldBe("Companion.NotFound");
        (await MeAsync(token.DeviceToken)).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await ListAsync(website, mallory)).ShouldBeEmpty();
    }

    /// <summary>Starts a pairing with an oversized label.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task OversizedComputerLabelIsAValidationProblem()
    {
        using var companion = CompanionClient();

        var response = await companion.PostAsJsonAsync("/companion/pairings", new StartCompanionPairingRequest(new string('x', 201)));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.Content.ReadFromJsonAsync<ValidationProblemDetails>()).ShouldNotBeNull().Errors.Keys.ShouldContain("ComputerLabel");
    }

    /// <summary>Starts more pairings from one address than the limit allows.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task PairingStartsAreLimitedPerAddress()
    {
        using var companion = CompanionClient();
        using var elsewhere = CompanionClient("192.0.2.77");
        for (var attempt = 0; attempt < new CompanionRateLimitOptions().PairingStartsPerWindow; attempt++)
        {
            (await companion.PostAsJsonAsync("/companion/pairings", new StartCompanionPairingRequest(Label))).EnsureSuccessStatusCode();
        }

        var refused = await companion.PostAsJsonAsync("/companion/pairings", new StartCompanionPairingRequest(Label));
        var otherAddress = await elsewhere.PostAsJsonAsync("/companion/pairings", new StartCompanionPairingRequest(Label));

        (await ProblemAsync(refused, HttpStatusCode.TooManyRequests)).Title.ShouldBe(CompanionRateLimits.RateLimitedTitle);
        refused.Headers.RetryAfter.ShouldNotBeNull();
        otherAddress.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    /// <summary>Polls for a token more often than the limit allows.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task TokenPollsAreLimitedPerAddress()
    {
        using var companion = CompanionClient();
        for (var attempt = 0; attempt < new CompanionRateLimitOptions().TokenPollsPerWindow; attempt++)
        {
            (await companion.PostAsJsonAsync("/companion/pairings/token", new CollectCompanionTokenRequest("unknown"))).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        }

        var refused = await companion.PostAsJsonAsync("/companion/pairings/token", new CollectCompanionTokenRequest("unknown"));

        refused.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
    }

    /// <summary>Tries more codes for one player than the limit allows.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task CodeChecksAreLimitedPerPlayer()
    {
        var bryn = Guid.NewGuid();
        using var website = WebsiteClient();
        for (var attempt = 0; attempt < new CompanionRateLimitOptions().CodeChecksPerWindow; attempt++)
        {
            (await website.GetAsync(PairingRoute(bryn, "ZZZ-ZZ2"))).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        }

        var refused = await website.PostAsync(PairingRoute(bryn, "ZZZ-ZZ2") + "/confirm", null);
        var otherPlayer = await website.GetAsync(PairingRoute(Guid.NewGuid(), "ZZZ-ZZ2"));

        refused.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
        otherPlayer.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    /// <summary>Calls the website's routes without the website key.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task WebsiteRoutesRequireTheWebsiteKey()
    {
        using var client = _api.CreateClient();

        var list = await client.GetAsync(CompanionsRoute(Guid.NewGuid()));
        var confirm = await client.PostAsync(PairingRoute(Guid.NewGuid(), "K7M-4QX") + "/confirm", null);

        list.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        confirm.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    /// <summary>Reads the OpenAPI document.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task OpenApiDocumentDescribesTheCompanionRoutesAndTheirToken()
    {
        using var client = _api.CreateClient();

        var document = await client.GetStringAsync("/openapi/v1.json");

        document.ShouldContain("/companion/pairings");
        document.ShouldContain("/companion/pairings/token");
        document.ShouldContain("/companion/me");
        document.ShouldContain("/internal/users/{userId}/companion-pairings/{pairingCode}/confirm");
        document.ShouldContain("/internal/users/{userId}/companions/{companionId}/revoke");
        document.ShouldContain(CompanionTokenDefaults.Scheme);
    }
    #endregion Tests

    #region Private Helpers
    /// <summary>Builds a website route about a pairing code.</summary>
    /// <param name="userId">The player.</param>
    /// <param name="code">The code.</param>
    /// <returns>The route.</returns>
    private static string PairingRoute(Guid userId, string code) =>
        CompanionManagementEndpoints.PairingsRoute.Replace("{userId:guid}", userId.ToString(), StringComparison.Ordinal) + "/" + code;

    /// <summary>Builds the website route of a player's companions.</summary>
    /// <param name="userId">The player.</param>
    /// <returns>The route.</returns>
    private static string CompanionsRoute(Guid userId) =>
        CompanionManagementEndpoints.CompanionsRoute.Replace("{userId:guid}", userId.ToString(), StringComparison.Ordinal);

    /// <summary>Builds the website route of one companion.</summary>
    /// <param name="userId">The player.</param>
    /// <param name="companionId">The companion.</param>
    /// <returns>The route.</returns>
    private static string CompanionRoute(Guid userId, Guid companionId) => CompanionsRoute(userId) + "/" + companionId;

    /// <summary>Lists a player's companions.</summary>
    /// <param name="website">The website client.</param>
    /// <param name="userId">The player.</param>
    /// <returns>The companions.</returns>
    private static async Task<List<PairedCompanion>> ListAsync(HttpClient website, Guid userId) =>
        (await website.GetFromJsonAsync<List<PairedCompanion>>(CompanionsRoute(userId))).ShouldNotBeNull();

    /// <summary>Starts a pairing.</summary>
    /// <param name="companion">The companion client.</param>
    /// <param name="label">The computer label.</param>
    /// <returns>The started pairing.</returns>
    private static async Task<StartedCompanionPairing> StartAsync(HttpClient companion, string label)
    {
        var response = await companion.PostAsJsonAsync("/companion/pairings", new StartCompanionPairingRequest(label));
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var started = (await response.Content.ReadFromJsonAsync<StartedCompanionPairing>()).ShouldNotBeNull();
        started.PairingCode.ShouldMatch("^[2-9A-HJKMNP-Z]{3}-[2-9A-HJKMNP-Z]{3}$");
        started.PollingIntervalSeconds.ShouldBe(5);
        return started;
    }

    /// <summary>Reads a ProblemDetails answer after checking its status.</summary>
    /// <param name="response">The response.</param>
    /// <param name="status">The expected status.</param>
    /// <returns>The problem.</returns>
    private static async Task<ProblemDetails> ProblemAsync(HttpResponseMessage response, HttpStatusCode status)
    {
        response.StatusCode.ShouldBe(status);
        return (await response.Content.ReadFromJsonAsync<ProblemDetails>()).ShouldNotBeNull();
    }

    /// <summary>Pairs a companion for a player through the whole flow.</summary>
    /// <param name="userId">The player.</param>
    /// <returns>The companion's token.</returns>
    private async Task<CompanionToken> PairAsync(Guid userId)
    {
        using var companion = CompanionClient();
        using var website = WebsiteClient();
        var started = await StartAsync(companion, Label);
        (await website.PostAsync(PairingRoute(userId, started.PairingCode) + "/confirm", null)).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        var collected = await companion.PostAsJsonAsync("/companion/pairings/token", new CollectCompanionTokenRequest(started.DeviceCode));
        return (await collected.Content.ReadFromJsonAsync<CompanionToken>()).ShouldNotBeNull();
    }

    /// <summary>Calls the companion's status route with a token.</summary>
    /// <param name="token">The device token.</param>
    /// <returns>The response.</returns>
    private async Task<HttpResponseMessage> MeAsync(string token)
    {
        using var client = CompanionClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await client.GetAsync("/companion/me");
    }

    /// <summary>Registers a player.</summary>
    /// <param name="displayName">The Discord display name.</param>
    /// <returns>The player's identifier.</returns>
    private async Task<Guid> RegisterAsync(string displayName)
    {
        var user = User.Register(DiscordUserId.Create(Random.Shared.NextInt64(100_000_000_000_000_000, 999_999_999_999_999_999).ToString(System.Globalization.CultureInfo.InvariantCulture)), displayName, avatarUrl: null);
        await using var scope = _api.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<IUserRepository>().AddAsync(user);
        var saved = await scope.ServiceProvider.GetRequiredService<DomainUnitOfWork>().SaveChangesAsync();
        saved.IsSuccess.ShouldBeTrue(saved.IsFailure ? saved.Error.Message : null);
        return user.Id.Value;
    }

    /// <summary>Builds a client that calls from this test's address, as a companion does.</summary>
    /// <returns>The client.</returns>
    private HttpClient CompanionClient() => CompanionClient(_address);

    /// <summary>Builds a client that calls from an address, as a companion does.</summary>
    /// <param name="address">The client address.</param>
    /// <returns>The client.</returns>
    private HttpClient CompanionClient(string address)
    {
        var client = _api.CreateClient();
        client.DefaultRequestHeaders.Add(ClientAddressStartupFilter.HeaderName, address);
        return client;
    }

    /// <summary>Builds a client that presents the website key.</summary>
    /// <returns>The client.</returns>
    private HttpClient WebsiteClient()
    {
        var client = _api.CreateClient();
        client.DefaultRequestHeaders.Add(WebsiteServiceDefaults.HeaderName, ApiFactory.WebsiteServiceKey);
        return client;
    }
    #endregion Private Helpers
}
