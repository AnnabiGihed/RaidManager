using RaidManager.Domain.Features.Shared.Identifiers;

namespace RaidManager.Domain.Features.Communities.Events;

/// <summary>Signals that a Discord server was linked to RaidManager as a community.</summary>
/// <param name="CommunityId">The community identifier.</param>
/// <param name="DiscordGuildId">The Discord guild snowflake.</param>
/// <param name="Name">The community name.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-09-29<br/>
/// Purpose: Creates projections and integration state for the community.
/// </remarks>
public sealed record CommunityCreated(CommunityId CommunityId, string DiscordGuildId, string Name) : DomainEvent;
