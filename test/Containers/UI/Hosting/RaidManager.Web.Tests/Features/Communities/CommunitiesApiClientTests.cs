using System.Net;
using Shouldly;
using Xunit;
using RaidManager.ViewModels.Features.Communities;
using RaidManager.Web.Features.Communities;
using RaidManager.Web.Tests.Support;

namespace RaidManager.Web.Tests.Features.Communities;

/// <summary>Verifies how the website reads the API's community answers.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: 404 means no community and 409 means already linked; other failures throw for the page to show.
/// </remarks>
public sealed class CommunitiesApiClientTests : IDisposable
{
    #region Constants
    /// <summary>Defines a community as the API returns it.</summary>
    private const string CommunityJson = """{"communityId":"6f1c3f4e-1d3a-4c55-9a8e-0d3c1b2a4f5e","discordGuildId":"987","name":"Dark Templars","realm":"Icecrown","administratorId":"0b5f3d2c-7a1e-4b8f-9c6d-1e2f3a4b5c6d","administratorName":"Gihed"}""";
    #endregion Constants

    #region Fields
    /// <summary>Stores the handler standing in for the API.</summary>
    private readonly RecordingHandler _api = new();
    #endregion Fields

    #region Tests
    /// <summary>Reads a community, and none for a 404.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task CommunitiesAreReadAndA404IsNone()
    {
        var communityId = Guid.Parse("6f1c3f4e-1d3a-4c55-9a8e-0d3c1b2a4f5e");
        _api.Answer($"/internal/communities/{communityId}", HttpStatusCode.OK, CommunityJson)
            .Answer("/internal/communities/by-discord-server/987", HttpStatusCode.NotFound)
            .Answer("/internal/users/0b5f3d2c-7a1e-4b8f-9c6d-1e2f3a4b5c6d/communities", HttpStatusCode.OK, $"[{CommunityJson}]");
        var client = Client();

        (await client.GetAsync(communityId, CancellationToken.None)).ShouldNotBeNull().Name.ShouldBe("Dark Templars");
        (await client.FindByDiscordServerAsync("987", CancellationToken.None)).ShouldBeNull();
        (await client.GetUserCommunitiesAsync(Guid.Parse("0b5f3d2c-7a1e-4b8f-9c6d-1e2f3a4b5c6d"), CancellationToken.None)).ShouldHaveSingleItem().Realm.ShouldBe("Icecrown");
    }

    /// <summary>Returns the new id after linking, and none when the server is already linked.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task LinkingReturnsTheIdOrNoneForAConflict()
    {
        var link = new PendingCommunityLink("987", "Dark Templars", Guid.NewGuid());
        _api.Answer("/internal/communities", HttpStatusCode.Created, """{"communityId":"6f1c3f4e-1d3a-4c55-9a8e-0d3c1b2a4f5e"}""");

        (await Client().LinkAsync(link, "Icecrown", CancellationToken.None)).ShouldBe(Guid.Parse("6f1c3f4e-1d3a-4c55-9a8e-0d3c1b2a4f5e"));
        _api.Requests.ShouldHaveSingleItem().Body.ShouldBe($$"""{"discordGuildId":"987","name":"Dark Templars","realm":"Icecrown","administratorUserId":"{{link.UserId}}"}""");

        _api.Answer("/internal/communities", HttpStatusCode.Conflict);
        (await Client().LinkAsync(link, "Icecrown", CancellationToken.None)).ShouldBeNull();
    }

    /// <summary>Throws for any other failure, so the page offers a retry.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task OtherFailuresThrow()
    {
        _api.Answer("/internal/communities/by-discord-server/987", HttpStatusCode.InternalServerError)
            .Answer("/internal/communities", HttpStatusCode.BadRequest);

        await Should.ThrowAsync<HttpRequestException>(() => Client().FindByDiscordServerAsync("987", CancellationToken.None));
        await Should.ThrowAsync<HttpRequestException>(() => Client().LinkAsync(new PendingCommunityLink("987", "x", Guid.NewGuid()), "Icecrown", CancellationToken.None));
    }

    /// <summary>Reads the roles card, and turns each refusal and Discord failure into its outcome.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task RoleCallsTurnStatusesIntoOutcomes()
    {
        var user = Guid.Parse("0b5f3d2c-7a1e-4b8f-9c6d-1e2f3a4b5c6d");
        var community = Guid.Parse("6f1c3f4e-1d3a-4c55-9a8e-0d3c1b2a4f5e");
        var route = $"/internal/users/{user}/communities/{community}";
        _api.Answer($"{route}/roles", HttpStatusCode.OK, """{"communityId":"6f1c3f4e-1d3a-4c55-9a8e-0d3c1b2a4f5e","serverName":"Dark Templars","canEdit":true,"mappableRoles":[{"id":"12","name":"Officier"}],"rows":[{"role":"Officer","discordRoles":[{"discordRoleId":"12","name":"Officier","missing":false}],"members":1}]}""")
            .Answer($"{route}/role-mappings/Officer/12", HttpStatusCode.NoContent);
        var client = Client();

        var card = await client.GetRoleSettingsAsync(user, community, CancellationToken.None);
        card.Status.ShouldBe(CommunityApiStatus.Succeeded);
        card.Settings.ShouldNotBeNull().Rows.ShouldHaveSingleItem().DiscordRoles.ShouldHaveSingleItem().Name.ShouldBe("Officier");
        (await client.MapRoleAsync(user, community, "12", "Officer", CancellationToken.None)).ShouldBe(CommunityApiStatus.Succeeded);
        _api.Requests[^1].Request.Method.ShouldBe(HttpMethod.Put);
        (await client.UnmapRoleAsync(user, community, "12", "Officer", CancellationToken.None)).ShouldBe(CommunityApiStatus.Succeeded);
        _api.Requests[^1].Request.Method.ShouldBe(HttpMethod.Delete);

        foreach (var (status, outcome) in new[]
        {
            (HttpStatusCode.Forbidden, CommunityApiStatus.Refused),
            (HttpStatusCode.BadRequest, CommunityApiStatus.Refused),
            (HttpStatusCode.ServiceUnavailable, CommunityApiStatus.DiscordUnavailable),
            (HttpStatusCode.Conflict, CommunityApiStatus.BotRemoved),
        })
        {
            _api.Answer($"{route}/roles", status);
            var refused = await client.GetRoleSettingsAsync(user, community, CancellationToken.None);
            refused.ShouldBe(new CommunityRoleSettingsAnswer(outcome, null));
        }

        _api.Answer($"{route}/members", HttpStatusCode.OK, """{"communityId":"6f1c3f4e-1d3a-4c55-9a8e-0d3c1b2a4f5e","serverName":"Dark Templars","checkedAtUtc":"2026-10-01T18:40:00+00:00","members":[{"discordUserId":"1","displayName":"Malarya","avatarUrl":null,"discordRoles":[],"role":"Officer"}]}""");
        (await client.GetMembersAsync(user, community, CancellationToken.None)).Members.ShouldNotBeNull().Members.ShouldHaveSingleItem().Role.ShouldBe("Officer");
        _api.Answer($"{route}/members", HttpStatusCode.ServiceUnavailable);
        (await client.GetMembersAsync(user, community, CancellationToken.None)).ShouldBe(new CommunityMembersAnswer(CommunityApiStatus.DiscordUnavailable, null));

        _api.Answer($"{route}/roles", HttpStatusCode.InternalServerError);
        await Should.ThrowAsync<HttpRequestException>(() => client.GetRoleSettingsAsync(user, community, CancellationToken.None));
    }
    #endregion Tests

    #region Public Methods
    /// <inheritdoc />
    public void Dispose() => _api.Dispose();
    #endregion Public Methods

    #region Private Helpers
    /// <summary>Creates the client over the stub.</summary>
    /// <returns>The client.</returns>
    private CommunitiesApiClient Client() =>
        new(new HttpClient(_api, disposeHandler: false) { BaseAddress = new Uri("https://api.test/") });
    #endregion Private Helpers
}
