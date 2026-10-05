using RaidManager.Application.Features.Characters.Queries.GetCharacterProfile;

namespace RaidManager.ApiService.Features.Characters;

/// <summary>Describes what RaidManager knows about one character, as the website receives it.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-05<br/>
/// Purpose: Keeps the wire format of the profile (story #19) separate from domain types: enums travel as names.
/// </remarks>
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
    IReadOnlyList<CharacterRaidSave> RaidSaves)
{
    #region Public Methods
    /// <summary>Maps the application response to the transport record.</summary>
    /// <param name="profile">The application response.</param>
    /// <returns>The transport record.</returns>
    public static CharacterProfile From(CharacterProfileResponse profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        return new CharacterProfile(
            profile.CharacterId,
            profile.Realm.ToString(),
            profile.Name,
            profile.Class.ToString(),
            profile.Race.ToString(),
            profile.Faction.ToString(),
            profile.Level,
            profile.GuildName,
            profile.Visibility.ToString(),
            profile.Sync.AddonAtUtc,
            profile.Sync.ArmoryAtUtc,
            profile.Sync.CompleteRaidSaveScanAtUtc,
            [.. profile.Professions.Select(profession => new CharacterProfession(profession.Name, profession.Rank, profession.MaxRank))],
            [.. profile.Loadouts.Select(CharacterLoadout.From)],
            [.. profile.RaidSaves.Select(save => new CharacterRaidSave(
                save.Instance.ToString(), save.Difficulty.ToString(), save.LockoutId, save.ResetsAtUtc, save.IsExtended))]);
    }
    #endregion Public Methods
}
