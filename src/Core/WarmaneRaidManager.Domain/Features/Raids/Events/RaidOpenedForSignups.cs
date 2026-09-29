using WarmaneRaidManager.Domain.Features.Shared.Identifiers;

namespace WarmaneRaidManager.Domain.Features.Raids.Events;

/// <summary>Signals that a raid began accepting signups.</summary>
/// <param name="RaidId">The raid identifier.</param>
/// <remarks>
/// Author: Rodolphe Balay<br/>
/// Date: 2026-09-29<br/>
/// Purpose: Allows web and Discord signup surfaces to expose the raid.
/// </remarks>
public sealed record RaidOpenedForSignups(RaidId RaidId) : DomainEvent;
