using Pivot.Framework.Application.Abstractions.Messaging.Queries;
using Pivot.Framework.Domain.Shared;
using RaidManager.Application.Features.Characters.Abstractions;
using RaidManager.Domain.Features.Characters.Errors;
using RaidManager.Domain.Features.Shared.Identifiers;

namespace RaidManager.Application.Features.Characters.Queries.GetCharacterProfile;

/// <summary>Handles <see cref="GetCharacterProfileQuery"/> through the read-only profile reader.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-05<br/>
/// Purpose: Answers the profile page without touching the write model. A character that isn't the player's is reported as not found, so the answer doesn't reveal that it exists.
/// </remarks>
internal sealed class GetCharacterProfileQueryHandler : IQueryHandler<GetCharacterProfileQuery, CharacterProfileResponse>
{
    #region Fields
    /// <summary>Stores the read-only profile reader.</summary>
    private readonly ICharacterProfileReader _profiles;

    /// <summary>Stores the clock that decides which raid saves have expired.</summary>
    private readonly TimeProvider _timeProvider;
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="GetCharacterProfileQueryHandler"/> class.</summary>
    /// <param name="profiles">The read-only profile reader.</param>
    /// <param name="timeProvider">The clock that decides which raid saves have expired.</param>
    public GetCharacterProfileQueryHandler(ICharacterProfileReader profiles, TimeProvider timeProvider)
    {
        _profiles = profiles;
        _timeProvider = timeProvider;
    }
    #endregion Constructors

    #region Public Methods
    /// <summary>Finds the profile.</summary>
    /// <param name="request">The query.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The profile, or <see cref="CharacterErrors.NotFound"/> (<see cref="ResultExceptionType.NotFound"/>).</returns>
    public async Task<Result<CharacterProfileResponse>> Handle(GetCharacterProfileQuery request, CancellationToken cancellationToken)
    {
        var profile = await _profiles.FindOwnedProfileAsync(
            new UserId(request.UserId), new CharacterId(request.CharacterId), _timeProvider.GetUtcNow(), cancellationToken);
        return profile is null
            ? Result.Failure<CharacterProfileResponse>(CharacterErrors.NotFound, ResultExceptionType.NotFound)
            : profile;
    }
    #endregion Public Methods
}
