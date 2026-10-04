namespace RaidManager.Companion.Client.Features.Pairing;

/// <summary>The body of <c>POST /companion/pairings</c>.</summary>
/// <param name="ComputerLabel">The computer's name.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: Mirrors the API's request contract of #382.
/// </remarks>
internal sealed record StartPairingRequest(string ComputerLabel);
