using RaidManager.Domain.Features.Shared.Identifiers;

namespace RaidManager.Domain.Features.Raids.Events;

/// <summary>Signals that a raid event was created.</summary>
/// <param name="RaidId">The raid identifier.</param>
/// <param name="CommunityId">The community identifier.</param>
/// <param name="Targets">The required instance and difficulty of each raid target.</param>
/// <param name="StartsAtUtc">The scheduled raid start timestamp.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-09-29<br/>
/// Purpose: Drives calendar, Discord-announcement and raid-list projections.
/// </remarks>
public sealed record RaidCreated(RaidId RaidId, CommunityId CommunityId, IReadOnlyList<string> Targets, DateTimeOffset StartsAtUtc) : DomainEvent;
