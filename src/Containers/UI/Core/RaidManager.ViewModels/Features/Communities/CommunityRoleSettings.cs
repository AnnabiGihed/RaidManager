namespace RaidManager.ViewModels.Features.Communities;

/// <summary>Describes a community's officer roles as the API returns them.</summary>
/// <param name="CommunityId">The community.</param>
/// <param name="ServerName">The Discord server's current name.</param>
/// <param name="CanEdit">Whether the signed-in user is the Administrator, who may change the mappings.</param>
/// <param name="MappableRoles">The server's roles that can be mapped, highest first.</param>
/// <param name="Rows">One row per RaidManager role, from Administrator to Member.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: The website's copy of the API's roles card contract.
/// </remarks>
public sealed record CommunityRoleSettings(
    Guid CommunityId,
    string ServerName,
    bool CanEdit,
    IReadOnlyList<DiscordRoleOption> MappableRoles,
    IReadOnlyList<CommunityRoleRow> Rows);
