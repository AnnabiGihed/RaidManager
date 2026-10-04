namespace RaidManager.ApiService.Features.Companions;

/// <summary>Asks to start pairing a companion.</summary>
/// <param name="ComputerLabel">The computer's label the player will see, at most 200 characters; longer than 64 is cut.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: Is the body of POST /companion/pairings.
/// </remarks>
public sealed record StartCompanionPairingRequest(string? ComputerLabel);
