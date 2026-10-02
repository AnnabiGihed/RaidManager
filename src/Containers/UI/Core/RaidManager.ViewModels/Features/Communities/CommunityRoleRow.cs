namespace RaidManager.ViewModels.Features.Communities;

/// <summary>Reads one row of the roles card: a role, its Discord roles, what it allows and how many members have it.</summary>
/// <param name="Kind">The row's kind: <c>Administrator</c>, <c>Role</c> or <c>Member</c>.</param>
/// <param name="RoleId">The role's identifier; <see langword="null"/> for the Administrator and Member rows.</param>
/// <param name="Name">The role's name.</param>
/// <param name="Permissions">What the role allows, by permission name.</param>
/// <param name="DiscordRoles">The Discord roles mapped to it.</param>
/// <param name="Members">How many people get it; for Member, how many get no role.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: The API's roles card row, as the website reads it (story #308).
/// </remarks>
public sealed record CommunityRoleRow(
    string Kind,
    Guid? RoleId,
    string Name,
    IReadOnlyList<string> Permissions,
    IReadOnlyList<MappedDiscordRole> DiscordRoles,
    int Members)
{
    #region Constants
    /// <summary>Defines the kind of a row for one of the community's roles.</summary>
    public const string RoleKind = "Role";
    #endregion Constants

    #region Properties
    /// <summary>Gets the row's key on the page: the role's id, or the kind for the Administrator and Member rows.</summary>
    public string Key => RoleId?.ToString() ?? Kind;
    #endregion Properties
}
