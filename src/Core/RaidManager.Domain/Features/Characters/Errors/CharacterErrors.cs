using Pivot.Framework.Domain.Shared;

namespace RaidManager.Domain.Features.Characters.Errors;

/// <summary>Defines the anticipated business failures of the character aggregate.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-09-29<br/>
/// Purpose: Gives character claim failures stable, machine-readable codes returned through Pivot results.
/// </remarks>
public static class CharacterErrors
{
    #region Static Instances
    /// <summary>Gets the error returned when the user has no claim on the character.</summary>
    public static readonly Error ClaimNotFound = new("Character.Claim.NotFound", "No claim exists for this user and character.");

    /// <summary>Gets the error returned when a decision targets a claim that is no longer pending.</summary>
    public static readonly Error ClaimNotPending = new("Character.Claim.NotPending", "Only a pending character claim can be approved or rejected.");

    /// <summary>Gets the error returned when another identity already owns the character.</summary>
    public static readonly Error OwnedByAnotherUser = new("Character.Claim.OwnedByAnotherUser", "The character is already owned by another user.");

    /// <summary>Gets the error returned when a complete raid-save scan is older than the one already accepted.</summary>
    public static readonly Error RaidSaveScanOutdated = new("Character.RaidSaveScan.Outdated", "A newer complete raid-save scan was already recorded.");
    #endregion Static Instances
}
