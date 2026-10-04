namespace RaidManager.Companion.Client.Features.Pairing;

/// <summary>Describes this computer once a player confirmed its pairing.</summary>
/// <param name="CompanionId">The companion's id on RaidManager.</param>
/// <param name="PlayerName">The name of the player it uploads for.</param>
/// <param name="DeviceToken">The device token; kept only in the token store, never shown or logged.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: Carries the device token from the API to the token store, and the player's name to the window (ADR-0030).
/// </remarks>
public sealed record PairedCompanion(Guid CompanionId, string PlayerName, string DeviceToken)
{
    #region Public Methods
    /// <summary>Describes the companion without its token, so the token never reaches a log.</summary>
    /// <returns>The companion's id and player.</returns>
    public override string ToString() => $"{CompanionId} ({PlayerName})";
    #endregion Public Methods
}
