using Pivot.Framework.Application.Abstractions.Messaging.Commands;
using Pivot.Framework.Domain.Repositories;
using Pivot.Framework.Domain.Shared;
using RaidManager.Application.Features.Companions.Abstractions;
using RaidManager.Domain.Features.Companions.Aggregates;
using RaidManager.Domain.Features.Companions.Repositories;
using RaidManager.Domain.Features.Companions.ValueObjects;

namespace RaidManager.Application.Features.Companions.Commands.StartCompanionPairing;

/// <summary>Handles <see cref="StartCompanionPairingCommand"/>: creates a pending pairing with a fresh device code and code.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: Picks a code no other live pairing shows, so a player can't confirm someone else's computer by coincidence.
/// </remarks>
internal sealed class StartCompanionPairingCommandHandler : ICommandHandler<StartCompanionPairingCommand, StartedCompanionPairingResponse>
{
    #region Fields
    /// <summary>Stores the pairing repository.</summary>
    private readonly ICompanionPairingRepository _pairings;

    /// <summary>Stores the secret and code generator.</summary>
    private readonly ICompanionCredentialGenerator _credentials;

    /// <summary>Stores the unit of work that commits the pairing.</summary>
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>Stores the clock.</summary>
    private readonly TimeProvider _timeProvider;
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="StartCompanionPairingCommandHandler"/> class.</summary>
    /// <param name="pairings">The pairing repository.</param>
    /// <param name="credentials">The secret and code generator.</param>
    /// <param name="unitOfWork">The unit of work that commits the pairing.</param>
    /// <param name="timeProvider">The clock.</param>
    public StartCompanionPairingCommandHandler(
        ICompanionPairingRepository pairings,
        ICompanionCredentialGenerator credentials,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _pairings = pairings;
        _credentials = credentials;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }
    #endregion Constructors

    #region Public Methods
    /// <summary>Creates and commits a pending pairing.</summary>
    /// <param name="request">The command.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The device code and the code to show, or the commit failure.</returns>
    public async Task<Result<StartedCompanionPairingResponse>> Handle(StartCompanionPairingCommand request, CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow();
        var code = await FreeCodeAsync(now, cancellationToken);
        var deviceCode = _credentials.NewSecret();
        var pairing = CompanionPairing.Start(CredentialHash.Of(deviceCode), code, request.ComputerLabel, now);

        await _pairings.AddAsync(pairing, cancellationToken);
        var saved = await _unitOfWork.SaveChangesAsync(cancellationToken);
        return saved.IsFailure
            ? Result.Failure<StartedCompanionPairingResponse>(saved.Error, saved.ResultExceptionType)
            : new StartedCompanionPairingResponse(deviceCode, pairing.Code.ToString(), pairing.ExpiresAtUtc, CompanionPairingDefaults.PollingIntervalSeconds);
    }
    #endregion Public Methods

    #region Private Helpers
    /// <summary>Draws codes until one isn't shown by a live pairing.</summary>
    /// <param name="now">The current time.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A free code.</returns>
    /// <exception cref="InvalidOperationException">Thrown when every attempt drew a code in use, which means the generator is broken.</exception>
    private async Task<PairingCode> FreeCodeAsync(DateTimeOffset now, CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < CompanionPairingDefaults.CodeAttempts; attempt++)
        {
            var code = _credentials.NewPairingCode();
            if (!await _pairings.IsCodeInUseAsync(code, now, cancellationToken))
            {
                return code;
            }
        }

        throw new InvalidOperationException("Every pairing code drawn was already in use.");
    }
    #endregion Private Helpers
}
