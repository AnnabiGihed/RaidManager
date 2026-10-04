using Pivot.Framework.Application.Abstractions.Messaging.Queries;
using Pivot.Framework.Domain.Shared;
using RaidManager.Domain.Features.Companions.Enums;
using RaidManager.Domain.Features.Companions.Errors;
using RaidManager.Domain.Features.Companions.Repositories;
using RaidManager.Domain.Features.Companions.ValueObjects;

namespace RaidManager.Application.Features.Companions.Queries.GetCompanionPairing;

/// <summary>Handles <see cref="GetCompanionPairingQuery"/>: describes the pairing a code belongs to while it can be confirmed.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: Answers the same failures as the confirmation, so the page can say at once that a code expired or was used.
/// </remarks>
internal sealed class GetCompanionPairingQueryHandler : IQueryHandler<GetCompanionPairingQuery, CompanionPairingResponse>
{
    #region Fields
    /// <summary>Stores the pairing repository.</summary>
    private readonly ICompanionPairingRepository _pairings;

    /// <summary>Stores the clock.</summary>
    private readonly TimeProvider _timeProvider;
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="GetCompanionPairingQueryHandler"/> class.</summary>
    /// <param name="pairings">The pairing repository.</param>
    /// <param name="timeProvider">The clock.</param>
    public GetCompanionPairingQueryHandler(ICompanionPairingRepository pairings, TimeProvider timeProvider)
    {
        _pairings = pairings;
        _timeProvider = timeProvider;
    }
    #endregion Constructors

    #region Public Methods
    /// <summary>Describes the pairing.</summary>
    /// <param name="request">The query.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// The pairing; <see cref="CompanionErrors.PairingNotFound"/> (<see cref="ResultExceptionType.NotFound"/>); or
    /// <see cref="CompanionErrors.PairingAlreadyConfirmed"/> or <see cref="CompanionErrors.PairingExpired"/> (<see cref="ResultExceptionType.Conflict"/>).
    /// </returns>
    public async Task<Result<CompanionPairingResponse>> Handle(GetCompanionPairingQuery request, CancellationToken cancellationToken)
    {
        var pairing = await _pairings.FindLatestUncollectedByCodeAsync(PairingCode.Create(request.PairingCode), cancellationToken);
        if (pairing is null)
        {
            return Result.Failure<CompanionPairingResponse>(CompanionErrors.PairingNotFound, ResultExceptionType.NotFound);
        }

        if (pairing.State != CompanionPairingState.Pending)
        {
            return Result.Failure<CompanionPairingResponse>(CompanionErrors.PairingAlreadyConfirmed, ResultExceptionType.Conflict);
        }

        if (pairing.IsExpired(_timeProvider.GetUtcNow()))
        {
            return Result.Failure<CompanionPairingResponse>(CompanionErrors.PairingExpired, ResultExceptionType.Conflict);
        }

        return new CompanionPairingResponse(pairing.Code.ToString(), pairing.ComputerLabel, pairing.RequestedAtUtc, pairing.ExpiresAtUtc);
    }
    #endregion Public Methods
}
