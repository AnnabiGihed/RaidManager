namespace RaidManager.Application.Features.Companions.Commands.StartCompanionPairing;

/// <summary>Carries what a companion needs to show its code and poll for its token.</summary>
/// <param name="DeviceCode">The secret the companion polls with; shown nowhere.</param>
/// <param name="PairingCode">The code to show, such as <c>K7M-4QX</c>.</param>
/// <param name="ExpiresAtUtc">When the code stops working.</param>
/// <param name="PollingIntervalSeconds">The shortest wait between two polls.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: Returns the device code once; the API keeps only its hash.
/// </remarks>
public sealed record StartedCompanionPairingResponse(string DeviceCode, string PairingCode, DateTimeOffset ExpiresAtUtc, int PollingIntervalSeconds);
