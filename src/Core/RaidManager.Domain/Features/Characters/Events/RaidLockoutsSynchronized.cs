using RaidManager.Domain.Features.Shared.Identifiers;

namespace RaidManager.Domain.Features.Characters.Events;

/// <summary>Signals that a character raid-lockout snapshot was synchronized.</summary>
/// <param name="CharacterId">The character identifier.</param>
/// <param name="SynchronizedAtUtc">The synchronization timestamp.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-09-29<br/>
/// Purpose: Updates raid eligibility and signup validation projections.
/// </remarks>
public sealed record RaidLockoutsSynchronized(CharacterId CharacterId, DateTimeOffset SynchronizedAtUtc) : DomainEvent;
