using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using Shouldly;
using Xunit;
using RaidManager.ApiService.Features.Communities;
using RaidManager.ApiService.Features.Identity;
using RaidManager.ApiService.Features.Shared.Authentication;
using RaidManager.ApiService.Tests.Support;
using RaidManager.Application.Features.Communities.Abstractions;

namespace RaidManager.ApiService.Tests.Features.Communities;

/// <summary>Verifies the officer roles endpoints against the real API and SQL Server, with Discord faked.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Proves members read the roles card, only the Administrator changes mappings, the stored server name follows
/// Discord, and Discord's failures answer 503 or 409.
/// </remarks>
[Collection(ApiTestGroup.Name)]
public sealed class CommunityRoleEndpointTests
{
    #region Fields
    /// <summary>Stores the API host.</summary>
    private readonly ApiFactory _api;
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="CommunityRoleEndpointTests"/> class.</summary>
    /// <param name="api">The API host.</param>
    public CommunityRoleEndpointTests(ApiFactory api)
    {
        _api = api;
    }
    #endregion Constructors

    #region Tests
    /// <summary>Maps a role as the Administrator, reads the card with its counts, and refreshes the stored name.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task AdministratorMapsARoleAndReadsTheCard()
    {
        using var client = WebsiteClient();
        var server = await LinkedServerAsync(client);

        var map = await client.PutAsync($"{server.Route(server.Administrator)}/role-mappings/{server.Officer}/{server.OfficerRoleId}", content: null);
        var secondRole = await client.PutAsync($"{server.Route(server.Administrator)}/role-mappings/{server.RaidLeader}/{server.OfficerRoleId}", content: null);
        var card = await client.GetFromJsonAsync<CommunityRoleSettings>($"{server.Route(server.Administrator)}/roles");

        map.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        secondRole.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        card.ShouldNotBeNull();
        card.CanEdit.ShouldBeTrue();
        card.ServerName.ShouldBe("Dark Templars Reborn");
        card.MappableRoles.Select(role => role.Name).ShouldBe(["Guild Master", "Officier"]);
        card.Rows.Select(row => (row.Kind, row.Name, row.Members)).ShouldBe([("Administrator", "Administrator", 1), ("Role", "Officer", 1), ("Role", "Raid leader", 1), ("Member", "Member", 1)]);
        card.Rows.Select(row => row.RoleId).ShouldBe([null, server.Officer, server.RaidLeader, null]);
        card.Rows[1].Permissions.ShouldBe(["ManageRaids", "BuildRosters", "RunRaidNight", "ReviewConflicts"]);
        card.Rows[2].Permissions.ShouldBe(["ManageRaids", "BuildRosters", "RunRaidNight"]);
        card.Rows[0].Permissions.Count.ShouldBe(5);
        card.Rows[3].Permissions.ShouldBeEmpty();
        card.Rows[2].DiscordRoles.ShouldHaveSingleItem().DiscordRoleId.ShouldBe(server.OfficerRoleId);
        card.Rows[1].DiscordRoles.ShouldHaveSingleItem().ShouldBe(new MappedDiscordRole(server.OfficerRoleId, "Officier", false));
        (await client.GetFromJsonAsync<CommunitySummary>($"{CommunityEndpoints.CommunitiesRoute}/{server.CommunityId}")).ShouldNotBeNull().Name.ShouldBe("Dark Templars Reborn");
    }

    /// <summary>Lists the server's people with their roles for a member, and keeps someone outside the server out.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task MembersAreListedWithTheirRoles()
    {
        using var client = WebsiteClient();
        var server = await LinkedServerAsync(client);
        (await client.PutAsync($"{server.Route(server.Administrator)}/role-mappings/{server.Officer}/{server.OfficerRoleId}", content: null)).EnsureSuccessStatusCode();

        var members = await client.GetFromJsonAsync<CommunityMembers>($"{server.Route(server.Member)}/members");
        var outsider = await client.GetAsync($"{server.Route(server.Outsider)}/members");

        members.ShouldNotBeNull();
        members.ServerName.ShouldBe("Dark Templars Reborn");
        members.Members.Select(member => (member.DisplayName, string.Join(", ", member.Roles))).ShouldBe([("Gihed", "Administrator"), ("Malarya", "Officer"), ("Daymox", "Member")]);
        members.Members[1].DiscordRoles.ShouldHaveSingleItem().Name.ShouldBe("Officier");
        outsider.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    /// <summary>Lets another member read the card but not change it, and keeps someone outside the server out.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task OnlyTheAdministratorEditsAndOnlyMembersRead()
    {
        using var client = WebsiteClient();
        var server = await LinkedServerAsync(client);

        var memberCard = await client.GetFromJsonAsync<CommunityRoleSettings>($"{server.Route(server.Member)}/roles");
        var memberMap = await client.PutAsync($"{server.Route(server.Member)}/role-mappings/{server.Officer}/{server.OfficerRoleId}", content: null);
        var memberUnmap = await client.DeleteAsync($"{server.Route(server.Member)}/role-mappings/{server.Officer}/{server.OfficerRoleId}");
        var outsiderCard = await client.GetAsync($"{server.Route(server.Outsider)}/roles");

        memberCard.ShouldNotBeNull().CanEdit.ShouldBeFalse();
        memberMap.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        memberUnmap.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        outsiderCard.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    /// <summary>Removes a mapping, and refuses a role the server can't map or a role the community doesn't have.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task MappingsAreRemovedAndInvalidOnesRefused()
    {
        using var client = WebsiteClient();
        var server = await LinkedServerAsync(client);
        var route = server.Route(server.Administrator);
        (await client.PutAsync($"{route}/role-mappings/{server.RaidLeader}/{server.OfficerRoleId}", content: null)).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var unmap = await client.DeleteAsync($"{route}/role-mappings/{server.RaidLeader}/{server.OfficerRoleId}");
        var unknownRole = await client.PutAsync($"{route}/role-mappings/{server.Officer}/999", content: null);
        var unknownRaidManagerRole = await client.PutAsync($"{route}/role-mappings/{Guid.NewGuid()}/{server.OfficerRoleId}", content: null);

        unmap.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await client.GetFromJsonAsync<CommunityRoleSettings>($"{route}/roles")).ShouldNotBeNull().Rows.ShouldAllBe(row => row.DiscordRoles.Count == 0);
        unknownRole.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        unknownRaidManagerRole.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    /// <summary>Answers 503 when Discord can't answer and 409 when the bot was removed from the server.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task DiscordFailuresAreReported()
    {
        using var client = WebsiteClient();
        var server = await LinkedServerAsync(client);
        var route = server.Route(server.Administrator);

        _api.Discord.Failure = DiscordErrors.Unavailable;
        try
        {
            (await client.GetAsync($"{route}/roles")).StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
            (await client.PutAsync($"{route}/role-mappings/{server.Officer}/{server.OfficerRoleId}", content: null)).StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        }
        finally
        {
            _api.Discord.Failure = null;
        }

        _api.Discord.BotRemovedFrom.Add(server.GuildId);
        (await client.GetAsync($"{route}/roles")).StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    /// <summary>Publishes the officer roles operations in the OpenAPI document.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task OpenApiDocumentDescribesTheRoleOperations()
    {
        using var client = _api.CreateClient();

        var document = await client.GetStringAsync("/openapi/v1.json");

        document.ShouldContain("/internal/users/{userId}/communities/{communityId}/roles");
        document.ShouldContain("/internal/users/{userId}/communities/{communityId}/members");
        document.ShouldContain("/internal/users/{userId}/communities/{communityId}/role-mappings/{roleId}/{discordRoleId}");
    }
    #endregion Tests

    #region Private Helpers
    /// <summary>Creates a Discord snowflake no other test uses.</summary>
    /// <returns>A numeric snowflake.</returns>
    private static string NewSnowflake() => Random.Shared.NextInt64(100_000_000_000_000_000, 999_999_999_999_999_999).ToString(CultureInfo.InvariantCulture);

    /// <summary>Signs a Discord account in, as the website does.</summary>
    /// <param name="client">The website client.</param>
    /// <param name="discordUserId">The Discord account.</param>
    /// <param name="name">The display name.</param>
    /// <returns>The RaidManager user id.</returns>
    private static async Task<Guid> SignInAsync(HttpClient client, string discordUserId, string name)
    {
        var response = await client.PostAsJsonAsync(IdentityEndpoints.DiscordSignInRoute, new DiscordSignInRequest(discordUserId, name, null));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<DiscordSignInResponse>()).ShouldNotBeNull().UserId;
    }

    /// <summary>Links a server with an Administrator, a member and an outsider, puts it in the fake Discord, and reads its preset roles.</summary>
    /// <param name="client">The website client.</param>
    /// <returns>The server's ids.</returns>
    private async Task<LinkedServer> LinkedServerAsync(HttpClient client)
    {
        var guildId = NewSnowflake();
        var (adminDiscord, memberDiscord, outsiderDiscord) = (NewSnowflake(), NewSnowflake(), NewSnowflake());
        var (guildMaster, officier) = (NewSnowflake(), NewSnowflake());
        var administrator = await SignInAsync(client, adminDiscord, "Gihed");
        var member = await SignInAsync(client, memberDiscord, "Malarya");
        var outsider = await SignInAsync(client, outsiderDiscord, "Stranger");
        var linked = await client.PostAsJsonAsync(CommunityEndpoints.CommunitiesRoute, new LinkCommunityRequest(guildId, "Dark Templars", "Icecrown", administrator));
        var communityId = (await linked.Content.ReadFromJsonAsync<LinkCommunityResponse>()).ShouldNotBeNull().CommunityId;
        _api.Discord.Servers[guildId] = (
            new DiscordServer("Dark Templars Reborn", [new DiscordServerRole(guildMaster, "Guild Master", 10), new DiscordServerRole(officier, "Officier", 8)]),
            [new DiscordServerMember(adminDiscord, "Gihed", [guildMaster]), new DiscordServerMember(memberDiscord, "Malarya", [officier]), new DiscordServerMember(NewSnowflake(), "Daymox", [])]);
        var card = (await client.GetFromJsonAsync<CommunityRoleSettings>($"/internal/users/{administrator}/communities/{communityId}/roles")).ShouldNotBeNull();
        Guid RoleNamed(string name) => card.Rows.Single(row => row.Name == name).RoleId.ShouldNotBeNull();
        return new LinkedServer(guildId, communityId, administrator, member, outsider, officier, RoleNamed("Officer"), RoleNamed("Raid leader"));
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

    #region Nested Types
    /// <summary>Holds the ids of a linked server in a test.</summary>
    /// <param name="GuildId">The Discord server.</param>
    /// <param name="CommunityId">The community.</param>
    /// <param name="Administrator">The Administrator's user id.</param>
    /// <param name="Member">A member's user id.</param>
    /// <param name="Outsider">The user id of someone outside the server.</param>
    /// <param name="OfficerRoleId">The Discord role named Officier.</param>
    /// <param name="Officer">The community's Officer preset.</param>
    /// <param name="RaidLeader">The community's Raid leader preset.</param>
    private sealed record LinkedServer(string GuildId, Guid CommunityId, Guid Administrator, Guid Member, Guid Outsider, string OfficerRoleId, Guid Officer, Guid RaidLeader)
    {
        /// <summary>Builds a user's route to the community.</summary>
        /// <param name="userId">The user.</param>
        /// <returns>The route.</returns>
        public string Route(Guid userId) => $"/internal/users/{userId}/communities/{CommunityId}";
    }
    #endregion Nested Types
}
