using RaidManager.Domain.Features.Shared.Identifiers;

namespace RaidManager.Domain.Features.Raids.Events;

/// <summary>Signals that an officer changed a raid's details.</summary>
/// <param name="RaidId">The raid identifier.</param>
/// <param name="StartChanged">Whether the scheduled start changed.</param>
/// <param name="TargetsChanged">Whether the required instances and difficulties changed.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Lets readiness be recalculated when the start or the targets change, and the Discord post follow any change.
/// </remarks>
public sealed record RaidDetailsChanged(RaidId RaidId, bool StartChanged, bool TargetsChanged) : DomainEvent;
