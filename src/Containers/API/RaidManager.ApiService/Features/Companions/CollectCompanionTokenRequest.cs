namespace RaidManager.ApiService.Features.Companions;

/// <summary>Asks for the device token of a pairing.</summary>
/// <param name="DeviceCode">The device code from POST /companion/pairings.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: Is the body of POST /companion/pairings/token.
/// </remarks>
public sealed record CollectCompanionTokenRequest(string DeviceCode);
