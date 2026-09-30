using RaidManager.Domain.Features.Characters.Enums;
using RaidManager.Domain.Features.Shared.Enums;

namespace RaidManager.Application.Features.Characters.Queries.GetPendingCharacterClaims;

/// <summary>Describes one character awaiting the player's decision.</summary>
/// <param name="CharacterId">The claimed character.</param>
/// <param name="Realm">The Warmane realm.</param>
/// <param name="Name">The character name.</param>
/// <param name="Class">The WotLK class.</param>
/// <param name="Race">The WotLK race.</param>
/// <param name="Level">The character level.</param>
/// <param name="ClaimState">Pending, or conflict when another player already owns the character.</param>
/// <param name="RequestedAtUtc">When the player's companion discovered the character.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-09-30<br/>
/// Purpose: Carries what a player needs to recognize a character and decide on it, without exposing the aggregate.
/// </remarks>
public sealed record PendingCharacterClaimResponse(
    Guid CharacterId,
    WarmaneRealm Realm,
    string Name,
    WowClass Class,
    WowRace Race,
    int Level,
    CharacterClaimState ClaimState,
    DateTimeOffset RequestedAtUtc);
