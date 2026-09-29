using WarmaneRaidManager.Domain.Features.Raids.Aggregates;
using WarmaneRaidManager.Domain.Features.Shared.Identifiers;

namespace WarmaneRaidManager.Domain.Features.Raids.Repositories;

/// <summary>Defines persistence operations for the raid aggregate root.</summary>
/// <remarks>
/// Author: Rodolphe Balay<br/>
/// Date: 2026-09-29<br/>
/// Purpose: Keeps aggregate persistence contracts in the Domain layer while implementations remain in EF Core infrastructure.
/// </remarks>
public interface IRaidRepository
{
    #region Methods
    /// <summary>Finds an aggregate by its strongly typed identifier.</summary>
    /// <param name="id">The aggregate identifier.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The aggregate when found; otherwise <see langword="null"/>.</returns>
    Task<Raid?> FindAsync(RaidId id, CancellationToken cancellationToken);

    /// <summary>Adds a newly created aggregate to persistence.</summary>
    /// <param name="aggregate">The aggregate to add.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task AddAsync(Raid aggregate, CancellationToken cancellationToken);

    /// <summary>Marks an existing aggregate for persistence of its current state.</summary>
    /// <param name="aggregate">The aggregate to update.</param>
    void Update(Raid aggregate);
    #endregion Methods
}
