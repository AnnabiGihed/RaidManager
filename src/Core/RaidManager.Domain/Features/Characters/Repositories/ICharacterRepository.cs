using Pivot.Framework.Domain.Repositories;
using RaidManager.Domain.Features.Characters.Aggregates;
using RaidManager.Domain.Features.Characters.ValueObjects;
using RaidManager.Domain.Features.Shared.Enums;
using RaidManager.Domain.Features.Shared.Identifiers;

namespace RaidManager.Domain.Features.Characters.Repositories;

/// <summary>Defines persistence operations for the Warmane character aggregate root.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-09-29<br/>
/// Purpose: Keeps aggregate persistence contracts in the Domain layer on Pivot's command repository, while implementations remain in EF Core infrastructure.
/// </remarks>
public interface ICharacterRepository : IAsyncCommandRepository<Character, CharacterId>
{
    #region Methods
    /// <summary>Finds the character with a name on a realm, which identify a character in the game.</summary>
    /// <param name="realm">The Warmane realm.</param>
    /// <param name="name">The normalized character name.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The character, or <see langword="null"/> when RaidManager doesn't know it.</returns>
    Task<Character?> FindByRealmAndNameAsync(WarmaneRealm realm, CharacterName name, CancellationToken cancellationToken);

    /// <summary>Lists the characters a user owns or claims, in any state.</summary>
    /// <param name="userId">The user.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The characters, tracked for changes; empty when the user has none.</returns>
    Task<IReadOnlyList<Character>> ListOwnedOrClaimedByAsync(UserId userId, CancellationToken cancellationToken);
    #endregion Methods
}
