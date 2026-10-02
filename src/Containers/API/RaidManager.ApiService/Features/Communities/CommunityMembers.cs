using RaidManager.Application.Features.Communities.Queries.GetCommunityMembers;

namespace RaidManager.ApiService.Features.Communities;

/// <summary>Describes the people in a community's Discord server to the website.</summary>
/// <param name="CommunityId">The community.</param>
/// <param name="ServerName">The Discord server's current name.</param>
/// <param name="CheckedAtUtc">When RaidManager asked Discord.</param>
/// <param name="Members">The people, highest RaidManager role first.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: The API contract of the members page; roles travel as names, like the other website contracts.
/// </remarks>
public sealed record CommunityMembers(Guid CommunityId, string ServerName, DateTimeOffset CheckedAtUtc, IReadOnlyList<CommunityMember> Members)
{
    #region Public Methods
    /// <summary>Maps a query response to the contract.</summary>
    /// <param name="members">The query response.</param>
    /// <returns>The contract.</returns>
    public static CommunityMembers From(CommunityMembersResponse members)
    {
        ArgumentNullException.ThrowIfNull(members);
        return new CommunityMembers(
            members.CommunityId,
            members.ServerName,
            members.CheckedAtUtc,
            [.. members.Members.Select(member => new CommunityMember(
                member.DiscordUserId,
                member.DisplayName,
                member.AvatarUrl,
                [.. member.DiscordRoles.Select(role => new DiscordRoleOption(role.Id, role.Name))],
                member.Roles))]);
    }
    #endregion Public Methods
}
