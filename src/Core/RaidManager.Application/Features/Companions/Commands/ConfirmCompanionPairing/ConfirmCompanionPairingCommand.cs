using Pivot.Framework.Application.Abstractions.Messaging.Commands;

namespace RaidManager.Application.Features.Companions.Commands.ConfirmCompanionPairing;

/// <summary>Records that a signed-in player confirmed the code their companion shows.</summary>
/// <param name="UserId">The player, from the website session.</param>
/// <param name="PairingCode">The code, with or without its dash.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: Binds a computer to the player's Discord identity with their explicit consent (ADR-0030, #15).
/// </remarks>
public sealed record ConfirmCompanionPairingCommand(Guid UserId, string PairingCode) : ICommand;
