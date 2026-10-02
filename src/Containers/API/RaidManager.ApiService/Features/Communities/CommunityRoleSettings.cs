using RaidManager.Application.Features.Communities.Abstractions;
using RaidManager.Application.Features.Communities.Queries.GetCommunityRoleSettings;

namespace RaidManager.ApiService.Features.Communities;

/// <summary>Describes a community's officer roles to the website.</summary>
/// <param name="CommunityId">The community.</param>
/// <param name="ServerName">The Discord server's current name.</param>
/// <param name="CanEdit">Whether the user may change roles: the Administrator, or a member whose role manages roles.</param>
/// <param name="CanGrantRoleManagement">Whether the user may let a role manage roles: only the Administrator.</param>
/// <param name="MappableRoles">The server's roles that can be mapped, highest first.</param>
/// <param name="Rows">One row per RaidManager role, from Administrator to Member.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: The API contract of the roles card; roles travel as names, like the other website contracts.
/// </remarks>
public sealed record CommunityRoleSettings(
    Guid CommunityId,
    string ServerName,
    bool CanEdit,
    bool CanGrantRoleManagement,
    IReadOnlyList<DiscordRoleOption> MappableRoles,
    IReadOnlyList<CommunityRoleRow> Rows)
{
    #region Public Methods
    /// <summary>Maps a query response to the contract.</summary>
    /// <param name="settings">The query response.</param>
    /// <returns>The contract.</returns>
    public static CommunityRoleSettings From(CommunityRoleSettingsResponse settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return new CommunityRoleSettings(
            settings.CommunityId,
            settings.ServerName,
            settings.CanEdit,
            settings.CanGrantRoleManagement,
            [.. settings.MappableRoles.Select(role => new DiscordRoleOption(role.Id, role.Name))],
            [.. settings.Rows.Select(row => new CommunityRoleRow(
                row.Kind,
                row.RoleId,
                row.Name,
                CommunityPermissionNames.Of(row.Permissions),
                [.. row.DiscordRoles.Select(role => new MappedDiscordRole(role.DiscordRoleId, role.Name, role.Missing))],
                row.CanChange,
                row.Members))]);
    }
    #endregion Public Methods
}
