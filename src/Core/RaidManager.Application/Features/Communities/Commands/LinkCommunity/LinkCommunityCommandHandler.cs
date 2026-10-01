using Pivot.Framework.Application.Abstractions.Messaging.Commands;
using Pivot.Framework.Domain.Repositories;
using Pivot.Framework.Domain.Shared;
using RaidManager.Domain.Features.Communities.Aggregates;
using RaidManager.Domain.Features.Communities.Errors;
using RaidManager.Domain.Features.Communities.Repositories;
using RaidManager.Domain.Features.Identity.Errors;
using RaidManager.Domain.Features.Identity.Repositories;
using RaidManager.Domain.Features.Shared.Enums;
using RaidManager.Domain.Features.Shared.Identifiers;

namespace RaidManager.Application.Features.Communities.Commands.LinkCommunity;

/// <summary>Handles <see cref="LinkCommunityCommand"/>: links the server once, with its installer as Administrator.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Keeps one community per Discord server: a server that is already linked is refused and left unchanged; the
/// database's unique server id is the last guard against two links at once.
/// </remarks>
internal sealed class LinkCommunityCommandHandler : ICommandHandler<LinkCommunityCommand, Guid>
{
    #region Fields
    /// <summary>Stores the community repository.</summary>
    private readonly ICommunityRepository _communities;

    /// <summary>Stores the user repository.</summary>
    private readonly IUserRepository _users;

    /// <summary>Stores the unit of work that commits the link.</summary>
    private readonly IUnitOfWork _unitOfWork;
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="LinkCommunityCommandHandler"/> class.</summary>
    /// <param name="communities">The community repository.</param>
    /// <param name="users">The user repository.</param>
    /// <param name="unitOfWork">The unit of work that commits the link.</param>
    public LinkCommunityCommandHandler(ICommunityRepository communities, IUserRepository users, IUnitOfWork unitOfWork)
    {
        _communities = communities;
        _users = users;
        _unitOfWork = unitOfWork;
    }
    #endregion Constructors

    #region Public Methods
    /// <summary>Links the server and commits it.</summary>
    /// <param name="request">The command.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// The new community's identifier; <see cref="IdentityErrors.UserNotFound"/> for an unknown user; or
    /// <see cref="CommunityErrors.AlreadyLinked"/> when the server already links to a community.
    /// </returns>
    public async Task<Result<Guid>> Handle(LinkCommunityCommand request, CancellationToken cancellationToken)
    {
        var administrator = await _users.FindByIdAsync(new UserId(request.AdministratorUserId), cancellationToken);
        if (administrator is null)
        {
            return Result.Failure<Guid>(IdentityErrors.UserNotFound, ResultExceptionType.NotFound);
        }

        if (await _communities.IsDiscordGuildLinkedAsync(request.DiscordGuildId, cancellationToken))
        {
            return Result.Failure<Guid>(CommunityErrors.AlreadyLinked, ResultExceptionType.Conflict);
        }

        var community = Community.Link(request.DiscordGuildId, request.Name, Enum.Parse<WarmaneRealm>(request.Realm), administrator.Id);
        await _communities.AddAsync(community, cancellationToken);
        var saved = await _unitOfWork.SaveChangesAsync(cancellationToken);
        return saved.IsFailure ? Result.Failure<Guid>(saved.Error, saved.ResultExceptionType) : community.Id.Value;
    }
    #endregion Public Methods
}
