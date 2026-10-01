using Pivot.Framework.Application.Abstractions.Messaging.Commands;
using Pivot.Framework.Domain.Repositories;
using Pivot.Framework.Domain.Shared;
using RaidManager.Application.Features.Communities.Abstractions;
using RaidManager.Domain.Features.Communities.Errors;
using RaidManager.Domain.Features.Communities.Repositories;
using RaidManager.Domain.Features.Identity.Repositories;
using RaidManager.Domain.Features.Shared.Identifiers;

namespace RaidManager.Application.Features.Communities.Commands.UnmapCommunityRole;

/// <summary>Handles <see cref="UnmapCommunityRoleCommand"/>.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Lets the Administrator stop a Discord role giving a RaidManager role, including a role deleted in Discord since.
/// </remarks>
internal sealed class UnmapCommunityRoleCommandHandler : ICommandHandler<UnmapCommunityRoleCommand>
{
    #region Fields
    /// <summary>Stores the community repository.</summary>
    private readonly ICommunityRepository _communities;

    /// <summary>Stores the user repository.</summary>
    private readonly IUserRepository _users;

    /// <summary>Stores the Discord membership lookup.</summary>
    private readonly IDiscordServerMembers _discordMembers;

    /// <summary>Stores the unit of work that commits the change.</summary>
    private readonly IUnitOfWork _unitOfWork;
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="UnmapCommunityRoleCommandHandler"/> class.</summary>
    /// <param name="communities">The community repository.</param>
    /// <param name="users">The user repository.</param>
    /// <param name="discordMembers">The Discord membership lookup.</param>
    /// <param name="unitOfWork">The unit of work that commits the change.</param>
    public UnmapCommunityRoleCommandHandler(ICommunityRepository communities, IUserRepository users, IDiscordServerMembers discordMembers, IUnitOfWork unitOfWork)
    {
        _communities = communities;
        _users = users;
        _discordMembers = discordMembers;
        _unitOfWork = unitOfWork;
    }
    #endregion Constructors

    #region Public Methods
    /// <summary>Applies the command.</summary>
    /// <param name="request">The command.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>Success, also when the role wasn't mapped, or <see cref="CommunityErrors.NotFound"/>, <see cref="CommunityErrors.NotAdministrator"/>, <see cref="CommunityErrors.NotAMember"/> or Discord's failure.</returns>
    public async Task<Result> Handle(UnmapCommunityRoleCommand request, CancellationToken cancellationToken)
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

        if (community.RoleMappings.All(mapping => mapping.DiscordRoleId != request.DiscordRoleId))
        {
            return Result.Success();
        }

        community.UnmapDiscordRole(request.DiscordRoleId);
        await _communities.UpdateAsync(community, cancellationToken);
        return await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
    #endregion Public Methods
}
