using WarmaneRaidManager.Domain.Features.Shared.Identifiers;

namespace WarmaneRaidManager.Domain.Features.Characters.Events;

/// <summary>Signals that ownership of a character was verified.</summary>
/// <param name="CharacterId">The character identifier.</param>
/// <param name="OwnerId">The verified owner identifier.</param>
/// <remarks>
/// Author: Rodolphe Balay<br/>
/// Date: 2026-09-29<br/>
/// Purpose: Allows private synchronized character data to be trusted for raid planning.
/// </remarks>
public sealed record CharacterClaimed(CharacterId CharacterId, UserId OwnerId) : DomainEvent;
