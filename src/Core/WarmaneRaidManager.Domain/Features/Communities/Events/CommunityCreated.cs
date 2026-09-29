using WarmaneRaidManager.Domain.Features.Shared.Identifiers;

namespace WarmaneRaidManager.Domain.Features.Communities.Events;

/// <summary>Signals that a Discord-backed raiding community was created.</summary>
/// <param name="CommunityId">The community identifier.</param>
/// <param name="DiscordGuildId">The Discord guild snowflake.</param>
/// <param name="Name">The character or community name.</param>
/// <remarks>
/// Author: Rodolphe Balay<br/>
/// Date: 2026-09-29<br/>
/// Purpose: Creates projections and integration state for the community.
/// </remarks>
public sealed record CommunityCreated(CommunityId CommunityId, string DiscordGuildId, string Name) : DomainEvent;
