using Pivot.Framework.Application.Abstractions.Messaging.Queries;

namespace RaidManager.Application.Features.Communities.Queries.GetCommunityRoleSettings;

/// <summary>Requests a community's officer roles as a member of its Discord server sees them.</summary>
/// <param name="CommunityId">The community.</param>
/// <param name="UserId">The signed-in user asking.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Feeds the roles card of the community page (board 4) from Discord's current roles and members (ADR-0022).
/// </remarks>
public sealed record GetCommunityRoleSettingsQuery(Guid CommunityId, Guid UserId) : IQuery<CommunityRoleSettingsResponse>;
