using Pivot.Framework.Application.Abstractions.Messaging.Queries;
using RaidManager.Domain.Features.Communities.Enums;

namespace RaidManager.Application.Features.Communities.Queries.GetCommunityRole;

/// <summary>Requests a user's current RaidManager role in a community, from their Discord membership and roles.</summary>
/// <param name="CommunityId">The community.</param>
/// <param name="UserId">The user whose role is checked.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: The one role check the website and the bot share (ADR-0022): officer actions ask it before they run.
/// </remarks>
public sealed record GetCommunityRoleQuery(Guid CommunityId, Guid UserId) : IQuery<CommunityMemberRole>;
