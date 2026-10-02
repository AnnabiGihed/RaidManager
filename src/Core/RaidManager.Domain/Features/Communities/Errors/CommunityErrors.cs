using Pivot.Framework.Domain.Shared;

namespace RaidManager.Domain.Features.Communities.Errors;

/// <summary>Defines the anticipated failures of community access.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Gives community access failures stable, machine-readable codes returned through Pivot results.
/// </remarks>
public static class CommunityErrors
{
    #region Static Instances
    /// <summary>Gets the error returned when no community exists with the requested identifier.</summary>
    public static readonly Error NotFound = new("Community.NotFound", "The community was not found.");

    /// <summary>Gets the error returned when the user is not in the community's Discord server.</summary>
    public static readonly Error NotAMember = new("Community.NotAMember", "You aren't a member of this community's Discord server.");

    /// <summary>Gets the error returned when the Discord server already links to a community.</summary>
    public static readonly Error AlreadyLinked = new("Community.AlreadyLinked", "This Discord server is already linked to a RaidManager community.");

    /// <summary>Gets the error returned when a Discord role can't be mapped: it doesn't exist, is @everyone, or belongs to another bot.</summary>
    public static readonly Error RoleNotMappable = new("Community.RoleNotMappable", "That Discord role can't give a RaidManager role.");

    /// <summary>Gets the error returned when someone who can't manage roles tries to change them.</summary>
    public static readonly Error NotRoleManager = new("Community.NotRoleManager", "Only the Administrator, or a member whose role manages roles, can change the community's roles.");

    /// <summary>Gets the error returned when a role manager tries to change a role that grants Manage community roles.</summary>
    public static readonly Error RoleLocked = new("Community.RoleLocked", "Only the Administrator can change a role that manages roles.");

    /// <summary>Gets the error returned when someone other than the Administrator tries to give Manage community roles.</summary>
    public static readonly Error CannotGrantRoleManagement = new("Community.CannotGrantRoleManagement", "Only the Administrator can let a role manage roles.");

    /// <summary>Gets the error returned for a role name that is blank or too long.</summary>
    public static readonly Error RoleNameInvalid = new("Community.RoleNameInvalid", "A role needs a name of at most 50 characters.");

    /// <summary>Gets the error returned for a role name another role of the community has.</summary>
    public static readonly Error RoleNameTaken = new("Community.RoleNameTaken", "The community already has a role with this name.");

    /// <summary>Gets the error returned for permissions RaidManager doesn't know.</summary>
    public static readonly Error RolePermissionsInvalid = new("Community.RolePermissionsInvalid", "A role can only allow the known permissions.");

    /// <summary>Gets the error for a role the community doesn't have.</summary>
    public static readonly Error RoleNotFound = new("Community.RoleNotFound", "The community has no such role.");
    #endregion Static Instances
}
