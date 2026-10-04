using RaidManager.Domain.Features.Companions.Enums;

namespace RaidManager.Application.Features.Companions.Queries.GetCompanions;

/// <summary>Describes one of a player's companions.</summary>
/// <param name="CompanionId">The companion's identifier.</param>
/// <param name="Label">The computer's label.</param>
/// <param name="PairedAtUtc">When it was paired.</param>
/// <param name="LastUsedAtUtc">When it last called the API, to within an hour.</param>
/// <param name="Status">Whether it is active, revoked or expired.</param>
/// <param name="RevokedAtUtc">When it was revoked, if it was.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: Gives the website each row of the paired companions list; never the token or its hash.
/// </remarks>
public sealed record CompanionResponse(Guid CompanionId, string Label, DateTimeOffset PairedAtUtc, DateTimeOffset LastUsedAtUtc, CompanionStatus Status, DateTimeOffset? RevokedAtUtc);
