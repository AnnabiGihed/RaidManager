using RaidManager.Domain.Features.Shared.Identifiers;

namespace RaidManager.Domain.Features.Companions.Events;

/// <summary>Signals that a player confirmed a companion's pairing code on the website.</summary>
/// <param name="PairingId">The pairing request.</param>
/// <param name="UserId">The player who confirmed it.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: Records the player's consent that binds a computer to their Discord identity.
/// </remarks>
public sealed record CompanionPairingConfirmed(CompanionPairingId PairingId, UserId UserId) : DomainEvent;
