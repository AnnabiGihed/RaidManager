using RaidManager.Domain.Features.Shared.Identifiers;

namespace RaidManager.Domain.Features.Companions.Events;

/// <summary>Signals that a companion received its device token and may upload for its player.</summary>
/// <param name="CompanionId">The paired companion.</param>
/// <param name="UserId">The player it uploads for.</param>
/// <param name="Label">The computer's label.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: Lets later features react to a new computer, such as a notice on the website.
/// </remarks>
public sealed record CompanionPaired(CompanionId CompanionId, UserId UserId, string Label) : DomainEvent;
