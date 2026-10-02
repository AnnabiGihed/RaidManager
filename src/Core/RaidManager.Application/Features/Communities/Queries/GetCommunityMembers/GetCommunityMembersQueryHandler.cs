using Pivot.Framework.Application.Abstractions.Messaging.Queries;
using Pivot.Framework.Domain.Shared;
using RaidManager.Application.Features.Communities.Abstractions;
using RaidManager.Application.Features.Communities.Queries.GetCommunityRoleSettings;
using RaidManager.Domain.Features.Communities.Aggregates;
using RaidManager.Domain.Features.Communities.Errors;
using RaidManager.Domain.Features.Communities.Repositories;
using RaidManager.Domain.Features.Identity.Repositories;
using RaidManager.Domain.Features.Shared.Identifiers;

namespace RaidManager.Application.Features.Communities.Queries.GetCommunityMembers;

/// <summary>Handles <see cref="GetCommunityMembersQuery"/>: reads the server's people from Discord and gives each their role.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Gives each person the role the role check gives them; bots aren't listed. Only a current member of the server may ask, and Discord not answering fails rather than showing an old list.
/// </remarks>
internal sealed class GetCommunityMembersQueryHandler : IQueryHandler<GetCommunityMembersQuery, CommunityMembersResponse>
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

    /// <summary>Stores the clock that stamps when Discord was asked.</summary>
    private readonly TimeProvider _timeProvider;
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="GetCommunityMembersQueryHandler"/> class.</summary>
    /// <param name="communities">The community repository.</param>
    /// <param name="users">The user repository.</param>
    /// <param name="discordMembers">The Discord membership lookup.</param>
    /// <param name="discordServers">The Discord server reader.</param>
    /// <param name="timeProvider">The clock that stamps when Discord was asked.</param>
    public GetCommunityMembersQueryHandler(
        ICommunityRepository communities,
        IUserRepository users,
        IDiscordServerMembers discordMembers,
        IDiscordServers discordServers,
        TimeProvider timeProvider)
    {
        _communities = communities;
        _users = users;
        _discordMembers = discordMembers;
        _discordServers = discordServers;
        _timeProvider = timeProvider;
    }
    #endregion Constructors

    #region Public Methods
    /// <summary>Lists the server's people with their roles.</summary>
    /// <param name="request">The query.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The members, or <see cref="CommunityErrors.NotFound"/>, <see cref="CommunityErrors.NotAMember"/> or Discord's failure.</returns>
    public async Task<Result<CommunityMembersResponse>> Handle(GetCommunityMembersQuery request, CancellationToken cancellationToken)
    {
        var community = await _communities.FindByIdAsync(new CommunityId(request.CommunityId), cancellationToken);
        if (community is null)
        {
            return Result.Failure<CommunityMembersResponse>(CommunityErrors.NotFound, ResultExceptionType.NotFound);
        }

        var access = await CommunityAccess.EnsureMemberAsync(community, await _users.FindByIdAsync(new UserId(request.UserId), cancellationToken), _discordMembers, cancellationToken);
        if (access.IsFailure)
        {
            return Result.Failure<CommunityMembersResponse>(access.Error, access.ResultExceptionType);
        }

        var server = await _discordServers.GetAsync(community.DiscordGuildId, cancellationToken);
        if (server.IsFailure)
        {
            return Result.Failure<CommunityMembersResponse>(server.Error);
        }

        var members = await _discordServers.ListMembersAsync(community.DiscordGuildId, cancellationToken);
        if (members.IsFailure)
        {
            return Result.Failure<CommunityMembersResponse>(members.Error);
        }

        // The Administrator first, then people by their first role in the list, then Members; each group by name.
        var administrator = await _users.FindByIdAsync(community.AdministratorId, cancellationToken);
        var rows = members.Value
            .Select(member => (Member: member, Administrator: member.UserId == administrator?.DiscordUserId.Value, Roles: community.RolesFor(member.RoleIds)))
            .OrderBy(entry => entry.Administrator ? int.MinValue : entry.Roles.Count > 0 ? entry.Roles[0].Position : int.MaxValue)
            .ThenBy(entry => entry.Member.DisplayName, StringComparer.CurrentCultureIgnoreCase)
            .Select(entry => new CommunityMemberResponse(
                entry.Member.UserId,
                entry.Member.DisplayName,
                entry.Member.AvatarUrl,
                [.. server.Value.Roles.Where(role => entry.Member.RoleIds.Contains(role.Id, StringComparer.Ordinal))],
                RoleNames(entry.Administrator, entry.Roles)))
            .ToList();
        return Result.Success(new CommunityMembersResponse(community.Id.Value, server.Value.Name, _timeProvider.GetUtcNow(), rows));
    }
    #endregion Public Methods

    #region Private Helpers
    /// <summary>Names the roles a person has.</summary>
    /// <param name="administrator">Whether the person is the Administrator.</param>
    /// <param name="roles">The roles their Discord roles give, in list order.</param>
    /// <returns>Administrator alone for the Administrator, otherwise the roles' names, otherwise Member.</returns>
    private static List<string> RoleNames(bool administrator, IReadOnlyList<CommunityRole> roles)
    {
        if (administrator)
        {
            return [CommunityRoleKinds.Administrator];
        }

        return roles.Count > 0 ? [.. roles.Select(role => role.Name)] : [CommunityRoleKinds.Member];
    }
    #endregion Private Helpers
}
