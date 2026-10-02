using Pivot.Framework.Application.Abstractions.Messaging.Commands;
using Pivot.Framework.Domain.Repositories;
using Pivot.Framework.Domain.Shared;
using RaidManager.Application.Features.Communities.Abstractions;
using RaidManager.Domain.Features.Communities.Errors;
using RaidManager.Domain.Features.Communities.Repositories;
using RaidManager.Domain.Features.Identity.Repositories;
using RaidManager.Domain.Features.Shared.Identifiers;

namespace RaidManager.Application.Features.Communities.Commands.DeleteCommunityRole;

/// <summary>Handles <see cref="DeleteCommunityRoleCommand"/>: checks with Discord who asks, then lets the community delete the role.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-02<br/>
/// Purpose: A role manager deletes every role except one that manages roles (owner decision on #308).
/// </remarks>
internal sealed class DeleteCommunityRoleCommandHandler : ICommandHandler<DeleteCommunityRoleCommand>
{
    #region Fields
    /// <summary>Stores the community repository.</summary>
    private readonly ICommunityRepository _communities;

    /// <summary>Stores the user repository.</summary>
    private readonly IUserRepository _users;

    /// <summary>Stores the Discord membership lookup.</summary>
    private readonly IDiscordServerMembers _discordMembers;

    /// <summary>Stores the unit of work.</summary>
    private readonly IUnitOfWork _unitOfWork;
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="DeleteCommunityRoleCommandHandler"/> class.</summary>
    /// <param name="communities">The community repository.</param>
    /// <param name="users">The user repository.</param>
    /// <param name="discordMembers">The Discord membership lookup.</param>
    /// <param name="unitOfWork">The unit of work.</param>
    public DeleteCommunityRoleCommandHandler(ICommunityRepository communities, IUserRepository users, IDiscordServerMembers discordMembers, IUnitOfWork unitOfWork)
    {
        _communities = communities;
        _users = users;
        _discordMembers = discordMembers;
        _unitOfWork = unitOfWork;
    }
    #endregion Constructors

    #region Public Methods
    /// <summary>Deletes the role.</summary>
    /// <param name="request">The command.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>Success, or the failure.</returns>
    public async Task<Result> Handle(DeleteCommunityRoleCommand request, CancellationToken cancellationToken)
    {
        var community = await _communities.FindByIdAsync(new CommunityId(request.CommunityId), cancellationToken);
        if (community is null)
        {
            return Result.Failure(CommunityErrors.NotFound, ResultExceptionType.NotFound);
        }

        var access = await CommunityAccess.EnsureRoleManagerAsync(
            community,
            await _users.FindByIdAsync(new UserId(request.UserId), cancellationToken),
            _discordMembers,
            cancellationToken);
        if (access.IsFailure)
        {
            return Result.Failure(access.Error, access.ResultExceptionType);
        }

        var deleted = community.DeleteRole(new CommunityRoleId(request.RoleId), access.Value);
        if (deleted.IsFailure)
        {
            return deleted;
        }

        await _communities.UpdateAsync(community, cancellationToken);
        return await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
    #endregion Public Methods
}
