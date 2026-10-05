namespace RaidManager.ViewModels.Features.Characters;

/// <summary>Describes what RaidManager knows about one character, as the API returns it.</summary>
/// <param name="CharacterId">The character identifier.</param>
/// <param name="Realm">The Warmane realm name.</param>
/// <param name="Name">The character name.</param>
/// <param name="Class">The class name.</param>
/// <param name="Race">The race name.</param>
/// <param name="Faction">The faction name.</param>
/// <param name="Level">The character level.</param>
/// <param name="GuildName">The guild, if any.</param>
/// <param name="Visibility"><c>Community</c> or <c>OfficersOnly</c>.</param>
/// <param name="AddonSynchronizedAtUtc">The latest successful addon sync, if any.</param>
/// <param name="ArmorySynchronizedAtUtc">The latest successful Warmane Armory sync, if any.</param>
/// <param name="CompleteRaidSaveScanAtUtc">The latest complete raid-save scan, if any.</param>
/// <param name="Professions">The synchronized professions, in the game's order.</param>
/// <param name="Loadouts">The loadouts, the primary one first.</param>
/// <param name="RaidSaves">The raid saves that haven't reset yet.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-05<br/>
/// Purpose: Carries board 2 of the character profile mockup (story #19).
/// </remarks>
public sealed record CharacterProfile(
    Guid CharacterId,
    string Realm,
    string Name,
    string Class,
    string Race,
    string Faction,
    int Level,
    string? GuildName,
    string Visibility,
    DateTimeOffset? AddonSynchronizedAtUtc,
    DateTimeOffset? ArmorySynchronizedAtUtc,
    DateTimeOffset? CompleteRaidSaveScanAtUtc,
    IReadOnlyList<CharacterProfession> Professions,
    IReadOnlyList<CharacterLoadout> Loadouts,
    IReadOnlyList<CharacterRaidSave> RaidSaves);
