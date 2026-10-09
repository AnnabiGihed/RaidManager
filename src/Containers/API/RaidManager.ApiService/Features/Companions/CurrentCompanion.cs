namespace RaidManager.ApiService.Features.Companions;

/// <summary>Describes the companion that called.</summary>
/// <param name="CompanionId">The companion's identifier.</param>
/// <param name="Label">The computer's label.</param>
/// <param name="SyncAgainRequestedAtUtc">When the player last asked the companion to send every character again, if they did (#615).</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: Is the answer of GET /companion/me, which a companion calls to check it is still paired.
/// </remarks>
public sealed record CurrentCompanion(Guid CompanionId, string Label, DateTimeOffset? SyncAgainRequestedAtUtc = null);
