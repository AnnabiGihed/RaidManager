using RaidManager.Application.Features.Characters.Queries.GetCharacterProfile;
using RaidManager.Application.Features.Characters.Queries.GetMyCharacters;
using RaidManager.Domain.Features.Shared.Identifiers;

namespace RaidManager.Application.Features.Characters.Abstractions;

/// <summary>Reads character profiles for queries, without loading aggregates.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-05<br/>
/// Purpose: Keeps the profile queries of story #19 off the command repositories (ADR-0010). Only a character's verified owner sees its profile until the visibility setting arrives (owner decision on #19, 2026-10-05).
/// </remarks>
public interface ICharacterProfileReader
{
    #region Methods
    /// <summary>Lists the characters the player owns through an approved claim, by realm and then name.</summary>
    /// <param name="userId">The player.</param>
    /// <param name="nowUtc">The instant before which a raid save has expired.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>One summary per owned character; an empty list when the player owns none.</returns>
    Task<IReadOnlyList<CharacterSummaryResponse>> ListOwnedAsync(UserId userId, DateTimeOffset nowUtc, CancellationToken cancellationToken);

    /// <summary>Finds the profile of a character the player owns through an approved claim.</summary>
    /// <param name="userId">The player.</param>
    /// <param name="characterId">The character.</param>
    /// <param name="nowUtc">The instant before which a raid save has expired.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The profile, or <see langword="null"/> when the character doesn't exist or isn't the player's.</returns>
    Task<CharacterProfileResponse?> FindOwnedProfileAsync(UserId userId, CharacterId characterId, DateTimeOffset nowUtc, CancellationToken cancellationToken);
    #endregion Methods
}
