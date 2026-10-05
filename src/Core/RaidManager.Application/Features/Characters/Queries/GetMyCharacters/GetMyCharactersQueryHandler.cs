using Pivot.Framework.Application.Abstractions.Messaging.Queries;
using Pivot.Framework.Domain.Shared;
using RaidManager.Application.Features.Characters.Abstractions;
using RaidManager.Domain.Features.Shared.Identifiers;

namespace RaidManager.Application.Features.Characters.Queries.GetMyCharacters;

/// <summary>Handles <see cref="GetMyCharactersQuery"/> through the read-only profile reader.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-05<br/>
/// Purpose: Answers the My characters page without touching the write model; the clock decides which raid saves still count.
/// </remarks>
internal sealed class GetMyCharactersQueryHandler : IQueryHandler<GetMyCharactersQuery, IReadOnlyList<CharacterSummaryResponse>>
{
    #region Fields
    /// <summary>Stores the read-only profile reader.</summary>
    private readonly ICharacterProfileReader _profiles;

    /// <summary>Stores the clock that decides which raid saves have expired.</summary>
    private readonly TimeProvider _timeProvider;
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="GetMyCharactersQueryHandler"/> class.</summary>
    /// <param name="profiles">The read-only profile reader.</param>
    /// <param name="timeProvider">The clock that decides which raid saves have expired.</param>
    public GetMyCharactersQueryHandler(ICharacterProfileReader profiles, TimeProvider timeProvider)
    {
        _profiles = profiles;
        _timeProvider = timeProvider;
    }
    #endregion Constructors

    #region Public Methods
    /// <summary>Lists the player's characters.</summary>
    /// <param name="request">The query.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The player's characters; an empty list when they own none.</returns>
    public async Task<Result<IReadOnlyList<CharacterSummaryResponse>>> Handle(GetMyCharactersQuery request, CancellationToken cancellationToken) =>
        Result.Success(await _profiles.ListOwnedAsync(new UserId(request.UserId), _timeProvider.GetUtcNow(), cancellationToken));
    #endregion Public Methods
}
