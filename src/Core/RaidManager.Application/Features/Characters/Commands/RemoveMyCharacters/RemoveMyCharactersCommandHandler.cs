using Pivot.Framework.Application.Abstractions.Messaging.Commands;
using Pivot.Framework.Domain.Repositories;
using Pivot.Framework.Domain.Shared;
using RaidManager.Domain.Features.Characters.Repositories;
using RaidManager.Domain.Features.Companions.Enums;
using RaidManager.Domain.Features.Companions.Repositories;
using RaidManager.Domain.Features.Shared.Identifiers;

namespace RaidManager.Application.Features.Characters.Commands.RemoveMyCharacters;

/// <summary>Handles <see cref="RemoveMyCharactersCommand"/>: deletes the player's own characters and withdraws their
/// other claims, then commits once.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-09<br/>
/// Purpose: A character only the player owns or claims is deleted with its claims, loadouts, raid saves and
/// professions, so the next sync imports it again with a pending claim; on a character another player owns or claims
/// too, only the player's claim goes (#613). Each of the player's active companions is asked to send every character
/// again, so they all come back for review without the player logging in with each (#615).
/// </remarks>
internal sealed class RemoveMyCharactersCommandHandler : ICommandHandler<RemoveMyCharactersCommand, int>
{
    #region Fields
    /// <summary>Stores the character repository.</summary>
    private readonly ICharacterRepository _characters;

    /// <summary>Stores the companion repository.</summary>
    private readonly ICompanionRepository _companions;

    /// <summary>Stores the unit of work that commits the removal.</summary>
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>Stores the clock that dates the companions' request.</summary>
    private readonly TimeProvider _timeProvider;
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="RemoveMyCharactersCommandHandler"/> class.</summary>
    /// <param name="characters">The character repository.</param>
    /// <param name="companions">The companion repository.</param>
    /// <param name="unitOfWork">The unit of work.</param>
    /// <param name="timeProvider">The clock.</param>
    public RemoveMyCharactersCommandHandler(ICharacterRepository characters, ICompanionRepository companions, IUnitOfWork unitOfWork, TimeProvider timeProvider)
    {
        _characters = characters;
        _companions = companions;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }
    #endregion Constructors

    #region Public Methods
    /// <summary>Removes the player's characters.</summary>
    /// <param name="request">The command.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The number of characters the player no longer has, or the commit's failure.</returns>
    public async Task<Result<int>> Handle(RemoveMyCharactersCommand request, CancellationToken cancellationToken)
    {
        var userId = new UserId(request.UserId);
        var characters = await _characters.ListOwnedOrClaimedByAsync(userId, cancellationToken);
        foreach (var character in characters)
        {
            if (character.BelongsOnlyTo(userId))
            {
                await _characters.DeleteAsync(character, cancellationToken);
                continue;
            }

            _ = character.WithdrawClaim(userId);
            await _characters.UpdateAsync(character, cancellationToken);
        }

        var nowUtc = _timeProvider.GetUtcNow();
        foreach (var companion in await _companions.ListByUserAsync(userId, cancellationToken))
        {
            if (companion.StatusAt(nowUtc) == CompanionStatus.Active)
            {
                companion.RequestSyncAgain(nowUtc);
                await _companions.UpdateAsync(companion, cancellationToken);
            }
        }

        var saved = await _unitOfWork.SaveChangesAsync(cancellationToken);
        return saved.IsFailure ? Result.Failure<int>(saved.Error, saved.ResultExceptionType) : characters.Count;
    }
    #endregion Public Methods
}
