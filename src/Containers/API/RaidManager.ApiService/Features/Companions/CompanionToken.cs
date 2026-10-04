using RaidManager.Application.Features.Companions.Commands.CollectCompanionToken;

namespace RaidManager.ApiService.Features.Companions;

/// <summary>Carries the newly paired companion's credential, given once.</summary>
/// <param name="CompanionId">The companion's identifier.</param>
/// <param name="DeviceToken">The device token, sent as <c>Authorization: Bearer</c> on companion routes.</param>
/// <param name="PlayerName">The display name of the player it is paired with.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: Is the answer of POST /companion/pairings/token once the player confirmed the code.
/// </remarks>
public sealed record CompanionToken(Guid CompanionId, string DeviceToken, string PlayerName)
{
    #region Factory Methods
    /// <summary>Maps the application response.</summary>
    /// <param name="response">The application response.</param>
    /// <returns>The transport model.</returns>
    public static CompanionToken From(CompanionTokenResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);
        return new CompanionToken(response.CompanionId, response.DeviceToken, response.PlayerName);
    }
    #endregion Factory Methods
}
