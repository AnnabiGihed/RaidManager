using RaidManager.Domain.Features.Characters.Enums;
using RaidManager.Domain.Features.Shared.Enums;

namespace RaidManager.Application.Features.Characters.Queries.GetCharacterProfile;

/// <summary>Describes what RaidManager knows about one character, as its profile shows it.</summary>
/// <param name="CharacterId">The character.</param>
/// <param name="Realm">The Warmane realm.</param>
/// <param name="Name">The character name.</param>
/// <param name="Class">The WotLK class.</param>
/// <param name="Race">The WotLK race.</param>
/// <param name="Faction">The faction.</param>
/// <param name="Level">The character level.</param>
/// <param name="GuildName">The guild, if any.</param>
/// <param name="Visibility">Who in the community may see the profile.</param>
/// <param name="Sync">When each source last succeeded.</param>
/// <param name="Professions">The synchronized professions, in the game's order.</param>
/// <param name="Loadouts">The loadouts, the primary one first.</param>
/// <param name="RaidSaves">The raid saves of the latest complete scan that haven't reset yet.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-05<br/>
/// Purpose: Carries board 2 of the character profile mockup (story #19) without exposing the aggregate.
/// </remarks>
public sealed record CharacterProfileResponse(
    Guid CharacterId,
    WarmaneRealm Realm,
    string Name,
    WowClass Class,
    WowRace Race,
    Faction Faction,
    int Level,
    string? GuildName,
    CharacterVisibility Visibility,
    ProfileSyncResponse Sync,
    IReadOnlyList<ProfileProfessionResponse> Professions,
    IReadOnlyList<ProfileLoadoutResponse> Loadouts,
    IReadOnlyList<ProfileRaidSaveResponse> RaidSaves);
