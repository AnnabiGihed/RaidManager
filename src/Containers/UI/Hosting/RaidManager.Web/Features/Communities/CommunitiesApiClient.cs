using System.Net;
using System.Net.Http.Json;
using RaidManager.ViewModels.Features.Communities;

namespace RaidManager.Web.Features.Communities;

/// <summary>Calls the API's community endpoints with the website's service key.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Implements <see cref="ICommunitiesApiClient"/> over the website-only routes; 404 and 409 are answers, other
/// failures throw for the page to show.
/// </remarks>
internal sealed class CommunitiesApiClient : ICommunitiesApiClient
{
    #region Constants
    /// <summary>Defines the route of the communities collection.</summary>
    private const string CommunitiesRoute = "internal/communities";
    #endregion Constants

    #region Fields
    /// <summary>Stores the HTTP client addressed at the API with the service key.</summary>
    private readonly HttpClient _httpClient;
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="CommunitiesApiClient"/> class.</summary>
    /// <param name="httpClient">The HTTP client addressed at the API with the service key.</param>
    public CommunitiesApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }
    #endregion Constructors

    #region Public Methods
    /// <inheritdoc />
    public async Task<IReadOnlyList<CommunitySummary>> GetUserCommunitiesAsync(Guid userId, IReadOnlyCollection<Guid> memberOf, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(memberOf);
        var query = memberOf.Count == 0 ? string.Empty : "?" + string.Join("&", memberOf.Select(id => $"memberOf={id}"));
        return await _httpClient.GetFromJsonAsync<List<CommunitySummary>>($"internal/users/{userId}/communities{query}", cancellationToken)
            ?? throw new HttpRequestException("The API returned an empty communities response.");
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CommunitySummary>> FindByDiscordServersAsync(IReadOnlyCollection<string> discordGuildIds, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PostAsJsonAsync($"{CommunitiesRoute}/by-discord-servers", new DiscordServersRequest(discordGuildIds), cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<CommunitySummary>>(cancellationToken)
            ?? throw new HttpRequestException("The API returned an empty communities response.");
    }

    /// <inheritdoc />
    public Task<CommunitySummary?> GetAsync(Guid communityId, CancellationToken cancellationToken) =>
        FindAsync($"{CommunitiesRoute}/{communityId}", cancellationToken);

    /// <inheritdoc />
    public Task<CommunitySummary?> FindByDiscordServerAsync(string discordGuildId, CancellationToken cancellationToken) =>
        FindAsync($"{CommunitiesRoute}/by-discord-server/{Uri.EscapeDataString(discordGuildId)}", cancellationToken);

    /// <inheritdoc />
    public async Task<Guid?> LinkAsync(PendingCommunityLink link, string realm, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(link);
        using var response = await _httpClient.PostAsJsonAsync(
            CommunitiesRoute,
            new LinkRequest(link.DiscordGuildId, link.ServerName, realm, link.UserId),
            cancellationToken);
        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        var linked = await response.Content.ReadFromJsonAsync<LinkResponse>(cancellationToken)
            ?? throw new HttpRequestException("The API returned an empty link response.");
        return linked.CommunityId;
    }

    /// <inheritdoc />
    public async Task<CommunityRoleSettingsAnswer> GetRoleSettingsAsync(Guid userId, Guid communityId, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync(UserCommunityRoute(userId, communityId) + "/roles", cancellationToken);
        var status = StatusOf(response);
        if (status != CommunityApiStatus.Succeeded)
        {
            return new CommunityRoleSettingsAnswer(status, null);
        }

        var settings = await response.Content.ReadFromJsonAsync<CommunityRoleSettings>(cancellationToken)
            ?? throw new HttpRequestException("The API returned an empty roles response.");
        return new CommunityRoleSettingsAnswer(status, settings);
    }

    /// <inheritdoc />
    public async Task<CommunityMembersAnswer> GetMembersAsync(Guid userId, Guid communityId, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync(UserCommunityRoute(userId, communityId) + "/members", cancellationToken);
        var status = StatusOf(response);
        if (status != CommunityApiStatus.Succeeded)
        {
            return new CommunityMembersAnswer(status, null);
        }

        var members = await response.Content.ReadFromJsonAsync<CommunityMembers>(cancellationToken)
            ?? throw new HttpRequestException("The API returned an empty members response.");
        return new CommunityMembersAnswer(status, members);
    }

    /// <inheritdoc />
    public async Task<CommunityApiStatus> MapRoleAsync(Guid userId, Guid communityId, string discordRoleId, string role, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PutAsync(MappingRoute(userId, communityId, role, discordRoleId), content: null, cancellationToken);
        return StatusOf(response);
    }

    /// <inheritdoc />
    public async Task<CommunityApiStatus> UnmapRoleAsync(Guid userId, Guid communityId, string discordRoleId, string role, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.DeleteAsync(MappingRoute(userId, communityId, role, discordRoleId), cancellationToken);
        return StatusOf(response);
    }
    #endregion Public Methods

    #region Private Helpers
    /// <summary>Builds a user's route to a community.</summary>
    /// <param name="userId">The signed-in user.</param>
    /// <param name="communityId">The community.</param>
    /// <returns>The route.</returns>
    private static string UserCommunityRoute(Guid userId, Guid communityId) => $"internal/users/{userId}/communities/{communityId}";

    /// <summary>Builds the route of a Discord role's mapping to a RaidManager role.</summary>
    /// <param name="userId">The signed-in user.</param>
    /// <param name="communityId">The community.</param>
    /// <param name="role">The RaidManager role's name.</param>
    /// <param name="discordRoleId">The Discord role snowflake.</param>
    /// <returns>The route.</returns>
    private static string MappingRoute(Guid userId, Guid communityId, string role, string discordRoleId) =>
        $"{UserCommunityRoute(userId, communityId)}/role-mappings/{Uri.EscapeDataString(role)}/{Uri.EscapeDataString(discordRoleId)}";

    /// <summary>Reads how the API answered a roles call; an unexpected status throws for the page to show.</summary>
    /// <param name="response">The response.</param>
    /// <returns>The outcome.</returns>
    private static CommunityApiStatus StatusOf(HttpResponseMessage response) => response.StatusCode switch
    {
        HttpStatusCode.Forbidden or HttpStatusCode.BadRequest => CommunityApiStatus.Refused,
        HttpStatusCode.ServiceUnavailable => CommunityApiStatus.DiscordUnavailable,
        HttpStatusCode.Conflict => CommunityApiStatus.BotRemoved,
        _ => response.EnsureSuccessStatusCode().IsSuccessStatusCode ? CommunityApiStatus.Succeeded : CommunityApiStatus.Refused,
    };

    /// <summary>Reads one community, treating 404 as none.</summary>
    /// <param name="route">The API route.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The community, or <see langword="null"/>.</returns>
    private async Task<CommunitySummary?> FindAsync(string route, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync(route, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CommunitySummary>(cancellationToken);
    }
    #endregion Private Helpers

    #region Nested Types
    /// <summary>Writes the API's request for the communities of a user's Discord servers.</summary>
    /// <param name="DiscordGuildIds">The Discord server snowflakes.</param>
    private sealed record DiscordServersRequest(IReadOnlyCollection<string> DiscordGuildIds);

    /// <summary>Writes the API's link request.</summary>
    /// <param name="DiscordGuildId">The Discord server snowflake.</param>
    /// <param name="Name">The server name.</param>
    /// <param name="Realm">The realm's name.</param>
    /// <param name="AdministratorUserId">The user who added the bot.</param>
    private sealed record LinkRequest(string DiscordGuildId, string Name, string Realm, Guid AdministratorUserId);

    /// <summary>Reads the API's link response.</summary>
    /// <param name="CommunityId">The new community's identifier.</param>
    private sealed record LinkResponse(Guid CommunityId);
    #endregion Nested Types
}
