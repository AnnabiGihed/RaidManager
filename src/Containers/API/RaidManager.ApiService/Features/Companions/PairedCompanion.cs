using RaidManager.Application.Features.Companions.Queries.GetCompanions;

namespace RaidManager.ApiService.Features.Companions;

/// <summary>Describes one of a player's companions.</summary>
/// <param name="CompanionId">The companion's identifier.</param>
/// <param name="Label">The computer's label.</param>
/// <param name="PairedAtUtc">When it was paired.</param>
/// <param name="LastUsedAtUtc">When it last called the API, to within an hour.</param>
/// <param name="Status">Its status as a name: <c>Active</c>, <c>Revoked</c> or <c>Expired</c>.</param>
/// <param name="RevokedAtUtc">When it was revoked, if it was.</param>
/// <param name="LastUploadAtUtc">When it last uploaded a character snapshot, or <see langword="null"/> before its first upload.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: Feeds the website's paired companions list (mockup states 2 and 4).
/// </remarks>
public sealed record PairedCompanion(Guid CompanionId, string Label, DateTimeOffset PairedAtUtc, DateTimeOffset LastUsedAtUtc, string Status, DateTimeOffset? RevokedAtUtc, DateTimeOffset? LastUploadAtUtc)
{
    #region Factory Methods
    /// <summary>Maps the application response.</summary>
    /// <param name="response">The application response.</param>
    /// <returns>The transport model.</returns>
    public static PairedCompanion From(CompanionResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);
        return new PairedCompanion(
            response.CompanionId, response.Label, response.PairedAtUtc, response.LastUsedAtUtc, response.Status.ToString(), response.RevokedAtUtc, response.LastUploadAtUtc);
    }
    #endregion Factory Methods
}
