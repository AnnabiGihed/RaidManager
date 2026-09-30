using RaidManager.Application.Features.Characters.Queries.GetPendingCharacterClaims;
using RaidManager.Domain.Features.Shared.Identifiers;

namespace RaidManager.Application.Features.Characters.Abstractions;

/// <summary>Reads character claims for queries, without loading aggregates.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-09-30<br/>
/// Purpose: Keeps queries off the command repositories: persistence implements this with read-only queries (ADR-0010).
/// </remarks>
public interface ICharacterClaimReader
{
    #region Methods
    /// <summary>Lists the player's pending and conflicted claims, oldest request first.</summary>
    /// <param name="userId">The player whose claims are listed.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>One entry per claimed character awaiting the player's decision.</returns>
    Task<IReadOnlyList<PendingCharacterClaimResponse>> ListAwaitingDecisionAsync(UserId userId, CancellationToken cancellationToken);
    #endregion Methods
}
