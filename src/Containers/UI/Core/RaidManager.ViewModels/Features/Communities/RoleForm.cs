namespace RaidManager.ViewModels.Features.Communities;

/// <summary>Holds the role form while it is open: the role's name and the permissions ticked.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-02<br/>
/// Purpose: The state of the create and edit dialogs (boards 10 and 11), changed by the page as the user types and ticks.
/// </remarks>
public sealed class RoleForm
{
    #region Constants
    /// <summary>Defines the longest role name.</summary>
    public const int MaximumNameLength = 50;
    #endregion Constants

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="RoleForm"/> class.</summary>
    /// <param name="roleId">The role being edited; <see langword="null"/> for a new role.</param>
    /// <param name="name">The role's current name.</param>
    /// <param name="permissions">The permissions ticked, by API name.</param>
    /// <param name="canGrantRoleManagement">Whether the user may tick Manage community roles.</param>
    public RoleForm(Guid? roleId, string name, IEnumerable<string> permissions, bool canGrantRoleManagement)
    {
        RoleId = roleId;
        OriginalName = name;
        Name = name;
        Permissions = [.. permissions];
        CanGrantRoleManagement = canGrantRoleManagement;
    }
    #endregion Constructors

    #region Properties
    /// <summary>Gets the role being edited; <see langword="null"/> for a new role.</summary>
    public Guid? RoleId { get; }

    /// <summary>Gets the role's name when the form opened.</summary>
    public string OriginalName { get; }

    /// <summary>Gets or sets the name typed.</summary>
    public string Name { get; set; }

    /// <summary>Gets the permissions ticked, by API name.</summary>
    public HashSet<string> Permissions { get; }

    /// <summary>Gets a value indicating whether the user may tick Manage community roles: only the Administrator.</summary>
    public bool CanGrantRoleManagement { get; }

    /// <summary>Gets or sets why the last save didn't happen, if it didn't.</summary>
    public string? Problem { get; set; }

    /// <summary>Gets a value indicating whether an existing role is being edited.</summary>
    public bool IsEditing => RoleId is not null;

    /// <summary>Gets the dialog's title.</summary>
    public string Title => IsEditing ? $"Edit {OriginalName}" : "Create a role";

    /// <summary>Gets the save button's label.</summary>
    public string SaveLabel => IsEditing ? "Save" : "Create role";

    /// <summary>Gets a value indicating whether the name can be saved: not blank, at most 50 characters.</summary>
    public bool CanSave => Name.Trim().Length is > 0 and <= MaximumNameLength;
    #endregion Properties

    #region Public Methods
    /// <summary>Ticks or clears a permission; Manage community roles stays as it is for someone who can't give it.</summary>
    /// <param name="name">The permission's API name.</param>
    /// <param name="allowed">Whether it is ticked.</param>
    public void Set(string name, bool allowed)
    {
        if (name == RolePermissionOption.ManageCommunityRoles && !CanGrantRoleManagement)
        {
            return;
        }

        if (allowed)
        {
            Permissions.Add(name);
        }
        else
        {
            Permissions.Remove(name);
        }
    }
    #endregion Public Methods
}
