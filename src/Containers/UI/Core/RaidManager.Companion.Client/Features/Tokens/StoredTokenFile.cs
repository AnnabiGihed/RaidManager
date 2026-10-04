namespace RaidManager.Companion.Client.Features.Tokens;

/// <summary>The content of the token file.</summary>
/// <param name="CompanionId">The companion's id.</param>
/// <param name="PlayerName">The player's name.</param>
/// <param name="ProtectedToken">The device token, encrypted by the protector and encoded in Base64.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: Holds the companion id, the player's name and the protected token, as ADR-0032 lists.
/// </remarks>
internal sealed record StoredTokenFile(Guid CompanionId, string PlayerName, string ProtectedToken);
