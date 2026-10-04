using Pivot.Framework.Domain.Repositories;
using RaidManager.Domain.Features.Companions.Aggregates;
using RaidManager.Domain.Features.Companions.ValueObjects;
using RaidManager.Domain.Features.Shared.Identifiers;

namespace RaidManager.Domain.Features.Companions.Repositories;

/// <summary>Defines persistence operations for paired companions.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: Adds the lookup by token hash that checks every companion request (ADR-0030).
/// </remarks>
public interface ICompanionRepository : IAsyncCommandRepository<Companion, CompanionId>
{
    #region Methods
    /// <summary>Finds the companion a device token belongs to.</summary>
    /// <param name="tokenHash">The hash of the device token.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The companion, or <see langword="null"/>.</returns>
    Task<Companion?> FindByTokenHashAsync(CredentialHash tokenHash, CancellationToken cancellationToken);

    /// <summary>Lists a player's companions, revoked ones included, without tracking them.</summary>
    /// <param name="userId">The player.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The companions, oldest pairing first.</returns>
    Task<IReadOnlyList<Companion>> ListByUserAsync(UserId userId, CancellationToken cancellationToken);
    #endregion Methods
}
