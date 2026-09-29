using RaidManager.Domain.Features.Shared.Identifiers;

namespace RaidManager.Domain.Features.Characters.Events;

/// <summary>Signals that a Warmane character was first imported.</summary>
/// <param name="CharacterId">The character identifier.</param>
/// <param name="Realm">The Warmane realm name.</param>
/// <param name="Name">The character or community name.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-09-29<br/>
/// Purpose: Starts projections for a newly discovered Warmane character.
/// </remarks>
public sealed record CharacterImported(CharacterId CharacterId, string Realm, string Name) : DomainEvent;
