namespace RaidManager.ViewModels.Features.Companions;

/// <summary>Carries what the API says about a code, and the pairing when one waits for it.</summary>
/// <param name="Status">What the API says.</param>
/// <param name="Pairing">The pairing, when <paramref name="Status"/> is <see cref="PairingCodeStatus.Waiting"/>.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: Lets the confirm page show the computer and expiry, or why the code can't be confirmed.
/// </remarks>
public sealed record PairingLookup(PairingCodeStatus Status, PendingPairing? Pairing);
