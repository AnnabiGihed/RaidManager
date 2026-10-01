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

    /// <summary>Gets the error returned when someone other than the Administrator tries to change the community's settings.</summary>
    public static readonly Error NotAdministrator = new("Community.NotAdministrator", "Only the community's Administrator can change its officer roles.");

    /// <summary>Gets the error returned when a Discord role can't be mapped: it doesn't exist, is @everyone, or belongs to another bot.</summary>
    public static readonly Error RoleNotMappable = new("Community.RoleNotMappable", "That Discord role can't give a RaidManager role.");
    #endregion Static Instances
}
