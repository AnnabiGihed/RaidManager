using Pivot.Framework.Application.Abstractions.Messaging.Commands;
using Pivot.Framework.Domain.Repositories;
using Pivot.Framework.Domain.Shared;
using RaidManager.Domain.Features.Characters.Errors;
using RaidManager.Domain.Features.Characters.Repositories;
using RaidManager.Domain.Features.Shared.Identifiers;

namespace RaidManager.Application.Features.Characters.Commands.ApproveCharacterClaim;

/// <summary>Handles <see cref="ApproveCharacterClaimCommand"/>: loads the character, applies the player's decision, and commits it.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-09-30<br/>
/// Purpose: Keeps the use case thin; the Character aggregate decides whether the claim may be approved.
/// </remarks>
internal sealed class ApproveCharacterClaimCommandHandler : ICommandHandler<ApproveCharacterClaimCommand>
{
    #region Fields
    /// <summary>Stores the character repository.</summary>
    private readonly ICharacterRepository _characters;

    /// <summary>Stores the unit of work that commits the decision.</summary>
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>Stores the clock that timestamps the decision.</summary>
    private readonly TimeProvider _timeProvider;
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="ApproveCharacterClaimCommandHandler"/> class.</summary>
    /// <param name="characters">The character repository.</param>
    /// <param name="unitOfWork">The unit of work that commits the decision.</param>
    /// <param name="timeProvider">The clock that timestamps the decision.</param>
    public ApproveCharacterClaimCommandHandler(ICharacterRepository characters, IUnitOfWork unitOfWork, TimeProvider timeProvider)
    {
        _characters = characters;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }
    #endregion Constructors

    #region Public Methods
    /// <summary>Applies the player's decision to their claim and commits it.</summary>
    /// <param name="request">The command.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// Success, <see cref="CharacterErrors.NotFound"/> when the character does not exist, or the aggregate's claim failure with its
    /// <see cref="ResultExceptionType"/>.
    /// </returns>
    public async Task<Result> Handle(ApproveCharacterClaimCommand request, CancellationToken cancellationToken)
    {
        var character = await _characters.FindByIdAsync(new CharacterId(request.CharacterId), cancellationToken);
        if (character is null)
        {
            return Result.Failure(CharacterErrors.NotFound, ResultExceptionType.NotFound);
        }

        var decision = character.ApproveClaim(new UserId(request.UserId), _timeProvider.GetUtcNow());

        // A claim on a character another player owns fails but moves to conflict review, and that change must be kept.
        if (decision.IsFailure && decision.Error != CharacterErrors.OwnedByAnotherUser)
        {
            return decision;
        }

        await _characters.UpdateAsync(character, cancellationToken);
        var saved = await _unitOfWork.SaveChangesAsync(cancellationToken);
        return saved.IsFailure ? saved : decision;
    }
    #endregion Public Methods
}
