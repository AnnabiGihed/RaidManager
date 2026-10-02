using Pivot.Framework.Application.Abstractions.Messaging.Queries;
using RaidManager.Domain.Features.Communities.Enums;

namespace RaidManager.Application.Features.Communities.Queries.GetCommunityPermissions;

/// <summary>Requests what a user may do in a community, from their Discord membership and roles.</summary>
/// <param name="CommunityId">The community.</param>
/// <param name="UserId">The user whose role is checked.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: The one permission check the website and the bot share (ADR-0022): raid actions ask it before they run,
/// for the permission they need (story #308).
/// </remarks>
public sealed record GetCommunityPermissionsQuery(Guid CommunityId, Guid UserId) : IQuery<CommunityPermissions>;
