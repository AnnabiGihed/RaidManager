namespace RaidManager.Companion.Client.Features.Pairing;

/// <summary>Describes a pairing the API started for this computer.</summary>
/// <param name="DeviceCode">The secret the companion polls with; never shown or logged.</param>
/// <param name="PairingCode">The code the player confirms on the website, such as <c>K7M-4QX</c>.</param>
/// <param name="ExpiresAtUtc">When the code stops being valid.</param>
/// <param name="PollingInterval">How long to wait between two polls, as the API asks.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: Carries the answer of <c>POST /companion/pairings</c> (ADR-0030, step 1) to the pairing view model.
/// </remarks>
public sealed record StartedPairing(string DeviceCode, string PairingCode, DateTimeOffset ExpiresAtUtc, TimeSpan PollingInterval)
{
    #region Public Methods
    /// <summary>Describes the pairing by its code only, so the device code never reaches a log.</summary>
    /// <returns>The pairing code.</returns>
    public override string ToString() => PairingCode;
    #endregion Public Methods
}
