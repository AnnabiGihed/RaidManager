using Pivot.Framework.Application.Abstractions.Messaging.Commands;

namespace RaidManager.Application.Features.Companions.Commands.StartCompanionPairing;

/// <summary>Requests a new pairing for a companion that isn't paired yet.</summary>
/// <param name="ComputerLabel">The computer's label the player will see, such as its Windows name.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: Starts the code flow of ADR-0030; the companion calls it before it has any credential.
/// </remarks>
public sealed record StartCompanionPairingCommand(string? ComputerLabel) : ICommand<StartedCompanionPairingResponse>;
