using Pivot.Framework.Application.Abstractions.Messaging.Queries;

namespace RaidManager.Application.Features.Communities.Queries.GetUserCommunities;

/// <summary>Requests the communities a user belongs to.</summary>
/// <param name="UserId">The user.</param>
/// <param name="MemberOf">The communities the user's Discord servers matched at sign-in (ADR-0023).</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Feeds the Overview and the community card of the sidebar: the communities the user administers, then the
/// ones they are a member of.
/// </remarks>
public sealed record GetUserCommunitiesQuery(Guid UserId, IReadOnlyList<Guid> MemberOf) : IQuery<IReadOnlyList<CommunitySummaryResponse>>;
