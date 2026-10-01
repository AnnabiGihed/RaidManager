using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Shouldly;
using Xunit;
using RaidManager.ApiService.Features.Communities;
using RaidManager.ApiService.Features.Identity;
using RaidManager.ApiService.Features.Shared.Authentication;
using RaidManager.ApiService.Tests.Support;

namespace RaidManager.ApiService.Tests.Features.Communities;

/// <summary>Verifies the website-only community endpoints against the real API and SQL Server.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Proves a server links once, with its installer as Administrator, and reads back the same way from every route.
/// </remarks>
public sealed class CommunityEndpointTests : IClassFixture<ApiFactory>
{
    #region Fields
    /// <summary>Stores the API host.</summary>
    private readonly ApiFactory _api;
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="CommunityEndpointTests"/> class.</summary>
    /// <param name="api">The API host.</param>
    public CommunityEndpointTests(ApiFactory api)
    {
        _api = api;
    }
    #endregion Constructors

    #region Tests
    /// <summary>Refuses a caller without the website key.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task RequestWithoutTheWebsiteKeyIsUnauthorized()
    {
        using var client = _api.CreateClient();

        var response = await client.GetAsync($"/internal/users/{Guid.NewGuid()}/communities");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    /// <summary>Links a server and reads it back by id, by server and through the Administrator's communities.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task ALinkedServerReadsBackFromEveryRoute()
    {
        using var client = WebsiteClient();
        var administrator = await SignInAsync(client, "Gihed");
        var serverId = NewSnowflake();

        var response = await client.PostAsJsonAsync(CommunityEndpoints.CommunitiesRoute, new LinkCommunityRequest(serverId, "Dark Templars", "Icecrown", administrator));

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var linked = (await response.Content.ReadFromJsonAsync<LinkCommunityResponse>()).ShouldNotBeNull();
        response.Headers.Location.ShouldBe(new Uri($"{CommunityEndpoints.CommunitiesRoute}/{linked.CommunityId}", UriKind.Relative));
        var expected = new CommunitySummary(linked.CommunityId, serverId, "Dark Templars", "Icecrown", administrator, "Gihed");
        (await client.GetFromJsonAsync<CommunitySummary>($"{CommunityEndpoints.CommunitiesRoute}/{linked.CommunityId}")).ShouldBe(expected);
        (await client.GetFromJsonAsync<CommunitySummary>($"{CommunityEndpoints.CommunitiesRoute}/by-discord-server/{serverId}")).ShouldBe(expected);
        (await client.GetFromJsonAsync<List<CommunitySummary>>($"/internal/users/{administrator}/communities")).ShouldBe([expected]);
    }

    /// <summary>Refuses to link a server twice and keeps the first link.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task LinkingAServerTwiceIsAConflictAndChangesNothing()
    {
        using var client = WebsiteClient();
        var first = await SignInAsync(client, "First");
        var second = await SignInAsync(client, "Second");
        var serverId = NewSnowflake();
        (await client.PostAsJsonAsync(CommunityEndpoints.CommunitiesRoute, new LinkCommunityRequest(serverId, "First link", "Icecrown", first)))
            .StatusCode.ShouldBe(HttpStatusCode.Created);

        var again = await client.PostAsJsonAsync(CommunityEndpoints.CommunitiesRoute, new LinkCommunityRequest(serverId, "Second link", "Lordaeron", second));

        again.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        var kept = (await client.GetFromJsonAsync<CommunitySummary>($"{CommunityEndpoints.CommunitiesRoute}/by-discord-server/{serverId}")).ShouldNotBeNull();
        kept.Name.ShouldBe("First link");
        kept.AdministratorId.ShouldBe(first);
        (await client.GetFromJsonAsync<List<CommunitySummary>>($"/internal/users/{second}/communities")).ShouldNotBeNull().ShouldBeEmpty();
    }

    /// <summary>Answers 404 for a community, a server or a user that doesn't exist.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task UnknownCommunitiesServersAndUsersAreNotFound()
    {
        using var client = WebsiteClient();

        (await client.GetAsync($"{CommunityEndpoints.CommunitiesRoute}/{Guid.NewGuid()}")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await client.GetAsync($"{CommunityEndpoints.CommunitiesRoute}/by-discord-server/{NewSnowflake()}")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await client.PostAsJsonAsync(CommunityEndpoints.CommunitiesRoute, new LinkCommunityRequest(NewSnowflake(), "Nobody", "Icecrown", Guid.NewGuid())))
            .StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    /// <summary>Rejects an unknown realm and a server id that isn't a snowflake, naming the field.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task InvalidInputIsABadRequestWithTheField()
    {
        using var client = WebsiteClient();
        var user = await SignInAsync(client, "Validator");

        var badRealm = await client.PostAsJsonAsync(CommunityEndpoints.CommunitiesRoute, new LinkCommunityRequest(NewSnowflake(), "Realmless", "Narnia", user));
        var badServer = await client.GetAsync($"{CommunityEndpoints.CommunitiesRoute}/by-discord-server/not-a-snowflake");

        badRealm.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await badRealm.Content.ReadFromJsonAsync<ValidationProblemDetails>()).ShouldNotBeNull().Errors.Keys.ShouldContain(nameof(LinkCommunityRequest.Realm));
        badServer.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await badServer.Content.ReadFromJsonAsync<ValidationProblemDetails>()).ShouldNotBeNull().Errors.Keys.ShouldContain("DiscordGuildId");
    }

    /// <summary>Publishes every community operation in the OpenAPI document.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task OpenApiDocumentDescribesTheCommunityOperations()
    {
        using var client = _api.CreateClient();

        var document = await client.GetStringAsync("/openapi/v1.json");

        document.ShouldContain("\"/internal/communities\"");
        document.ShouldContain("/internal/communities/{communityId}");
        document.ShouldContain("/internal/communities/by-discord-server/{discordGuildId}");
        document.ShouldContain("/internal/users/{userId}/communities");
    }
    #endregion Tests

    #region Private Helpers
    /// <summary>Creates a Discord snowflake no other test uses.</summary>
    /// <returns>A numeric snowflake.</returns>
    private static string NewSnowflake() => Random.Shared.NextInt64(100_000_000_000_000_000, 999_999_999_999_999_999)
        .ToString(System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>Signs a new Discord account in, as the website does, and returns its user id.</summary>
    /// <param name="client">The website client.</param>
    /// <param name="displayName">The display name.</param>
    /// <returns>The user id.</returns>
    private static async Task<Guid> SignInAsync(HttpClient client, string displayName)
    {
        var response = await client.PostAsJsonAsync(IdentityEndpoints.DiscordSignInRoute, new DiscordSignInRequest(NewSnowflake(), displayName, null));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<DiscordSignInResponse>()).ShouldNotBeNull().UserId;
    }

    /// <summary>Creates a client that presents the website key, as the website server does.</summary>
    /// <returns>The HTTP client.</returns>
    private HttpClient WebsiteClient()
    {
        var client = _api.CreateClient();
        client.DefaultRequestHeaders.Add(WebsiteServiceDefaults.HeaderName, ApiFactory.WebsiteServiceKey);
        return client;
    }
    #endregion Private Helpers
}
