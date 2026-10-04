namespace RaidManager.Companion.Client.Features.Pairing;

/// <summary>The body of <c>POST /companion/pairings/token</c>.</summary>
/// <param name="DeviceCode">The device code.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: Mirrors the API's request contract of #382.
/// </remarks>
internal sealed record CollectTokenRequest(string DeviceCode)
{
    #region Public Methods
    /// <summary>Describes the request without its device code, so the code never reaches a log.</summary>
    /// <returns>The request's name.</returns>
    public override string ToString() => nameof(CollectTokenRequest);
    #endregion Public Methods
}
