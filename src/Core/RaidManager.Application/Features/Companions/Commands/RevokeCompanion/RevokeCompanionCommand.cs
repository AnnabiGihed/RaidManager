using Pivot.Framework.Application.Abstractions.Messaging.Commands;

namespace RaidManager.Application.Features.Companions.Commands.RevokeCompanion;

/// <summary>Revokes one of a player's companions, which stops its uploads at once.</summary>
/// <param name="UserId">The signed-in player, from the website session.</param>
/// <param name="CompanionId">The companion to revoke.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: Carries the revocation of ADR-0030 from the website's paired companions list (#15).
/// </remarks>
public sealed record RevokeCompanionCommand(Guid UserId, Guid CompanionId) : ICommand;
