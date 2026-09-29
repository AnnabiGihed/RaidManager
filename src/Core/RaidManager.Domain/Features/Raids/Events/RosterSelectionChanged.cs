using RaidManager.Domain.Features.Shared.Identifiers;

namespace RaidManager.Domain.Features.Raids.Events;

/// <summary>Signals that the selected roster option for a user changed.</summary>
/// <param name="RaidId">The raid identifier.</param>
/// <param name="UserId">The application user identifier.</param>
/// <param name="CharacterId">The character identifier.</param>
/// <param name="LoadoutId">The synchronized loadout identifier.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-09-29<br/>
/// Purpose: Updates final roster, composition and raid-buff projections.
/// </remarks>
public sealed record RosterSelectionChanged(RaidId RaidId, UserId UserId, CharacterId CharacterId, LoadoutId LoadoutId) : DomainEvent;
