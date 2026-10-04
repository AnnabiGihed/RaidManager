namespace RaidManager.Companion.Client.Features.Pairing;

/// <summary>The answer of <c>POST /companion/pairings</c>.</summary>
/// <param name="DeviceCode">The device code.</param>
/// <param name="PairingCode">The pairing code.</param>
/// <param name="ExpiresAtUtc">When the code expires.</param>
/// <param name="PollingIntervalSeconds">The polling interval, in seconds.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: Mirrors the API's answer contract of #382.
/// </remarks>
internal sealed record StartedPairingResponse(string DeviceCode, string PairingCode, DateTimeOffset ExpiresAtUtc, int PollingIntervalSeconds);
