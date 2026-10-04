using RaidManager.Application.Features.Companions.Queries.GetCompanionPairing;

namespace RaidManager.ApiService.Features.Companions;

/// <summary>Describes a pairing waiting for the player's confirmation.</summary>
/// <param name="PairingCode">The code, such as <c>K7M-4QX</c>.</param>
/// <param name="ComputerLabel">The computer's label.</param>
/// <param name="RequestedAtUtc">When the companion asked.</param>
/// <param name="ExpiresAtUtc">When the code stops working.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: Feeds the website's confirmation page (mockup state 1).
/// </remarks>
public sealed record CompanionPairingDetails(string PairingCode, string ComputerLabel, DateTimeOffset RequestedAtUtc, DateTimeOffset ExpiresAtUtc)
{
    #region Factory Methods
    /// <summary>Maps the application response.</summary>
    /// <param name="response">The application response.</param>
    /// <returns>The transport model.</returns>
    public static CompanionPairingDetails From(CompanionPairingResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);
        return new CompanionPairingDetails(response.PairingCode, response.ComputerLabel, response.RequestedAtUtc, response.ExpiresAtUtc);
    }
    #endregion Factory Methods
}
