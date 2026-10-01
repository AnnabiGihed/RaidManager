using Pivot.Framework.Application.Abstractions.Messaging.Commands;
using Pivot.Framework.Domain.Repositories;
using Pivot.Framework.Domain.Shared;
using RaidManager.Domain.Features.Communities.Errors;
using RaidManager.Domain.Features.Communities.Repositories;
using RaidManager.Domain.Features.Shared.Identifiers;

namespace RaidManager.Application.Features.Communities.Commands.RefreshCommunityName;

/// <summary>Handles <see cref="RefreshCommunityNameCommand"/>.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Keeps the stored server name current whenever RaidManager reads the server from Discord (owner decision on #14); an unchanged name saves nothing.
/// </remarks>
internal sealed class RefreshCommunityNameCommandHandler : ICommandHandler<RefreshCommunityNameCommand>
{
    #region Fields
    /// <summary>Stores the community repository.</summary>
    private readonly ICommunityRepository _communities;

    /// <summary>Stores the unit of work that commits the change.</summary>
    private readonly IUnitOfWork _unitOfWork;
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="RefreshCommunityNameCommandHandler"/> class.</summary>
    /// <param name="communities">The community repository.</param>
    /// <param name="unitOfWork">The unit of work that commits the change.</param>
    public RefreshCommunityNameCommandHandler(ICommunityRepository communities, IUnitOfWork unitOfWork)
    {
        _communities = communities;
        _unitOfWork = unitOfWork;
    }
    #endregion Constructors

    #region Public Methods
    /// <summary>Applies the command.</summary>
    /// <param name="request">The command.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>Success, or <see cref="CommunityErrors.NotFound"/>.</returns>
    public async Task<Result> Handle(RefreshCommunityNameCommand request, CancellationToken cancellationToken)
    {
        var community = await _communities.FindByIdAsync(new CommunityId(request.CommunityId), cancellationToken);
        if (community is null)
        {
            return Result.Failure(CommunityErrors.NotFound, ResultExceptionType.NotFound);
        }

        if (!community.Rename(request.Name))
        {
            return Result.Success();
        }

        await _communities.UpdateAsync(community, cancellationToken);
        return await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
    #endregion Public Methods
}
