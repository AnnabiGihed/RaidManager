using Pivot.Framework.Application.Abstractions.Messaging.Commands;

namespace RaidManager.Application.Features.Companions.Commands.CollectCompanionToken;

/// <summary>Asks for the device token of a pairing, with the device code the companion received.</summary>
/// <param name="DeviceCode">The device code.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: Is the poll of ADR-0030: it answers pending until the player confirms, then the token once.
/// </remarks>
public sealed record CollectCompanionTokenCommand(string DeviceCode) : ICommand<CompanionTokenResponse>;
