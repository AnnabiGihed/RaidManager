using System.Globalization;
using RaidManager.Domain.Features.Characters.Enums;
using RaidManager.Domain.Features.Characters.ValueObjects;
using RaidManager.Domain.Features.Shared.Enums;

namespace RaidManager.Application.Features.Characters.Commands.ImportCharacterSnapshot;

/// <summary>Turns the raw values of an addon snapshot into the Character aggregate's values.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-05<br/>
/// Purpose: Holds the one translation of the addon contract (<c>docs/reference/addon-savedvariables.md</c>, schema 1) to
/// the domain: game tokens to enums, inventory slots to equipment slots, saved instances to WotLK raid saves, and talent
/// groups to loadouts (owner decision on #384). Only an observed section becomes a value; an unavailable one stays
/// <see langword="null"/> so it never erases a known fact.
/// </remarks>
internal static class AddonSnapshotMapper
{
    #region Constants
    /// <summary>Defines the status of a section the game answered.</summary>
    public const string Observed = "observed";

    /// <summary>Defines the status of a section or slot the game didn't answer.</summary>
    public const string Unavailable = "unavailable";

    /// <summary>Defines the glyph type of a major glyph.</summary>
    private const int MajorGlyph = 1;

    /// <summary>Defines the glyph type of a minor glyph.</summary>
    private const int MinorGlyph = 2;

    /// <summary>Defines the first heroic raw difficulty of a raid with a 10-player and a 25-player size.</summary>
    private const int FirstHeroicDifficulty = 3;

    /// <summary>Defines the size of a 25-player raid.</summary>
    private const int TwentyFivePlayers = 25;

    /// <summary>Defines the size of a 10-player raid.</summary>
    private const int TenPlayers = 10;

    /// <summary>Defines the number of bits in the lower part of a lockout id.</summary>
    private const int LockoutIdLowerBits = 32;
    #endregion Constants

    #region Static Instances
    /// <summary>Stores the class of each class token <c>UnitClass</c> returns.</summary>
    private static readonly Dictionary<string, WowClass> Classes = new(StringComparer.Ordinal)
    {
        ["DEATHKNIGHT"] = WowClass.DeathKnight,
        ["DRUID"] = WowClass.Druid,
        ["HUNTER"] = WowClass.Hunter,
        ["MAGE"] = WowClass.Mage,
        ["PALADIN"] = WowClass.Paladin,
        ["PRIEST"] = WowClass.Priest,
        ["ROGUE"] = WowClass.Rogue,
        ["SHAMAN"] = WowClass.Shaman,
        ["WARLOCK"] = WowClass.Warlock,
        ["WARRIOR"] = WowClass.Warrior,
    };

    /// <summary>Stores the race of each race token <c>UnitRace</c> returns; the game calls the Undead <c>Scourge</c>.</summary>
    private static readonly Dictionary<string, WowRace> Races = new(StringComparer.Ordinal)
    {
        ["Human"] = WowRace.Human,
        ["Dwarf"] = WowRace.Dwarf,
        ["NightElf"] = WowRace.NightElf,
        ["Gnome"] = WowRace.Gnome,
        ["Draenei"] = WowRace.Draenei,
        ["Orc"] = WowRace.Orc,
        ["Scourge"] = WowRace.Undead,
        ["Tauren"] = WowRace.Tauren,
        ["Troll"] = WowRace.Troll,
        ["BloodElf"] = WowRace.BloodElf,
    };

    /// <summary>Stores the faction of each faction name the game returns.</summary>
    private static readonly Dictionary<string, Faction> Factions = new(StringComparer.Ordinal)
    {
        ["Alliance"] = Faction.Alliance,
        ["Horde"] = Faction.Horde,
    };

    /// <summary>Stores the equipment slot of each inventory slot, 1 to 19, in the game's numbering.</summary>
    private static readonly EquipmentSlot[] InventorySlots =
    [
        EquipmentSlot.Head,
        EquipmentSlot.Neck,
        EquipmentSlot.Shoulders,
        EquipmentSlot.Shirt,
        EquipmentSlot.Chest,
        EquipmentSlot.Waist,
        EquipmentSlot.Legs,
        EquipmentSlot.Feet,
        EquipmentSlot.Wrists,
        EquipmentSlot.Hands,
        EquipmentSlot.FingerOne,
        EquipmentSlot.FingerTwo,
        EquipmentSlot.TrinketOne,
        EquipmentSlot.TrinketTwo,
        EquipmentSlot.Back,
        EquipmentSlot.MainHand,
        EquipmentSlot.OffHand,
        EquipmentSlot.RangedOrRelic,
        EquipmentSlot.Tabard,
    ];

    /// <summary>Stores the WotLK raid of each English instance name; other raids and dungeons aren't raid saves RaidManager plans.</summary>
    private static readonly Dictionary<string, RaidInstance> Raids = new(StringComparer.Ordinal)
    {
        ["Naxxramas"] = RaidInstance.Naxxramas,
        ["The Obsidian Sanctum"] = RaidInstance.ObsidianSanctum,
        ["The Eye of Eternity"] = RaidInstance.EyeOfEternity,
        ["Vault of Archavon"] = RaidInstance.VaultOfArchavon,
        ["Ulduar"] = RaidInstance.Ulduar,
        ["Trial of the Crusader"] = RaidInstance.TrialOfTheCrusader,
        ["Trial of the Grand Crusader"] = RaidInstance.TrialOfTheCrusader,
        ["Icecrown Citadel"] = RaidInstance.IcecrownCitadel,
        ["The Ruby Sanctum"] = RaidInstance.RubySanctum,
    };

    /// <summary>Stores the client locales whose instance names are the English ones RaidManager reads.</summary>
    private static readonly HashSet<string> EnglishLocales = new(StringComparer.Ordinal) { "enUS", "enGB" };
    #endregion Static Instances

    #region Public Methods
    /// <summary>Gets the realm a snapshot names.</summary>
    /// <param name="text">The realm name, such as <c>Icecrown</c>.</param>
    /// <param name="realm">The realm, when known.</param>
    /// <returns>Whether RaidManager knows the realm.</returns>
    public static bool TryParseRealm(string? text, out WarmaneRealm realm)
    {
        realm = default;
        return !string.IsNullOrWhiteSpace(text)
            && !text.Any(char.IsDigit)
            && Enum.TryParse(text.Trim(), ignoreCase: true, out realm)
            && Enum.IsDefined(realm);
    }

    /// <summary>Gets the realm of a validated snapshot.</summary>
    /// <param name="text">A realm name <see cref="TryParseRealm"/> accepted.</param>
    /// <returns>The realm.</returns>
    public static WarmaneRealm ParseRealm(string text) => Enum.Parse<WarmaneRealm>(text.Trim(), ignoreCase: true);

    /// <summary>Gets whether a class token is a WotLK class.</summary>
    /// <param name="token">The token.</param>
    /// <returns>Whether it is known.</returns>
    public static bool IsKnownClass(string? token) => token is not null && Classes.ContainsKey(token);

    /// <summary>Gets whether a race token is a WotLK race.</summary>
    /// <param name="token">The token.</param>
    /// <returns>Whether it is known.</returns>
    public static bool IsKnownRace(string? token) => token is not null && Races.ContainsKey(token);

    /// <summary>Gets whether a faction name is a WotLK faction.</summary>
    /// <param name="name">The faction name.</param>
    /// <returns>Whether it is known.</returns>
    public static bool IsKnownFaction(string? name) => name is not null && Factions.ContainsKey(name);

    /// <summary>Gets the identity of a snapshot.</summary>
    /// <param name="identity">The identity section.</param>
    /// <returns>The identity, or <see langword="null"/> when it wasn't observed.</returns>
    public static AddonIdentity? ToIdentity(SnapshotIdentity? identity) =>
        IsObserved(identity?.Status)
            ? new AddonIdentity(Classes[identity!.Class!], Races[identity.Race!], Factions[identity.Faction!], identity.Level!.Value)
            : null;

    /// <summary>Gets the domain snapshot of a validated character snapshot.</summary>
    /// <param name="character">The character snapshot.</param>
    /// <returns>The domain snapshot.</returns>
    public static AddonSnapshot ToAddonSnapshot(SnapshotCharacter character) =>
        new(
            Instant(character.CapturedAt),
            ToIdentity(character.Identity),
            ToGuild(character.Guild),
            ToProfessions(character.Professions),
            ToTalents(character.Talents),
            ToGear(character.Equipped),
            ToRaidSaves(character.Lockouts, character.Client));

    /// <summary>Gets whether a section's status says the game answered.</summary>
    /// <param name="status">The status.</param>
    /// <returns>Whether the section is observed.</returns>
    public static bool IsObserved(string? status) => string.Equals(status, Observed, StringComparison.Ordinal);

    /// <summary>Gets the instant of a time the addon wrote.</summary>
    /// <param name="seconds">Seconds since 1970 (UTC).</param>
    /// <returns>The UTC instant.</returns>
    public static DateTimeOffset Instant(long seconds) => DateTimeOffset.FromUnixTimeSeconds(seconds);
    #endregion Public Methods

    #region Private Helpers
    /// <summary>Gets the guild of a snapshot.</summary>
    /// <param name="guild">The guild section.</param>
    /// <returns>The guild, or <see langword="null"/> when it wasn't observed.</returns>
    private static AddonGuild? ToGuild(SnapshotGuild? guild) =>
        IsObserved(guild?.Status) ? new AddonGuild(guild!.InGuild == true ? guild.Name : null) : null;

    /// <summary>Gets the professions of a snapshot.</summary>
    /// <param name="professions">The professions section.</param>
    /// <returns>The professions, or <see langword="null"/> when they weren't observed.</returns>
    private static AddonProfessions? ToProfessions(SnapshotProfessions? professions) =>
        IsObserved(professions?.Status)
            ? new AddonProfessions(
                [.. professions!.Items!.Select(profession => new Profession(profession.Name!.Trim(), profession.Rank, profession.MaxRank))],
                Instant(professions.ObservedAt!.Value))
            : null;

    /// <summary>Gets the talent groups of a snapshot.</summary>
    /// <param name="talents">The talents section.</param>
    /// <returns>The talent groups, or <see langword="null"/> when they weren't observed.</returns>
    private static AddonTalents? ToTalents(SnapshotTalents? talents) =>
        IsObserved(talents?.Status)
            ? new AddonTalents(talents!.ActiveGroup!.Value, [.. talents.Groups!.Select(group => new AddonTalentGroup(group.Group, ToConfiguration(group)))])
            : null;

    /// <summary>Gets the talents and glyphs of one talent group.</summary>
    /// <param name="group">The talent group.</param>
    /// <returns>The configuration, named after its tree with the most points spent.</returns>
    private static TalentConfiguration ToConfiguration(SnapshotTalentGroup group)
    {
        var tabs = group.Tabs!;
        var points = tabs.Select(tab => tab.PointsSpent).ToArray();
        var mainTree = Array.IndexOf(points, points.Max());
        var glyphs = (group.Glyphs ?? []).Where(glyph => glyph.Enabled == true && glyph.Empty != true && glyph.SpellId > 0).ToList();
        return new TalentConfiguration(
            tabs[mainTree].Name!.Trim(),
            points[0],
            points[1],
            points[2],
            string.Join('-', tabs.Select(tab => tab.Ranks)),
            [.. glyphs.Where(glyph => glyph.Type == MajorGlyph).Select(glyph => glyph.SpellId!.Value)],
            [.. glyphs.Where(glyph => glyph.Type == MinorGlyph).Select(glyph => glyph.SpellId!.Value)]);
    }

    /// <summary>Gets the gear worn at the capture.</summary>
    /// <param name="equipped">The equipped section.</param>
    /// <returns>The gear, or <see langword="null"/> when it wasn't observed.</returns>
    private static AddonGear? ToGear(SnapshotEquipped? equipped)
    {
        if (!IsObserved(equipped?.Status))
        {
            return null;
        }

        var slots = equipped!.Slots!;
        var items = slots
            .Where(slot => slot.Empty != true && !IsUnavailable(slot.Status))
            .Select(slot => new GearItem(InventorySlots[slot.Slot - 1], slot.ItemId!.Value, null, slot.ItemString!, null))
            .ToList();
        var unread = slots.Where(slot => IsUnavailable(slot.Status)).Select(slot => InventorySlots[slot.Slot - 1]).ToList();
        return new AddonGear(items, unread);
    }

    /// <summary>Gets the WotLK raid saves of a saved-instance scan.</summary>
    /// <param name="lockouts">The lockouts section.</param>
    /// <param name="client">The client, whose locale tells whether instance names can be read.</param>
    /// <returns>The scan, or <see langword="null"/> when it wasn't observed.</returns>
    /// <remarks>
    /// Only current saves of the WotLK raids RaidManager plans are kept. A client in another language names instances in
    /// that language, so its scan is kept as incomplete: it can't prove that a save is gone.
    /// </remarks>
    private static AddonRaidSaves? ToRaidSaves(SnapshotLockouts? lockouts, SnapshotClient? client)
    {
        if (!IsObserved(lockouts?.Status))
        {
            return null;
        }

        var observedAtUtc = Instant(lockouts!.ObservedAt!.Value);
        var isEnglish = client?.Locale is { } locale && EnglishLocales.Contains(locale);
        var saves = lockouts.Items!
            .Where(item => item.IsRaid && item.Locked && item.Name is not null && Raids.ContainsKey(item.Name))
            .Select(item => ToRaidLockout(item, observedAtUtc))
            .OfType<RaidLockout>()
            .ToList();
        return new AddonRaidSaves(lockouts.Complete == true && isEnglish, saves, observedAtUtc);
    }

    /// <summary>Gets the raid save of one saved raid.</summary>
    /// <param name="item">The saved raid.</param>
    /// <param name="observedAtUtc">The scan's observation, from which the reset is counted.</param>
    /// <returns>The raid save, or <see langword="null"/> when its size is neither 10 nor 25 players.</returns>
    private static RaidLockout? ToRaidLockout(SnapshotLockout item, DateTimeOffset observedAtUtc)
    {
        var isHeroic = item.Difficulty >= FirstHeroicDifficulty;
        RaidDifficulty? difficulty = item.MaxPlayers switch
        {
            TenPlayers => isHeroic ? RaidDifficulty.TenPlayerHeroic : RaidDifficulty.TenPlayer,
            TwentyFivePlayers => isHeroic ? RaidDifficulty.TwentyFivePlayerHeroic : RaidDifficulty.TwentyFivePlayer,
            _ => null,
        };
        if (difficulty is null)
        {
            return null;
        }

        var lockoutId = ((ulong)item.IdMostSig << LockoutIdLowerBits) | (uint)item.LockoutId;
        return new RaidLockout(
            Raids[item.Name!],
            difficulty.Value,
            lockoutId.ToString(CultureInfo.InvariantCulture),
            observedAtUtc.AddSeconds(item.ResetSeconds),
            item.Extended);
    }

    /// <summary>Gets whether a slot's status says the game didn't describe its item.</summary>
    /// <param name="status">The status.</param>
    /// <returns>Whether the slot is unavailable.</returns>
    private static bool IsUnavailable(string? status) => string.Equals(status, Unavailable, StringComparison.Ordinal);
    #endregion Private Helpers
}
