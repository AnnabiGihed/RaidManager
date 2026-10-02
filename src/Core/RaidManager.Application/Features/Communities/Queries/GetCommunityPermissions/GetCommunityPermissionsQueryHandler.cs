using Pivot.Framework.Application.Abstractions.Messaging.Queries;
using Pivot.Framework.Domain.Shared;
using RaidManager.Application.Features.Communities.Abstractions;
using RaidManager.Domain.Features.Communities.Enums;
using RaidManager.Domain.Features.Communities.Errors;
using RaidManager.Domain.Features.Communities.Repositories;
using RaidManager.Domain.Features.Identity.Repositories;
using RaidManager.Domain.Features.Shared.Identifiers;

namespace RaidManager.Application.Features.Communities.Queries.GetCommunityPermissions;

/// <summary>Handles <see cref="GetCommunityPermissionsQuery"/>: asks Discord for the user's membership and lets the community give the permissions.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Applies ADR-0022: membership and Discord roles come from Discord at check time, and a check Discord can't
/// answer fails closed rather than granting anything.
/// </remarks>
internal sealed class GetCommunityPermissionsQueryHandler : IQueryHandler<GetCommunityPermissionsQuery, CommunityPermissions>
{
    #region Fields
    /// <summary>Stores the community repository.</summary>
    private readonly ICommunityRepository _communities;

    /// <summary>Stores the user repository, which knows each user's Discord account.</summary>
    private readonly IUserRepository _users;

    /// <summary>Stores the Discord membership lookup.</summary>
    private readonly IDiscordServerMembers _discordMembers;
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="GetCommunityPermissionsQueryHandler"/> class.</summary>
    /// <param name="communities">The community repository.</param>
    /// <param name="users">The user repository.</param>
    /// <param name="discordMembers">The Discord membership lookup.</param>
    public GetCommunityPermissionsQueryHandler(ICommunityRepository communities, IUserRepository users, IDiscordServerMembers discordMembers)
    {
        _communities = communities;
        _users = users;
        _discordMembers = discordMembers;
    }
    #endregion Constructors

    #region Public Methods
    /// <summary>Gives what the user may do in the community now.</summary>
    /// <param name="request">The query.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// The permissions; <see cref="CommunityErrors.NotFound"/> for an unknown community; <see cref="CommunityErrors.NotAMember"/>
    /// when the user isn't in the Discord server; or <see cref="DiscordErrors.Unavailable"/> when Discord couldn't answer.
    /// </returns>
    public async Task<Result<CommunityPermissions>> Handle(GetCommunityPermissionsQuery request, CancellationToken cancellationToken)
    {
        var community = await _communities.FindByIdAsync(new CommunityId(request.CommunityId), cancellationToken);
        if (community is null)
        {
            return Result.Failure<CommunityPermissions>(CommunityErrors.NotFound, ResultExceptionType.NotFound);
        }

        // A user RaidManager doesn't know has no Discord account to check, so they can't be in the server.
        var user = await _users.FindByIdAsync(new UserId(request.UserId), cancellationToken);
        if (user is null)
        {
            return Result.Failure<CommunityPermissions>(CommunityErrors.NotAMember, ResultExceptionType.AccessDenied);
        }

        var membership = await _discordMembers.FindAsync(community.DiscordGuildId, user.DiscordUserId.Value, cancellationToken);
        if (membership.IsFailure)
        {
            return Result.Failure<CommunityPermissions>(membership.Error);
        }

        if (!membership.Value.IsMember)
        {
            return Result.Failure<CommunityPermissions>(CommunityErrors.NotAMember, ResultExceptionType.AccessDenied);
        }

        return Result.Success(community.PermissionsFor(user.Id, membership.Value.RoleIds));
    }
    #endregion Public Methods
}
