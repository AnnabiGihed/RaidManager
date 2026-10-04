using Pivot.Framework.Application.Abstractions.Messaging.Commands;
using Pivot.Framework.Domain.Repositories;
using Pivot.Framework.Domain.Shared;
using RaidManager.Domain.Features.Companions.Errors;
using RaidManager.Domain.Features.Companions.Repositories;
using RaidManager.Domain.Features.Shared.Identifiers;

namespace RaidManager.Application.Features.Companions.Commands.RevokeCompanion;

/// <summary>Handles <see cref="RevokeCompanionCommand"/>: revokes the companion and commits it.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: Keeps the use case thin; the companion decides who may revoke it.
/// </remarks>
internal sealed class RevokeCompanionCommandHandler : ICommandHandler<RevokeCompanionCommand>
{
    #region Fields
    /// <summary>Stores the companion repository.</summary>
    private readonly ICompanionRepository _companions;

    /// <summary>Stores the unit of work that commits the revocation.</summary>
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>Stores the clock.</summary>
    private readonly TimeProvider _timeProvider;
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="RevokeCompanionCommandHandler"/> class.</summary>
    /// <param name="companions">The companion repository.</param>
    /// <param name="unitOfWork">The unit of work that commits the revocation.</param>
    /// <param name="timeProvider">The clock.</param>
    public RevokeCompanionCommandHandler(ICompanionRepository companions, IUnitOfWork unitOfWork, TimeProvider timeProvider)
    {
        _companions = companions;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }
    #endregion Constructors

    #region Public Methods
    /// <summary>Revokes the companion and commits it.</summary>
    /// <param name="request">The command.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// Success; <see cref="CompanionErrors.NotFound"/> (<see cref="ResultExceptionType.NotFound"/>) when the player has no such
    /// companion; <see cref="CompanionErrors.AlreadyRevoked"/>; or the commit failure.
    /// </returns>
    public async Task<Result> Handle(RevokeCompanionCommand request, CancellationToken cancellationToken)
    {
        var companion = await _companions.FindByIdAsync(new CompanionId(request.CompanionId), cancellationToken);
        if (companion is null)
        {
            return Result.Failure(CompanionErrors.NotFound, ResultExceptionType.NotFound);
        }

        var revoked = companion.Revoke(new UserId(request.UserId), _timeProvider.GetUtcNow());
        if (revoked.IsFailure)
        {
            return revoked;
        }

        await _companions.UpdateAsync(companion, cancellationToken);
        return await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
    #endregion Public Methods
}
