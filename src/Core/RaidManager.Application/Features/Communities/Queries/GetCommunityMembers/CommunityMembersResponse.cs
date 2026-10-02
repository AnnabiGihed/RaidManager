namespace RaidManager.Application.Features.Communities.Queries.GetCommunityMembers;

/// <summary>Describes the people in a community's Discord server, as Discord reported them at one moment.</summary>
/// <param name="CommunityId">The community.</param>
/// <param name="ServerName">The Discord server's current name.</param>
/// <param name="CheckedAtUtc">When RaidManager asked Discord.</param>
/// <param name="Members">The people, highest RaidManager role first, then by name.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: The read shape of the members page; nothing in it is stored (ADR-0022).
/// </remarks>
public sealed record CommunityMembersResponse(Guid CommunityId, string ServerName, DateTimeOffset CheckedAtUtc, IReadOnlyList<CommunityMemberResponse> Members);
