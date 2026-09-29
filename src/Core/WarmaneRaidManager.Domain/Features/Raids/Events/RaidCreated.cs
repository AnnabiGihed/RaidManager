using WarmaneRaidManager.Domain.Features.Shared.Identifiers;

namespace WarmaneRaidManager.Domain.Features.Raids.Events;

/// <summary>Signals that a raid event was created.</summary>
/// <param name="RaidId">The raid identifier.</param>
/// <param name="CommunityId">The community identifier.</param>
/// <param name="Instance">The raid instance name.</param>
/// <param name="StartsAtUtc">The scheduled raid start timestamp.</param>
/// <remarks>
/// Author: Rodolphe Balay<br/>
/// Date: 2026-09-29<br/>
/// Purpose: Drives calendar, Discord-announcement and raid-list projections.
/// </remarks>
public sealed record RaidCreated(RaidId RaidId, CommunityId CommunityId, string Instance, DateTimeOffset StartsAtUtc) : DomainEvent;
