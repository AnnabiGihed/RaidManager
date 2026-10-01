using RaidManager.Application.Features.Communities.Abstractions;

namespace RaidManager.Application.Features.Communities.Queries.GetCommunityRoleSettings;

/// <summary>Describes a community's officer roles as the roles card shows them.</summary>
/// <param name="CommunityId">The community.</param>
/// <param name="ServerName">The Discord server's current name.</param>
/// <param name="CanEdit">Whether the asking user is the Administrator, who may change the mappings.</param>
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
    IReadOnlyList<DiscordServerRole> MappableRoles,
    IReadOnlyList<CommunityRoleRowResponse> Rows);
