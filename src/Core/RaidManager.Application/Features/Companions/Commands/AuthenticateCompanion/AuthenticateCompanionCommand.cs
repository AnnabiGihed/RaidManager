using Pivot.Framework.Application.Abstractions.Messaging.Commands;

namespace RaidManager.Application.Features.Companions.Commands.AuthenticateCompanion;

/// <summary>Checks a companion's device token and records its use.</summary>
/// <param name="DeviceToken">The bearer token the companion sent, or an empty value when it sent none.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: Runs on every companion request, so a revoked or expired companion is refused at once (ADR-0030, #15).
/// </remarks>
public sealed record AuthenticateCompanionCommand(string DeviceToken) : ICommand<AuthenticatedCompanionResponse>;
