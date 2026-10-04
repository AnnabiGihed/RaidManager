namespace RaidManager.ViewModels.Features.Companions;

/// <summary>Calls the API's website-only companion routes on behalf of the website server.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: Lets the companion view models look up and confirm codes, and list and revoke companions, without a running API in tests.
/// </remarks>
public interface ICompanionsApiClient
{
    #region Methods
    /// <summary>Looks up the pairing a code belongs to.</summary>
    /// <param name="userId">The signed-in player.</param>
    /// <param name="pairingCode">The code, as the companion's link gave it.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>The pairing, or why the code can't be confirmed.</returns>
    /// <exception cref="HttpRequestException">Thrown when the API fails, limits the player, or can't be reached.</exception>
    Task<PairingLookup> GetPairingAsync(Guid userId, string pairingCode, CancellationToken cancellationToken);

    /// <summary>Confirms a code for the player.</summary>
    /// <param name="userId">The signed-in player.</param>
    /// <param name="pairingCode">The code.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns><see cref="PairingCodeStatus.Paired"/>, or why the code can't be confirmed.</returns>
    /// <exception cref="HttpRequestException">Thrown when the API fails, limits the player, or can't be reached.</exception>
    Task<PairingCodeStatus> ConfirmAsync(Guid userId, string pairingCode, CancellationToken cancellationToken);

    /// <summary>Lists the player's companions, revoked and expired ones included, oldest first.</summary>
    /// <param name="userId">The signed-in player.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>The companions; empty when there are none.</returns>
    /// <exception cref="HttpRequestException">Thrown when the API fails or can't be reached.</exception>
    Task<IReadOnlyList<PairedCompanion>> GetCompanionsAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>Revokes one of the player's companions.</summary>
    /// <param name="userId">The signed-in player.</param>
    /// <param name="companionId">The companion.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>Whether the API revoked it or refused.</returns>
    /// <exception cref="HttpRequestException">Thrown when the API fails or can't be reached.</exception>
    Task<RevokeOutcome> RevokeAsync(Guid userId, Guid companionId, CancellationToken cancellationToken);
    #endregion Methods
}
