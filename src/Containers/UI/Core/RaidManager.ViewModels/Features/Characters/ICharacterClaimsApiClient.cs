namespace RaidManager.ViewModels.Features.Characters;

/// <summary>Calls the API's character claim review endpoints on behalf of the website server.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Lets the review view model and the sign-in redirect read and decide claims without a running API in tests.
/// </remarks>
public interface ICharacterClaimsApiClient
{
    #region Methods
    /// <summary>Lists the player's pending and conflicted claims, oldest first.</summary>
    /// <param name="userId">The signed-in player.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>The claims; empty when none await a decision.</returns>
    /// <exception cref="HttpRequestException">Thrown when the API rejects the call or cannot be reached.</exception>
    Task<IReadOnlyList<CharacterClaim>> GetPendingAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>Approves the player's claim on a character.</summary>
    /// <param name="userId">The signed-in player.</param>
    /// <param name="characterId">The claimed character.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>Whether the API recorded or refused the approval.</returns>
    /// <exception cref="HttpRequestException">Thrown when the API fails or cannot be reached.</exception>
    Task<ClaimDecisionOutcome> ApproveAsync(Guid userId, Guid characterId, CancellationToken cancellationToken);

    /// <summary>Rejects the player's claim on a character.</summary>
    /// <param name="userId">The signed-in player.</param>
    /// <param name="characterId">The claimed character.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>Whether the API recorded or refused the rejection.</returns>
    /// <exception cref="HttpRequestException">Thrown when the API fails or cannot be reached.</exception>
    Task<ClaimDecisionOutcome> RejectAsync(Guid userId, Guid characterId, CancellationToken cancellationToken);
    #endregion Methods
}
