using RaidManager.Domain.Features.Shared.Identifiers;

namespace RaidManager.Domain.Features.Raids.Events;

/// <summary>Signals that a raid began accepting signups.</summary>
/// <param name="RaidId">The raid identifier.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-09-29<br/>
/// Purpose: Allows web and Discord signup surfaces to expose the raid.
/// </remarks>
public sealed record RaidOpenedForSignups(RaidId RaidId) : DomainEvent;
