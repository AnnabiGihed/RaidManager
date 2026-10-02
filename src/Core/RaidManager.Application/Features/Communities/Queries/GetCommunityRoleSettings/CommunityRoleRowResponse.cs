using RaidManager.Domain.Features.Communities.Enums;

namespace RaidManager.Application.Features.Communities.Queries.GetCommunityRoleSettings;

/// <summary>Describes one row of the roles card: a role, its Discord roles, what it allows and how many members have it.</summary>
/// <param name="Kind">The row's kind: <see cref="CommunityRoleKinds.Administrator"/>, <see cref="CommunityRoleKinds.Role"/> or <see cref="CommunityRoleKinds.Member"/>.</param>
/// <param name="RoleId">The role's identifier; <see langword="null"/> for the Administrator and Member rows.</param>
/// <param name="Name">The role's name.</param>
/// <param name="Permissions">What the role allows; every permission for the Administrator, none for Member.</param>
/// <param name="DiscordRoles">The Discord roles mapped to it; empty for Administrator and Member.</param>
/// <param name="Members">How many people get it from their Discord roles; for Member, how many get no role.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: One row of the roles card of board 9 (story #308).
/// </remarks>
public sealed record CommunityRoleRowResponse(
    string Kind,
    Guid? RoleId,
    string Name,
    CommunityPermissions Permissions,
    IReadOnlyList<MappedDiscordRoleResponse> DiscordRoles,
    int Members);
