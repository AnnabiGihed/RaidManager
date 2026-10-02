namespace RaidManager.ViewModels.Features.Communities;

/// <summary>Describes the people in a community's Discord server, as the API returns them.</summary>
/// <param name="CommunityId">The community.</param>
/// <param name="ServerName">The Discord server's current name.</param>
/// <param name="CheckedAtUtc">When RaidManager asked Discord.</param>
/// <param name="Members">The people, highest RaidManager role first.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: The website's copy of the API's members contract.
/// </remarks>
public sealed record CommunityMembers(Guid CommunityId, string ServerName, DateTimeOffset CheckedAtUtc, IReadOnlyList<CommunityMember> Members);
