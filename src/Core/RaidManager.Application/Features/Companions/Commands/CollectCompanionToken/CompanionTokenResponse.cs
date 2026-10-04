namespace RaidManager.Application.Features.Companions.Commands.CollectCompanionToken;

/// <summary>Carries a newly paired companion's credential.</summary>
/// <param name="CompanionId">The companion's identifier.</param>
/// <param name="DeviceToken">The device token to send as a bearer token; the API keeps only its hash.</param>
/// <param name="PlayerName">The display name of the player it is paired with, which the companion shows.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: Hands the companion its token once (ADR-0030).
/// </remarks>
public sealed record CompanionTokenResponse(Guid CompanionId, string DeviceToken, string PlayerName);
