using RaidManager.Domain.Features.Shared.Identifiers;

namespace RaidManager.Domain.Features.Communities.Events;

/// <summary>Signals that a user joined a raiding community.</summary>
/// <param name="CommunityId">The community identifier.</param>
/// <param name="UserId">The application user identifier.</param>
/// <param name="Role">The community role name.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-09-29<br/>
/// Purpose: Updates community membership and future Discord authorization projections.
/// </remarks>
public sealed record CommunityMemberAdded(CommunityId CommunityId, UserId UserId, string Role) : DomainEvent;
