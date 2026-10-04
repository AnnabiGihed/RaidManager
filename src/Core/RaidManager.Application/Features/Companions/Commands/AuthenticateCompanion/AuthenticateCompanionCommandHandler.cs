using Pivot.Framework.Application.Abstractions.Messaging.Commands;
using Pivot.Framework.Domain.Repositories;
using Pivot.Framework.Domain.Shared;
using RaidManager.Application.Features.Companions.Abstractions;
using RaidManager.Domain.Features.Companions.Errors;
using RaidManager.Domain.Features.Companions.Repositories;
using RaidManager.Domain.Features.Companions.ValueObjects;

namespace RaidManager.Application.Features.Companions.Commands.AuthenticateCompanion;

/// <summary>Handles <see cref="AuthenticateCompanionCommand"/>: finds the companion by its token's hash and admits it if active.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: Writes last use only when it moved by an hour or more, so most requests read and never write.
/// </remarks>
internal sealed class AuthenticateCompanionCommandHandler : ICommandHandler<AuthenticateCompanionCommand, AuthenticatedCompanionResponse>
{
    #region Fields
    /// <summary>Stores the companion repository.</summary>
    private readonly ICompanionRepository _companions;

    /// <summary>Stores the unit of work that commits last use.</summary>
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>Stores the clock.</summary>
    private readonly TimeProvider _timeProvider;
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="AuthenticateCompanionCommandHandler"/> class.</summary>
    /// <param name="companions">The companion repository.</param>
    /// <param name="unitOfWork">The unit of work that commits last use.</param>
    /// <param name="timeProvider">The clock.</param>
    public AuthenticateCompanionCommandHandler(ICompanionRepository companions, IUnitOfWork unitOfWork, TimeProvider timeProvider)
    {
        _companions = companions;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }
    #endregion Constructors

    #region Public Methods
    /// <summary>Checks the token.</summary>
    /// <param name="request">The command.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// The companion; or <see cref="CompanionErrors.TokenUnknown"/>, <see cref="CompanionErrors.Revoked"/> or
    /// <see cref="CompanionErrors.Expired"/> (<see cref="ResultExceptionType.AuthenticationRequired"/>); or the commit failure.
    /// </returns>
    public async Task<Result<AuthenticatedCompanionResponse>> Handle(AuthenticateCompanionCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(request.DeviceToken) || request.DeviceToken.Length > CompanionPairingDefaults.MaximumSecretLength)
        {
            return Result.Failure<AuthenticatedCompanionResponse>(CompanionErrors.TokenUnknown, ResultExceptionType.AuthenticationRequired);
        }

        var companion = await _companions.FindByTokenHashAsync(CredentialHash.Of(request.DeviceToken), cancellationToken);
        if (companion is null)
        {
            return Result.Failure<AuthenticatedCompanionResponse>(CompanionErrors.TokenUnknown, ResultExceptionType.AuthenticationRequired);
        }

        var used = companion.Use(_timeProvider.GetUtcNow());
        if (used.IsFailure)
        {
            return Result.Failure<AuthenticatedCompanionResponse>(used.Error, used.ResultExceptionType);
        }

        if (used.Value)
        {
            await _companions.UpdateAsync(companion, cancellationToken);
            var saved = await _unitOfWork.SaveChangesAsync(cancellationToken);
            if (saved.IsFailure)
            {
                return Result.Failure<AuthenticatedCompanionResponse>(saved.Error, saved.ResultExceptionType);
            }
        }

        return new AuthenticatedCompanionResponse(companion.Id.Value, companion.UserId.Value, companion.Label);
    }
    #endregion Public Methods
}
