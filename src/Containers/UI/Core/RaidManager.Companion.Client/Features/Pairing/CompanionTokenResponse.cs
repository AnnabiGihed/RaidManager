namespace RaidManager.Companion.Client.Features.Pairing;

/// <summary>The answer of <c>POST /companion/pairings/token</c> once the code is confirmed.</summary>
/// <param name="CompanionId">The companion's id.</param>
/// <param name="DeviceToken">The device token.</param>
/// <param name="PlayerName">The player's name.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: Mirrors the API's answer contract of #382.
/// </remarks>
internal sealed record CompanionTokenResponse(Guid CompanionId, string DeviceToken, string PlayerName)
{
    #region Public Methods
    /// <summary>Describes the answer without its token, so the token never reaches a log.</summary>
    /// <returns>The companion's id.</returns>
    public override string ToString() => CompanionId.ToString();
    #endregion Public Methods
}
