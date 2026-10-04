using Pivot.Framework.Application.Abstractions.Messaging.Commands;
using Pivot.Framework.Domain.Repositories;
using Pivot.Framework.Domain.Shared;
using RaidManager.Domain.Features.Companions.Errors;
using RaidManager.Domain.Features.Companions.Repositories;
using RaidManager.Domain.Features.Companions.ValueObjects;
using RaidManager.Domain.Features.Identity.Errors;
using RaidManager.Domain.Features.Identity.Repositories;
using RaidManager.Domain.Features.Shared.Identifiers;

namespace RaidManager.Application.Features.Companions.Commands.ConfirmCompanionPairing;

/// <summary>Handles <see cref="ConfirmCompanionPairingCommand"/>: confirms the pairing that shows the code.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: Keeps the use case thin; the pairing decides whether it can still be confirmed.
/// </remarks>
internal sealed class ConfirmCompanionPairingCommandHandler : ICommandHandler<ConfirmCompanionPairingCommand>
{
    #region Fields
    /// <summary>Stores the pairing repository.</summary>
    private readonly ICompanionPairingRepository _pairings;

    /// <summary>Stores the user repository.</summary>
    private readonly IUserRepository _users;

    /// <summary>Stores the unit of work that commits the confirmation.</summary>
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>Stores the clock.</summary>
    private readonly TimeProvider _timeProvider;
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="ConfirmCompanionPairingCommandHandler"/> class.</summary>
    /// <param name="pairings">The pairing repository.</param>
    /// <param name="users">The user repository.</param>
    /// <param name="unitOfWork">The unit of work that commits the confirmation.</param>
    /// <param name="timeProvider">The clock.</param>
    public ConfirmCompanionPairingCommandHandler(ICompanionPairingRepository pairings, IUserRepository users, IUnitOfWork unitOfWork, TimeProvider timeProvider)
    {
        _pairings = pairings;
        _users = users;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }
    #endregion Constructors

    #region Public Methods
    /// <summary>Confirms the pairing and commits it.</summary>
    /// <param name="request">The command.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// Success; <see cref="IdentityErrors.UserNotFound"/> or <see cref="CompanionErrors.PairingNotFound"/>
    /// (<see cref="ResultExceptionType.NotFound"/>); the pairing's own failure; or the commit failure.
    /// </returns>
    public async Task<Result> Handle(ConfirmCompanionPairingCommand request, CancellationToken cancellationToken)
    {
        var userId = new UserId(request.UserId);
        if (await _users.FindByIdAsync(userId, cancellationToken) is null)
        {
            return Result.Failure(IdentityErrors.UserNotFound, ResultExceptionType.NotFound);
        }

        var pairing = await _pairings.FindLatestUncollectedByCodeAsync(PairingCode.Create(request.PairingCode), cancellationToken);
        if (pairing is null)
        {
            return Result.Failure(CompanionErrors.PairingNotFound, ResultExceptionType.NotFound);
        }

        var confirmed = pairing.Confirm(userId, _timeProvider.GetUtcNow());
        if (confirmed.IsFailure)
        {
            return confirmed;
        }

        await _pairings.UpdateAsync(pairing, cancellationToken);
        return await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
    #endregion Public Methods
}
