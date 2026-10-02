namespace RaidManager.ViewModels.Features.Communities;

/// <summary>Describes one permission a role can allow, as the role form and the roles card word it.</summary>
/// <param name="Name">The API name, such as <c>ManageRaids</c>.</param>
/// <param name="Title">The form's title, such as "Manage raids".</param>
/// <param name="Detail">The form's line saying what it allows.</param>
/// <param name="Word">The short word the roles card lists, such as "raids".</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-02<br/>
/// Purpose: The wording of the five permissions of story #308, in one place (boards 9 and 10).
/// </remarks>
public sealed record RolePermissionOption(string Name, string Title, string Detail, string Word)
{
    #region Constants
    /// <summary>Defines the API name of the permission only the Administrator can give.</summary>
    public const string ManageCommunityRoles = "ManageCommunityRoles";
    #endregion Constants

    #region Properties
    /// <summary>Gets the five permissions, in the form's order.</summary>
    public static IReadOnlyList<RolePermissionOption> All { get; } =
    [
        new("ManageRaids", "Manage raids", "Create and edit raids, templates and recurrence; lock or reopen signups.", "raids"),
        new("BuildRosters", "Build rosters", "Select and swap participants, record exceptions, publish, set boss assignments.", "rosters"),
        new("RunRaidNight", "Run raid night", "Record attendance and export the roster to the addon.", "raid night"),
        new("ReviewConflicts", "Review conflicts", "Decide character claims another player already owns.", "conflicts"),
        new(ManageCommunityRoles, "Manage community roles", "Create, edit and delete roles and map Discord roles to them.", "roles"),
    ];
    #endregion Properties
}
