namespace RaidManager.ViewModels.Features.Companions;

/// <summary>Describes one of a player's companions, as the API lists it.</summary>
/// <param name="CompanionId">The companion's identifier.</param>
/// <param name="Label">The computer's label.</param>
/// <param name="PairedAtUtc">When it was paired.</param>
/// <param name="Status">Its status: <see cref="ActiveStatus"/>, <see cref="RevokedStatus"/> or <see cref="ExpiredStatus"/>.</param>
/// <param name="RevokedAtUtc">When it was revoked, if it was.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: Mirrors the API's transport record, so the website never depends on domain types.
/// </remarks>
public sealed record PairedCompanion(Guid CompanionId, string Label, DateTimeOffset PairedAtUtc, string Status, DateTimeOffset? RevokedAtUtc)
{
    #region Constants
    /// <summary>Defines the status of a companion that may upload.</summary>
    public const string ActiveStatus = "Active";

    /// <summary>Defines the status of a companion its player revoked.</summary>
    public const string RevokedStatus = "Revoked";

    /// <summary>Defines the status of a companion unused for 180 days.</summary>
    public const string ExpiredStatus = "Expired";
    #endregion Constants

    #region Properties
    /// <summary>Gets a value indicating whether the companion may still upload, so it can be revoked.</summary>
    public bool IsActive => Status == ActiveStatus;
    #endregion Properties
}
