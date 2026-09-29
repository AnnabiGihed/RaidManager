using WarmaneRaidManager.Domain.Features.Shared.Identifiers;

namespace WarmaneRaidManager.Domain.Features.Raids.Events;

/// <summary>Signals that a user submitted or refreshed raid availability.</summary>
/// <param name="RaidId">The raid identifier.</param>
/// <param name="SignupId">The raid signup identifier.</param>
/// <param name="UserId">The application user identifier.</param>
/// <remarks>
/// Author: Rodolphe Balay<br/>
/// Date: 2026-09-29<br/>
/// Purpose: Updates signup projections and officer views.
/// </remarks>
public sealed record RaidSignupSubmitted(RaidId RaidId, RaidSignupId SignupId, UserId UserId) : DomainEvent;
