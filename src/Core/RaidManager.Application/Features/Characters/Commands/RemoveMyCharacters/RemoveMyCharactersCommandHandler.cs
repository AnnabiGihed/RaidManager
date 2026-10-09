using Pivot.Framework.Application.Abstractions.Messaging.Commands;
using Pivot.Framework.Domain.Repositories;
using Pivot.Framework.Domain.Shared;
using RaidManager.Domain.Features.Characters.Repositories;
using RaidManager.Domain.Features.Shared.Identifiers;

namespace RaidManager.Application.Features.Characters.Commands.RemoveMyCharacters;

/// <summary>Handles <see cref="RemoveMyCharactersCommand"/>: deletes the player's own characters and withdraws their
/// other claims, then commits once.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-09<br/>
/// Purpose: A character only the player owns or claims is deleted with its claims, loadouts, raid saves and
/// professions, so the next sync imports it again with a pending claim; on a character another player owns or claims
/// too, only the player's claim goes (#613).
/// </remarks>
internal sealed class RemoveMyCharactersCommandHandler : ICommandHandler<RemoveMyCharactersCommand, int>
{
    #region Fields
    /// <summary>Stores the character repository.</summary>
    private readonly ICharacterRepository _characters;

    /// <summary>Stores the unit of work that commits the removal.</summary>
    private readonly IUnitOfWork _unitOfWork;
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="RemoveMyCharactersCommandHandler"/> class.</summary>
    /// <param name="characters">The character repository.</param>
    /// <param name="unitOfWork">The unit of work.</param>
    public RemoveMyCharactersCommandHandler(ICharacterRepository characters, IUnitOfWork unitOfWork)
    {
        _characters = characters;
        _unitOfWork = unitOfWork;
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

        var saved = await _unitOfWork.SaveChangesAsync(cancellationToken);
        return saved.IsFailure ? Result.Failure<int>(saved.Error, saved.ResultExceptionType) : characters.Count;
    }
    #endregion Public Methods
}
