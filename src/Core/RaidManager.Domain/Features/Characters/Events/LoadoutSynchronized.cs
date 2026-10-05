using RaidManager.Domain.Features.Shared.Identifiers;

namespace RaidManager.Domain.Features.Characters.Events;

/// <summary>Signals that a character loadout was synchronized.</summary>
/// <param name="CharacterId">The character identifier.</param>
/// <param name="LoadoutId">The synchronized loadout identifier.</param>
/// <param name="GearScore">The synchronized GearScore, or <see langword="null"/> when the source gives none.</param>
/// <param name="SynchronizedAtUtc">The synchronization timestamp.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-09-29<br/>
/// Purpose: Updates read models and raid-planning freshness information for one raid-capable loadout.
/// </remarks>
public sealed record LoadoutSynchronized(CharacterId CharacterId, LoadoutId LoadoutId, int? GearScore, DateTimeOffset SynchronizedAtUtc) : DomainEvent;
