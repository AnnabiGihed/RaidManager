using Pivot.Framework.Application.Abstractions.Messaging.Commands;
using Pivot.Framework.Domain.Repositories;
using Pivot.Framework.Domain.Shared;
using RaidManager.Application.Features.Communities.Abstractions;
using RaidManager.Domain.Features.Communities.Errors;
using RaidManager.Domain.Features.Communities.Repositories;
using RaidManager.Domain.Features.Identity.Repositories;
using RaidManager.Domain.Features.Shared.Identifiers;

namespace RaidManager.Application.Features.Communities.Commands.MapCommunityRole;

/// <summary>Handles <see cref="MapCommunityRoleCommand"/>.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Lets the Administrator choose which Discord roles give RaidManager permissions (board 4); the role must be one of the server's mappable roles now.
/// </remarks>
internal sealed class MapCommunityRoleCommandHandler : ICommandHandler<MapCommunityRoleCommand>
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

    /// <summary>Stores the unit of work that commits the change.</summary>
    private readonly IUnitOfWork _unitOfWork;
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="MapCommunityRoleCommandHandler"/> class.</summary>
    /// <param name="communities">The community repository.</param>
    /// <param name="users">The user repository.</param>
    /// <param name="discordMembers">The Discord membership lookup.</param>
    /// <param name="discordServers">The Discord server reader.</param>
    /// <param name="unitOfWork">The unit of work that commits the change.</param>
    public MapCommunityRoleCommandHandler(ICommunityRepository communities, IUserRepository users, IDiscordServerMembers discordMembers, IDiscordServers discordServers, IUnitOfWork unitOfWork)
    {
        _communities = communities;
        _users = users;
        _discordMembers = discordMembers;
        _discordServers = discordServers;
        _unitOfWork = unitOfWork;
    }
    #endregion Constructors

    #region Public Methods
    /// <summary>Applies the command.</summary>
    /// <param name="request">The command.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>Success, or <see cref="CommunityErrors.NotFound"/>, <see cref="CommunityErrors.NotAdministrator"/>, <see cref="CommunityErrors.NotAMember"/>, <see cref="CommunityErrors.RoleNotMappable"/> or Discord's failure.</returns>
    public async Task<Result> Handle(MapCommunityRoleCommand request, CancellationToken cancellationToken)
    {
        var community = await _communities.FindByIdAsync(new CommunityId(request.CommunityId), cancellationToken);
        if (community is null)
        {
            return Result.Failure(CommunityErrors.NotFound, ResultExceptionType.NotFound);
        }

        var access = await CommunityAccess.EnsureAdministratorAsync(
            community,
            await _users.FindByIdAsync(new UserId(request.UserId), cancellationToken),
            _discordMembers,
            cancellationToken);
        if (access.IsFailure)
        {
            return access;
        }

        if (community.FindRole(new CommunityRoleId(request.RoleId)) is null)
        {
            return Result.Failure(CommunityErrors.RoleNotFound, ResultExceptionType.NotFound);
        }

        var server = await _discordServers.GetAsync(community.DiscordGuildId, cancellationToken);
        if (server.IsFailure)
        {
            return Result.Failure(server.Error);
        }

        if (!server.Value.Roles.Any(role => role.Id == request.DiscordRoleId))
        {
            return Result.Failure(CommunityErrors.RoleNotMappable);
        }

        community.MapDiscordRole(request.DiscordRoleId, new CommunityRoleId(request.RoleId));
        await _communities.UpdateAsync(community, cancellationToken);
        return await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
    #endregion Public Methods
}
