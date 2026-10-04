using Pivot.Framework.Application.Abstractions.Messaging.Commands;
using Pivot.Framework.Domain.Repositories;
using Pivot.Framework.Domain.Shared;
using RaidManager.Application.Features.Companions.Abstractions;
using RaidManager.Domain.Features.Companions.Errors;
using RaidManager.Domain.Features.Companions.Repositories;
using RaidManager.Domain.Features.Companions.ValueObjects;
using RaidManager.Domain.Features.Identity.Errors;
using RaidManager.Domain.Features.Identity.Repositories;

namespace RaidManager.Application.Features.Companions.Commands.CollectCompanionToken;

/// <summary>Handles <see cref="CollectCompanionTokenCommand"/>: pairs the companion once its player confirmed the code.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: Commits the companion and the completed pairing together, so a token is handed out once and never lost.
/// </remarks>
internal sealed class CollectCompanionTokenCommandHandler : ICommandHandler<CollectCompanionTokenCommand, CompanionTokenResponse>
{
    #region Fields
    /// <summary>Stores the pairing repository.</summary>
    private readonly ICompanionPairingRepository _pairings;

    /// <summary>Stores the companion repository.</summary>
    private readonly ICompanionRepository _companions;

    /// <summary>Stores the user repository, for the player's name.</summary>
    private readonly IUserRepository _users;

    /// <summary>Stores the secret generator.</summary>
    private readonly ICompanionCredentialGenerator _credentials;

    /// <summary>Stores the unit of work that commits the pairing.</summary>
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>Stores the clock.</summary>
    private readonly TimeProvider _timeProvider;
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="CollectCompanionTokenCommandHandler"/> class.</summary>
    /// <param name="pairings">The pairing repository.</param>
    /// <param name="companions">The companion repository.</param>
    /// <param name="users">The user repository.</param>
    /// <param name="credentials">The secret generator.</param>
    /// <param name="unitOfWork">The unit of work that commits the pairing.</param>
    /// <param name="timeProvider">The clock.</param>
    public CollectCompanionTokenCommandHandler(
        ICompanionPairingRepository pairings,
        ICompanionRepository companions,
        IUserRepository users,
        ICompanionCredentialGenerator credentials,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _pairings = pairings;
        _companions = companions;
        _users = users;
        _credentials = credentials;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }
    #endregion Constructors

    #region Public Methods
    /// <summary>Pairs the companion and returns its token, or says why not yet.</summary>
    /// <param name="request">The command.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// The token; <see cref="CompanionErrors.PairingInvalid"/> for an unknown device code; the pairing's own failure
    /// (pending, expired, already collected); <see cref="IdentityErrors.UserNotFound"/>; or the commit failure.
    /// </returns>
    public async Task<Result<CompanionTokenResponse>> Handle(CollectCompanionTokenCommand request, CancellationToken cancellationToken)
    {
        var pairing = await _pairings.FindByDeviceCodeHashAsync(CredentialHash.Of(request.DeviceCode), cancellationToken);
        if (pairing is null)
        {
            return Result.Failure<CompanionTokenResponse>(CompanionErrors.PairingInvalid);
        }

        var token = _credentials.NewSecret();
        var paired = pairing.Complete(CredentialHash.Of(token), _timeProvider.GetUtcNow());
        if (paired.IsFailure)
        {
            return Result.Failure<CompanionTokenResponse>(paired.Error, paired.ResultExceptionType);
        }

        var companion = paired.Value;
        var player = await _users.FindByIdAsync(companion.UserId, cancellationToken);
        if (player is null)
        {
            return Result.Failure<CompanionTokenResponse>(IdentityErrors.UserNotFound, ResultExceptionType.NotFound);
        }

        await _companions.AddAsync(companion, cancellationToken);
        await _pairings.UpdateAsync(pairing, cancellationToken);
        var saved = await _unitOfWork.SaveChangesAsync(cancellationToken);
        return saved.IsFailure
            ? Result.Failure<CompanionTokenResponse>(saved.Error, saved.ResultExceptionType)
            : new CompanionTokenResponse(companion.Id.Value, token, player.DisplayName);
    }
    #endregion Public Methods
}
