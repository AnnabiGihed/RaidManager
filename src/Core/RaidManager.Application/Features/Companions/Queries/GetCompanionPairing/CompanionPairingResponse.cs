namespace RaidManager.Application.Features.Companions.Queries.GetCompanionPairing;

/// <summary>Describes a pairing waiting for confirmation.</summary>
/// <param name="PairingCode">The code, such as <c>K7M-4QX</c>.</param>
/// <param name="ComputerLabel">The computer's label.</param>
/// <param name="RequestedAtUtc">When the companion asked.</param>
/// <param name="ExpiresAtUtc">When the code stops working.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: Shows the player what they are about to confirm; never the device code.
/// </remarks>
public sealed record CompanionPairingResponse(string PairingCode, string ComputerLabel, DateTimeOffset RequestedAtUtc, DateTimeOffset ExpiresAtUtc);
