using Pivot.Framework.Application.Abstractions.Messaging.Queries;

namespace RaidManager.Application.Features.Communities.Queries.GetCommunityMembers;

/// <summary>Requests the people in a community's Discord server, with the RaidManager role each one gets.</summary>
/// <param name="CommunityId">The community.</param>
/// <param name="UserId">The signed-in user asking.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Feeds the members page (board 5) from Discord's current members and roles (ADR-0022).
/// </remarks>
public sealed record GetCommunityMembersQuery(Guid CommunityId, Guid UserId) : IQuery<CommunityMembersResponse>;
