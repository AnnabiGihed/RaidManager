using Pivot.Framework.Application.Abstractions.Messaging.Queries;

namespace RaidManager.Application.Features.Communities.Queries.GetCommunity;

/// <summary>Requests one community by its identifier.</summary>
/// <param name="CommunityId">The community.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Feeds the community settings page its server, realm and Administrator.
/// </remarks>
public sealed record GetCommunityQuery(Guid CommunityId) : IQuery<CommunitySummaryResponse>;
