using RaidManager.Domain.Features.Characters.Enums;
using RaidManager.Domain.Features.Shared.Enums;

namespace RaidManager.Application.Features.Characters.Queries.GetMyCharacters;

/// <summary>Describes one of the player's characters in the My characters list.</summary>
/// <param name="CharacterId">The character.</param>
/// <param name="Realm">The Warmane realm.</param>
/// <param name="Name">The character name.</param>
/// <param name="Class">The WotLK class.</param>
/// <param name="Level">The character level.</param>
/// <param name="PrimaryLoadout">The primary loadout, or <see langword="null"/> before any loadout was synchronized.</param>
/// <param name="CurrentRaidSaveCount">The raid saves of the latest complete scan that haven't reset yet.</param>
/// <param name="LastSynchronizedAtUtc">The latest successful sync from any source, or <see langword="null"/> before the first.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-05<br/>
/// Purpose: Carries one row of board 1 without exposing the aggregate.
/// </remarks>
public sealed record CharacterSummaryResponse(
    Guid CharacterId,
    WarmaneRealm Realm,
    string Name,
    WowClass Class,
    int Level,
    LoadoutSummaryResponse? PrimaryLoadout,
    int CurrentRaidSaveCount,
    DateTimeOffset? LastSynchronizedAtUtc);
