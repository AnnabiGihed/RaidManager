using RaidManager.Application.Features.Communities.Abstractions;

namespace RaidManager.Application.Features.Communities.Queries.GetCommunityRoleSettings;

/// <summary>Describes a community's officer roles as the roles card shows them.</summary>
/// <param name="CommunityId">The community.</param>
/// <param name="ServerName">The Discord server's current name.</param>
/// <param name="CanEdit">Whether the user may change roles: the Administrator, or a member whose role manages roles.</param>
/// <param name="CanGrantRoleManagement">Whether the user may let a role manage roles: only the Administrator.</param>
/// <param name="MappableRoles">The server's roles that can be mapped, highest first.</param>
/// <param name="Rows">One row per RaidManager role, from Administrator to Member.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: The read shape of the roles card: Discord's roles and counts, and RaidManager's mappings of them.
/// </remarks>
public sealed record CommunityRoleSettingsResponse(
    Guid CommunityId,
    string ServerName,
    bool CanEdit,
    bool CanGrantRoleManagement,
    IReadOnlyList<DiscordServerRole> MappableRoles,
    IReadOnlyList<CommunityRoleRowResponse> Rows);
