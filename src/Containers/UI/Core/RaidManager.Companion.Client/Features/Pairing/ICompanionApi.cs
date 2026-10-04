namespace RaidManager.Companion.Client.Features.Pairing;

/// <summary>Calls the API's public <c>/companion</c> routes.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: Hides HTTP from the pairing flow, so its rules are tested with a fake (ADR-0030, ADR-0032).
/// </remarks>
public interface ICompanionApi
{
    #region Public Methods
    /// <summary>Starts a pairing for this computer.</summary>
    /// <param name="computerLabel">The computer's name, shown to the player on the website.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>The started pairing.</returns>
    /// <exception cref="HttpRequestException">Thrown when RaidManager can't be reached or refuses the request.</exception>
    Task<StartedPairing> StartPairingAsync(string computerLabel, CancellationToken cancellationToken);

    /// <summary>Asks once for the device token of a pairing.</summary>
    /// <param name="deviceCode">The pairing's device code.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>What the API answered; a network failure is <see cref="TokenPollStatus.Unavailable"/>.</returns>
    Task<TokenPoll> CollectTokenAsync(string deviceCode, CancellationToken cancellationToken);

    /// <summary>Checks that a stored device token is still accepted.</summary>
    /// <param name="deviceToken">The device token.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>Whether the token is valid, refused, or couldn't be checked.</returns>
    Task<TokenCheckStatus> CheckTokenAsync(string deviceToken, CancellationToken cancellationToken);
    #endregion Public Methods
}
