using RaidManager.Domain.Features.Communities.Enums;

namespace RaidManager.Application.Features.Communities.Abstractions;

/// <summary>Converts a role's permissions to and from the names the API uses.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-02<br/>
/// Purpose: The API speaks in permission names, such as <c>ManageRaids</c>, so the stored flag values never leak into its
/// contract (ADR-0024).
/// </remarks>
public static class CommunityPermissionNames
{
    #region Fields
    /// <summary>Stores each single permission, in the order the API lists them.</summary>
    private static readonly CommunityPermissions[] Known =
    [
        CommunityPermissions.ManageRaids,
        CommunityPermissions.BuildRosters,
        CommunityPermissions.RunRaidNight,
        CommunityPermissions.ReviewConflicts,
        CommunityPermissions.ManageCommunityRoles,
    ];
    #endregion Fields

    #region Public Methods
    /// <summary>Names each permission in a set.</summary>
    /// <param name="permissions">The permissions.</param>
    /// <returns>The names, in the API's order; empty for none.</returns>
    public static IReadOnlyList<string> Of(CommunityPermissions permissions) =>
        [.. Known.Where(permission => permissions.HasFlag(permission)).Select(permission => permission.ToString())];

    /// <summary>Tells whether a name is one of the permissions a role can allow.</summary>
    /// <param name="name">The name.</param>
    /// <returns><see langword="true"/> for a known permission's exact name.</returns>
    public static bool IsKnown(string name) => Known.Any(permission => string.Equals(permission.ToString(), name, StringComparison.Ordinal));

    /// <summary>Combines permission names into a set; the names have been checked with <see cref="IsKnown"/>.</summary>
    /// <param name="names">The names.</param>
    /// <returns>The permissions.</returns>
    public static CommunityPermissions Parse(IEnumerable<string> names) =>
        names.Aggregate(CommunityPermissions.None, (permissions, name) => permissions | Enum.Parse<CommunityPermissions>(name));
    #endregion Public Methods
}
