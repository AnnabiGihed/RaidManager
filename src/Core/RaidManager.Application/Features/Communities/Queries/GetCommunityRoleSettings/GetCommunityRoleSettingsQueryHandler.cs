using Pivot.Framework.Application.Abstractions.Messaging.Queries;
using Pivot.Framework.Domain.Shared;
using RaidManager.Application.Features.Communities.Abstractions;
using RaidManager.Domain.Features.Communities.Aggregates;
using RaidManager.Domain.Features.Communities.Enums;
using RaidManager.Domain.Features.Communities.Errors;
using RaidManager.Domain.Features.Communities.Repositories;
using RaidManager.Domain.Features.Identity.Repositories;
using RaidManager.Domain.Features.Shared.Identifiers;

namespace RaidManager.Application.Features.Communities.Queries.GetCommunityRoleSettings;

/// <summary>Handles <see cref="GetCommunityRoleSettingsQuery"/>: reads the server from Discord and applies the community's mappings.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Lists the Administrator, the community's roles and Member, and counts per row the people who get that
/// role from their Discord roles (one Discord role can give several, owner decision on #289); bots aren't counted.
/// Only a current member of the server may ask.
/// </remarks>
internal sealed class GetCommunityRoleSettingsQueryHandler : IQueryHandler<GetCommunityRoleSettingsQuery, CommunityRoleSettingsResponse>
{
    #region Fields
    /// <summary>Stores the community repository.</summary>
    private readonly ICommunityRepository _communities;

    /// <summary>Stores the user repository.</summary>
    private readonly IUserRepository _users;

    /// <summary>Stores the Discord membership lookup.</summary>
    private readonly IDiscordServerMembers _discordMembers;

    /// <summary>Stores the Discord server reader.</summary>
    private readonly IDiscordServers _discordServers;
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="GetCommunityRoleSettingsQueryHandler"/> class.</summary>
    /// <param name="communities">The community repository.</param>
    /// <param name="users">The user repository.</param>
    /// <param name="discordMembers">The Discord membership lookup.</param>
    /// <param name="discordServers">The Discord server reader.</param>
    public GetCommunityRoleSettingsQueryHandler(
        ICommunityRepository communities,
        IUserRepository users,
        IDiscordServerMembers discordMembers,
        IDiscordServers discordServers)
    {
        _communities = communities;
        _users = users;
        _discordMembers = discordMembers;
        _discordServers = discordServers;
    }
    #endregion Constructors

    #region Public Methods
    /// <summary>Reads the community's officer roles.</summary>
    /// <param name="request">The query.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The roles card, or <see cref="CommunityErrors.NotFound"/>, <see cref="CommunityErrors.NotAMember"/> or Discord's failure.</returns>
    public async Task<Result<CommunityRoleSettingsResponse>> Handle(GetCommunityRoleSettingsQuery request, CancellationToken cancellationToken)
    {
        var community = await _communities.FindByIdAsync(new CommunityId(request.CommunityId), cancellationToken);
        if (community is null)
        {
            return Result.Failure<CommunityRoleSettingsResponse>(CommunityErrors.NotFound, ResultExceptionType.NotFound);
        }

        var user = await _users.FindByIdAsync(new UserId(request.UserId), cancellationToken);
        var access = await CommunityAccess.EnsureMemberAsync(community, user, _discordMembers, cancellationToken);
        if (access.IsFailure)
        {
            return Result.Failure<CommunityRoleSettingsResponse>(access.Error, access.ResultExceptionType);
        }

        var server = await _discordServers.GetAsync(community.DiscordGuildId, cancellationToken);
        if (server.IsFailure)
        {
            return Result.Failure<CommunityRoleSettingsResponse>(server.Error);
        }

        var members = await _discordServers.ListMembersAsync(community.DiscordGuildId, cancellationToken);
        if (members.IsFailure)
        {
            return Result.Failure<CommunityRoleSettingsResponse>(members.Error);
        }

        // Each row counts the people who get that role from their Discord roles; Member counts those who get none.
        var administrator = await _users.FindByIdAsync(community.AdministratorId, cancellationToken);
        var people = members.Value.Where(member => member.UserId != administrator?.DiscordUserId.Value).ToList();
        List<CommunityRoleRowResponse> rows =
        [
            new(CommunityRoleKinds.Administrator, null, CommunityRoleKinds.Administrator, CommunityPermissions.All, [], members.Value.Count - people.Count),
            .. community.Roles.Select(role => new CommunityRoleRowResponse(
                CommunityRoleKinds.Role,
                role.Id.Value,
                role.Name,
                role.Permissions,
                Mapped(community, server.Value, role.Id),
                people.Count(member => Gives(community, member, role.Id)))),
            new(CommunityRoleKinds.Member, null, CommunityRoleKinds.Member, CommunityPermissions.None, [], people.Count(member => community.RolesFor(member.RoleIds).Count == 0)),
        ];

        return Result.Success(new CommunityRoleSettingsResponse(
            community.Id.Value,
            server.Value.Name,
            user!.Id == community.AdministratorId,
            server.Value.Roles,
            rows));
    }
    #endregion Public Methods

    #region Private Helpers
    /// <summary>Tells whether a member's Discord roles give one of the community's roles.</summary>
    /// <param name="community">The community.</param>
    /// <param name="member">The member.</param>
    /// <param name="roleId">The role.</param>
    /// <returns><see langword="true"/> when one of the member's Discord roles is mapped to it.</returns>
    private static bool Gives(Community community, DiscordServerMember member, CommunityRoleId roleId) =>
        community.RoleMappings.Any(mapping => mapping.RoleId == roleId && member.RoleIds.Contains(mapping.DiscordRoleId, StringComparer.Ordinal));

    /// <summary>Lists the Discord roles mapped to one of the community's roles, with their current names.</summary>
    /// <param name="community">The community.</param>
    /// <param name="server">The Discord server.</param>
    /// <param name="roleId">The role.</param>
    /// <returns>The mapped roles; a role deleted in Discord has no name.</returns>
    private static List<MappedDiscordRoleResponse> Mapped(Community community, DiscordServer server, CommunityRoleId roleId) =>
        [.. community.RoleMappings
            .Where(mapping => mapping.RoleId == roleId)
            .Select(mapping => new MappedDiscordRoleResponse(
                mapping.DiscordRoleId,
                server.Roles.FirstOrDefault(discordRole => discordRole.Id == mapping.DiscordRoleId)?.Name))];
    #endregion Private Helpers
}
