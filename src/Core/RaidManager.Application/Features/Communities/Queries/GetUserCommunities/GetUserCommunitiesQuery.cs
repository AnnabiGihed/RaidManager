using Pivot.Framework.Application.Abstractions.Messaging.Queries;

namespace RaidManager.Application.Features.Communities.Queries.GetUserCommunities;

/// <summary>Requests the communities a user belongs to.</summary>
/// <param name="UserId">The user.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Feeds the Overview and the community card of the sidebar. Lists the communities the user administers; #296 adds the ones they are a member of.
/// </remarks>
public sealed record GetUserCommunitiesQuery(Guid UserId) : IQuery<IReadOnlyList<CommunitySummaryResponse>>;
