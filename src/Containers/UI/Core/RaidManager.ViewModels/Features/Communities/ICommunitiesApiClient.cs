namespace RaidManager.ViewModels.Features.Communities;

/// <summary>Calls the API's community endpoints for the signed-in user.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Keeps the community view models free of HTTP; the website implements it with the service-key client.
/// </remarks>
public interface ICommunitiesApiClient
{
    #region Methods
    /// <summary>Lists the communities a user belongs to.</summary>
    /// <param name="userId">The signed-in user.</param>
    /// <param name="memberOf">The communities the user's Discord servers matched at sign-in.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The communities the user administers, then the others, each by name; empty when the user has none.</returns>
    Task<IReadOnlyList<CommunitySummary>> GetUserCommunitiesAsync(Guid userId, IReadOnlyCollection<Guid> memberOf, CancellationToken cancellationToken);

    /// <summary>Lists the communities a user's Discord servers link to.</summary>
    /// <param name="discordGuildIds">The Discord server snowflakes.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The linked communities; empty when none of the servers is linked.</returns>
    Task<IReadOnlyList<CommunitySummary>> FindByDiscordServersAsync(IReadOnlyCollection<string> discordGuildIds, CancellationToken cancellationToken);

    /// <summary>Gets a community.</summary>
    /// <param name="communityId">The community.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The community, or <see langword="null"/> when it doesn't exist.</returns>
    Task<CommunitySummary?> GetAsync(Guid communityId, CancellationToken cancellationToken);

    /// <summary>Gets the community a Discord server links to.</summary>
    /// <param name="discordGuildId">The Discord server snowflake.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The community, or <see langword="null"/> when the server isn't linked.</returns>
    Task<CommunitySummary?> FindByDiscordServerAsync(string discordGuildId, CancellationToken cancellationToken);

    /// <summary>Links a Discord server as a community, with the user as its Administrator.</summary>
    /// <param name="link">The server the bot was added to and the user who added it.</param>
    /// <param name="realm">The chosen Warmane realm's name.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The new community's identifier, or <see langword="null"/> when the server was linked in the meantime.</returns>
    Task<Guid?> LinkAsync(PendingCommunityLink link, string realm, CancellationToken cancellationToken);

    /// <summary>Reads a community's officer roles as the signed-in user sees them.</summary>
    /// <param name="userId">The signed-in user.</param>
    /// <param name="communityId">The community.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The roles card, or why it couldn't be read.</returns>
    Task<CommunityRoleSettingsAnswer> GetRoleSettingsAsync(Guid userId, Guid communityId, CancellationToken cancellationToken);

    /// <summary>Lists the people in a community's Discord server with the RaidManager role each one gets.</summary>
    /// <param name="userId">The signed-in user.</param>
    /// <param name="communityId">The community.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The members, or why they couldn't be read.</returns>
    Task<CommunityMembersAnswer> GetMembersAsync(Guid userId, Guid communityId, CancellationToken cancellationToken);

    /// <summary>Maps a Discord role to Officer or Raid leader.</summary>
    /// <param name="userId">The signed-in user; only the Administrator may.</param>
    /// <param name="communityId">The community.</param>
    /// <param name="discordRoleId">The Discord role snowflake.</param>
    /// <param name="roleId">The community role.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>How the API answered.</returns>
    Task<CommunityApiStatus> MapRoleAsync(Guid userId, Guid communityId, string discordRoleId, Guid roleId, CancellationToken cancellationToken);

    /// <summary>Stops a Discord role giving one RaidManager role; any other role it gives stays.</summary>
    /// <param name="userId">The signed-in user; only the Administrator may.</param>
    /// <param name="communityId">The community.</param>
    /// <param name="discordRoleId">The Discord role snowflake.</param>
    /// <param name="roleId">The community role.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>How the API answered.</returns>
    Task<CommunityApiStatus> UnmapRoleAsync(Guid userId, Guid communityId, string discordRoleId, Guid roleId, CancellationToken cancellationToken);

    /// <summary>Creates a role in the community.</summary>
    /// <param name="userId">The signed-in user: the Administrator or a role manager.</param>
    /// <param name="communityId">The community.</param>
    /// <param name="name">The role name.</param>
    /// <param name="permissions">What it allows, by API name.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>How the API answered; <see cref="CommunityApiStatus.NameTaken"/> when another role has the name.</returns>
    Task<CommunityApiStatus> CreateRoleAsync(Guid userId, Guid communityId, string name, IReadOnlyCollection<string> permissions, CancellationToken cancellationToken);

    /// <summary>Renames one of the community's roles and changes what it allows.</summary>
    /// <param name="userId">The signed-in user: the Administrator or a role manager.</param>
    /// <param name="communityId">The community.</param>
    /// <param name="roleId">The role.</param>
    /// <param name="name">The new name.</param>
    /// <param name="permissions">What it allows now, by API name.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>How the API answered; <see cref="CommunityApiStatus.NameTaken"/> when another role has the name.</returns>
    Task<CommunityApiStatus> UpdateRoleAsync(Guid userId, Guid communityId, Guid roleId, string name, IReadOnlyCollection<string> permissions, CancellationToken cancellationToken);

    /// <summary>Deletes one of the community's roles.</summary>
    /// <param name="userId">The signed-in user: the Administrator or a role manager.</param>
    /// <param name="communityId">The community.</param>
    /// <param name="roleId">The role.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>How the API answered.</returns>
    Task<CommunityApiStatus> DeleteRoleAsync(Guid userId, Guid communityId, Guid roleId, CancellationToken cancellationToken);
    #endregion Methods
}
