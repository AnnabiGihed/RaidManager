using Pivot.Framework.Domain.Shared;

namespace RaidManager.Domain.Features.Identity.Errors;

/// <summary>Defines the anticipated failures about users.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Gives user failures stable, machine-readable codes returned through Pivot results.
/// </remarks>
public static class IdentityErrors
{
    #region Static Instances
    /// <summary>Gets the error returned when no user exists with the requested identifier.</summary>
    public static readonly Error UserNotFound = new("User.NotFound", "The user was not found.");
    #endregion Static Instances
}
