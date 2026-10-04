namespace RaidManager.ViewModels.Features.Companions;

/// <summary>Describes a pairing waiting for the player's confirmation.</summary>
/// <param name="PairingCode">The code, such as <c>K7M-4QX</c>.</param>
/// <param name="ComputerLabel">The computer's label.</param>
/// <param name="RequestedAtUtc">When the companion asked.</param>
/// <param name="ExpiresAtUtc">When the code stops working.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: Mirrors the API's transport record, so the website never depends on domain types.
/// </remarks>
public sealed record PendingPairing(string PairingCode, string ComputerLabel, DateTimeOffset RequestedAtUtc, DateTimeOffset ExpiresAtUtc);
