using RaidManager.Domain.Features.Shared.Identifiers;

namespace RaidManager.Domain.Features.Communities.Events;

/// <summary>Signals that a community's roles or the Discord roles giving them changed.</summary>
/// <param name="CommunityId">The community identifier.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Lets cached role checks for the community be dropped, so members get the change at their next check.
/// </remarks>
public sealed record CommunityRoleMappingsChanged(CommunityId CommunityId) : DomainEvent;
