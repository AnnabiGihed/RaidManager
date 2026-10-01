using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Shouldly;
using Xunit;
using RaidManager.ApiService.Features.Identity;
using RaidManager.ApiService.Features.Shared.Authentication;
using RaidManager.ApiService.Tests.Support;

namespace RaidManager.ApiService.Tests.Features.Identity;

/// <summary>Verifies the website-only Discord sign-in endpoint through HTTP.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-09-30<br/>
/// Purpose: Proves only the website key opens the endpoint, that a Discord account keeps one user, and that bad input is a 400.
/// </remarks>
[Collection(ApiTestGroup.Name)]
public sealed class DiscordSignInEndpointTests
{
    #region Fields
    /// <summary>Stores the API factory.</summary>
    private readonly ApiFactory _api;
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="DiscordSignInEndpointTests"/> class.</summary>
    /// <param name="api">The API factory.</param>
    public DiscordSignInEndpointTests(ApiFactory api)
    {
        _api = api;
    }
    #endregion Constructors

    #region Tests
    /// <summary>Calls the endpoint without the website key.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task RequestWithoutTheWebsiteKeyIsUnauthorized()
    {
        using var client = _api.CreateClient();

        var response = await client.PostAsJsonAsync(IdentityEndpoints.DiscordSignInRoute, Request(NewDiscordId()));

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    /// <summary>Calls the endpoint with a wrong website key.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task RequestWithAWrongWebsiteKeyIsUnauthorized()
    {
        using var client = _api.CreateClient();
        client.DefaultRequestHeaders.Add(WebsiteServiceDefaults.HeaderName, "not-the-website-key-but-long-enough-to-look-real");

        var response = await client.PostAsJsonAsync(IdentityEndpoints.DiscordSignInRoute, Request(NewDiscordId()));

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    /// <summary>Signs the same Discord account in twice through HTTP.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task SigningInTwiceReturnsTheSameUser()
    {
        using var client = WebsiteClient();
        var discordId = NewDiscordId();

        var first = await client.PostAsJsonAsync(IdentityEndpoints.DiscordSignInRoute, Request(discordId));
        var second = await client.PostAsJsonAsync(IdentityEndpoints.DiscordSignInRoute, Request(discordId));

        first.StatusCode.ShouldBe(HttpStatusCode.OK);
        second.StatusCode.ShouldBe(HttpStatusCode.OK);
        var firstUser = (await first.Content.ReadFromJsonAsync<DiscordSignInResponse>()).ShouldNotBeNull();
        var secondUser = (await second.Content.ReadFromJsonAsync<DiscordSignInResponse>()).ShouldNotBeNull();
        secondUser.UserId.ShouldBe(firstUser.UserId);
        firstUser.UserId.ShouldNotBe(Guid.Empty);
    }

    /// <summary>Sends a Discord identifier that is not a snowflake.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task InvalidDiscordIdIsABadRequestWithTheField()
    {
        using var client = WebsiteClient();

        var response = await client.PostAsJsonAsync(IdentityEndpoints.DiscordSignInRoute, Request("not-a-snowflake"));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var problem = (await response.Content.ReadFromJsonAsync<ValidationProblemDetails>()).ShouldNotBeNull();
        problem.Errors.Keys.ShouldContain(nameof(DiscordSignInRequest.DiscordUserId));
    }

    /// <summary>Reads the OpenAPI document and checks the endpoint and its security scheme.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task OpenApiDocumentDescribesTheEndpointAndTheWebsiteKey()
    {
        using var client = _api.CreateClient();

        var document = await client.GetStringAsync("/openapi/v1.json");

        document.ShouldContain(IdentityEndpoints.DiscordSignInRoute);
        document.ShouldContain(WebsiteServiceDefaults.HeaderName);
        document.ShouldContain($"\"{WebsiteServiceDefaults.Scheme}\"");
    }
    #endregion Tests

    #region Private Helpers
    /// <summary>Creates a request body for a Discord account.</summary>
    /// <param name="discordId">The Discord account identifier.</param>
    /// <returns>The request body.</returns>
    private static DiscordSignInRequest Request(string discordId) => new(discordId, "Arthas", null);

    /// <summary>Creates a Discord snowflake no other test uses.</summary>
    /// <returns>A numeric Discord user identifier.</returns>
    private static string NewDiscordId() => Random.Shared.NextInt64(100_000_000_000_000_000, 999_999_999_999_999_999)
        .ToString(System.Globalization.CultureInfo.InvariantCulture);

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
