using RaidManager.Domain.Features.Characters.Aggregates;
using RaidManager.Domain.Features.Shared.Identifiers;

namespace RaidManager.Domain.Features.Characters.Repositories;

/// <summary>Defines persistence operations for the Warmane character aggregate root.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-09-29<br/>
/// Purpose: Keeps aggregate persistence contracts in the Domain layer while implementations remain in EF Core infrastructure.
/// </remarks>
public interface ICharacterRepository
{
    #region Methods
    /// <summary>Finds an aggregate by its strongly typed identifier.</summary>
    /// <param name="id">The aggregate identifier.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The aggregate when found; otherwise <see langword="null"/>.</returns>
    Task<Character?> FindAsync(CharacterId id, CancellationToken cancellationToken);

    /// <summary>Adds a newly created aggregate to persistence.</summary>
    /// <param name="aggregate">The aggregate to add.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task AddAsync(Character aggregate, CancellationToken cancellationToken);

    /// <summary>Marks an existing aggregate for persistence of its current state.</summary>
    /// <param name="aggregate">The aggregate to update.</param>
    void Update(Character aggregate);
    #endregion Methods
}
