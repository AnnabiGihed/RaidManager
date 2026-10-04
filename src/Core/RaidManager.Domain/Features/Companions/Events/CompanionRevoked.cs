using RaidManager.Domain.Features.Shared.Identifiers;

namespace RaidManager.Domain.Features.Companions.Events;

/// <summary>Signals that a player revoked a companion, which stops its uploads at once.</summary>
/// <param name="CompanionId">The revoked companion.</param>
/// <param name="UserId">The player who revoked it.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: Records the final revocation of ADR-0030.
/// </remarks>
public sealed record CompanionRevoked(CompanionId CompanionId, UserId UserId) : DomainEvent;
