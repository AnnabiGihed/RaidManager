namespace RaidManager.ViewModels.Features.Communities;

/// <summary>Describes a community's roles as the API returns them.</summary>
/// <param name="CommunityId">The community.</param>
/// <param name="ServerName">The Discord server's current name.</param>
/// <param name="CanEdit">Whether the signed-in user may change roles: the Administrator, or a member whose role manages roles.</param>
/// <param name="CanGrantRoleManagement">Whether the signed-in user may let a role manage roles: only the Administrator.</param>
/// <param name="MappableRoles">The server's roles that can be mapped, highest first.</param>
/// <param name="Rows">The Administrator row, one row per role, then the Member row.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: The website's copy of the API's roles card contract.
/// </remarks>
public sealed record CommunityRoleSettings(
    Guid CommunityId,
    string ServerName,
    bool CanEdit,
    bool CanGrantRoleManagement,
    IReadOnlyList<DiscordRoleOption> MappableRoles,
    IReadOnlyList<CommunityRoleRow> Rows);
