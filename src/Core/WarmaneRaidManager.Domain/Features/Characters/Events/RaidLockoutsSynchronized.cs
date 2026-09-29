using WarmaneRaidManager.Domain.Features.Shared.Identifiers;

namespace WarmaneRaidManager.Domain.Features.Characters.Events;

/// <summary>Signals that a character raid-lockout snapshot was synchronized.</summary>
/// <param name="CharacterId">The character identifier.</param>
/// <param name="SynchronizedAtUtc">The synchronization timestamp.</param>
/// <remarks>
/// Author: Rodolphe Balay<br/>
/// Date: 2026-09-29<br/>
/// Purpose: Updates raid eligibility and signup validation projections.
/// </remarks>
public sealed record RaidLockoutsSynchronized(CharacterId CharacterId, DateTimeOffset SynchronizedAtUtc) : DomainEvent;
