namespace RaidManager.Application.Features.Companions.Commands.AuthenticateCompanion;

/// <summary>Identifies the companion and the player a request comes from.</summary>
/// <param name="CompanionId">The companion.</param>
/// <param name="UserId">The player it uploads for.</param>
/// <param name="Label">The computer's label.</param>
/// <param name="SyncAgainRequestedAtUtc">When the player last asked the companion to send every character again, if they did.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: Becomes the caller's identity on companion routes.
/// </remarks>
public sealed record AuthenticatedCompanionResponse(Guid CompanionId, Guid UserId, string Label, DateTimeOffset? SyncAgainRequestedAtUtc = null);
