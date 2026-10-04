using RaidManager.Application.Features.Companions.Commands.StartCompanionPairing;

namespace RaidManager.ApiService.Features.Companions;

/// <summary>Carries what the companion shows and polls with.</summary>
/// <param name="DeviceCode">The secret to poll with; never shown.</param>
/// <param name="PairingCode">The code to show, such as <c>K7M-4QX</c>.</param>
/// <param name="ExpiresAtUtc">When the code stops working.</param>
/// <param name="PollingIntervalSeconds">The shortest wait between two polls.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: Is the answer of POST /companion/pairings.
/// </remarks>
public sealed record StartedCompanionPairing(string DeviceCode, string PairingCode, DateTimeOffset ExpiresAtUtc, int PollingIntervalSeconds)
{
    #region Factory Methods
    /// <summary>Maps the application response.</summary>
    /// <param name="response">The application response.</param>
    /// <returns>The transport model.</returns>
    public static StartedCompanionPairing From(StartedCompanionPairingResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);
        return new StartedCompanionPairing(response.DeviceCode, response.PairingCode, response.ExpiresAtUtc, response.PollingIntervalSeconds);
    }
    #endregion Factory Methods
}
