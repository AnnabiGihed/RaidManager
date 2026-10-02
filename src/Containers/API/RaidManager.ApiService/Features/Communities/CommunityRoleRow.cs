namespace RaidManager.ApiService.Features.Communities;

/// <summary>Describes one row of the roles card: a role, its Discord roles, what it allows and how many members have it.</summary>
/// <param name="Kind">The row's kind: <c>Administrator</c>, <c>Role</c> for one of the community's roles, or <c>Member</c>.</param>
/// <param name="RoleId">The role's identifier; <see langword="null"/> for the Administrator and Member rows.</param>
/// <param name="Name">The role's name, such as <c>Officer</c> or <c>Veteran</c>.</param>
/// <param name="Permissions">What the role allows: <c>ManageRaids</c>, <c>BuildRosters</c>, <c>RunRaidNight</c>, <c>ReviewConflicts</c>, <c>ManageCommunityRoles</c>.</param>
/// <param name="DiscordRoles">The Discord roles mapped to it.</param>
/// <param name="CanChange">Whether the user may edit, delete and map this role.</param>
/// <param name="Members">How many people in the server get it; for Member, how many get no role.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: One row of the roles card (story #308).
/// </remarks>
public sealed record CommunityRoleRow(
    string Kind,
    Guid? RoleId,
    string Name,
    IReadOnlyList<string> Permissions,
    IReadOnlyList<MappedDiscordRole> DiscordRoles,
    bool CanChange,
    int Members);
