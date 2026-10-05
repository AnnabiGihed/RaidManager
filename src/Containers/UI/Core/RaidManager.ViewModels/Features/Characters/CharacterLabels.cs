using System.Globalization;
using System.Text.RegularExpressions;

namespace RaidManager.ViewModels.Features.Characters;

/// <summary>Turns the names the API sends into the words the character pages show.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-05<br/>
/// Purpose: Keeps the wording of the character profile mockup (story #19) in one place: classes, roles, raids, difficulties, slots, sources and item names.
/// </remarks>
public static partial class CharacterLabels
{
    #region Constants
    /// <summary>Defines what a value RaidManager doesn't have shows: a dash (owner decision on #384).</summary>
    private const string MissingValue = "—";
    #endregion Constants

    #region Fields
    /// <summary>Stores the role names as the profile writes them.</summary>
    private static readonly Dictionary<string, string> Roles = new(StringComparer.Ordinal)
    {
        ["Tank"] = "Tank",
        ["Healer"] = "Healer",
        ["MeleeDamage"] = "Melee damage",
        ["RangedDamage"] = "Ranged damage",
    };

    /// <summary>Stores the raid names as the game writes them.</summary>
    private static readonly Dictionary<string, string> Instances = new(StringComparer.Ordinal)
    {
        ["Naxxramas"] = "Naxxramas",
        ["ObsidianSanctum"] = "Obsidian Sanctum",
        ["EyeOfEternity"] = "Eye of Eternity",
        ["VaultOfArchavon"] = "Vault of Archavon",
        ["Ulduar"] = "Ulduar",
        ["TrialOfTheCrusader"] = "Trial of the Crusader",
        ["IcecrownCitadel"] = "Icecrown Citadel",
        ["RubySanctum"] = "Ruby Sanctum",
    };

    /// <summary>Stores the raid sizes and difficulties as the profile writes them.</summary>
    private static readonly Dictionary<string, string> Difficulties = new(StringComparer.Ordinal)
    {
        ["TenPlayer"] = "10 players",
        ["TenPlayerHeroic"] = "10 players, heroic",
        ["TwentyFivePlayer"] = "25 players",
        ["TwentyFivePlayerHeroic"] = "25 players, heroic",
    };

    /// <summary>Stores the equipment slot names as the profile writes them.</summary>
    private static readonly Dictionary<string, string> Slots = new(StringComparer.Ordinal)
    {
        ["Shoulders"] = "Shoulders",
        ["FingerOne"] = "Finger",
        ["FingerTwo"] = "Finger",
        ["TrinketOne"] = "Trinket",
        ["TrinketTwo"] = "Trinket",
        ["MainHand"] = "Main hand",
        ["OffHand"] = "Off hand",
        ["RangedOrRelic"] = "Ranged",
    };

    /// <summary>Stores how each data source is named after a fact.</summary>
    private static readonly Dictionary<string, string> Sources = new(StringComparer.Ordinal)
    {
        ["WowAddon"] = "from the addon",
        ["WarmaneArmory"] = "from the Warmane Armory",
        ["Derived"] = "calculated",
    };
    #endregion Fields

    #region Public Methods
    /// <summary>Gets a class name with its words apart.</summary>
    /// <param name="className">The class name, for example <c>DeathKnight</c>.</param>
    /// <returns>For example <c>Death Knight</c>.</returns>
    public static string Class(string className) => CapitalWords().Replace(className, " $1").Trim();

    /// <summary>Gets a role as the profile writes it.</summary>
    /// <param name="role">The role name, for example <c>MeleeDamage</c>.</param>
    /// <returns>For example <c>Melee damage</c>.</returns>
    public static string Role(string role) => Roles.GetValueOrDefault(role, role);

    /// <summary>Gets a raid name as the game writes it.</summary>
    /// <param name="instance">The instance name, for example <c>IcecrownCitadel</c>.</param>
    /// <returns>For example <c>Icecrown Citadel</c>.</returns>
    public static string Instance(string instance) => Instances.GetValueOrDefault(instance, instance);

    /// <summary>Gets a raid size and difficulty as the profile writes it.</summary>
    /// <param name="difficulty">The difficulty name, for example <c>TwentyFivePlayerHeroic</c>.</param>
    /// <returns>For example <c>25 players, heroic</c>.</returns>
    public static string Difficulty(string difficulty) => Difficulties.GetValueOrDefault(difficulty, difficulty);

    /// <summary>Gets an equipment slot as the profile writes it.</summary>
    /// <param name="slot">The slot name, for example <c>TrinketTwo</c>.</param>
    /// <returns>For example <c>Trinket</c>.</returns>
    public static string Slot(string slot) => Slots.GetValueOrDefault(slot, slot);

    /// <summary>Gets the words that say where a fact came from.</summary>
    /// <param name="source">The source name, for example <c>WowAddon</c>.</param>
    /// <returns>For example <c>from the addon</c>.</returns>
    public static string Source(string source) => Sources.GetValueOrDefault(source, source);

    /// <summary>Gets a GearScore with a thousands separator.</summary>
    /// <param name="gearScore">The GearScore, or <see langword="null"/> before an item catalog gives one (#552).</param>
    /// <returns>For example <c>GearScore 5,712</c>, or <c>GearScore —</c> without one.</returns>
    public static string GearScore(int? gearScore) => $"GearScore {Number(gearScore)}";

    /// <summary>Gets an item level.</summary>
    /// <param name="itemLevel">The item level, or <see langword="null"/> before an item catalog gives one (#552).</param>
    /// <returns>For example <c>264</c>, or <c>—</c> without one.</returns>
    public static string ItemLevel(int? itemLevel) => Number(itemLevel);

    /// <summary>Gets the item name an in-game item link carries.</summary>
    /// <param name="item">The equipped item.</param>
    /// <returns>The name between the link's brackets, or <c>Item</c> and its identifier when the link has none.</returns>
    public static string ItemName(CharacterGearItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        var match = LinkName().Match(item.ItemLink);
        return match.Success ? match.Groups["name"].Value : string.Create(CultureInfo.InvariantCulture, $"Item {item.ItemId}");
    }
    #endregion Public Methods

    #region Private Helpers
    /// <summary>Formats a number with a thousands separator, or a dash for a value RaidManager doesn't have.</summary>
    /// <param name="value">The number.</param>
    /// <returns>For example <c>5,712</c>, or <c>—</c>.</returns>
    private static string Number(int? value) => value?.ToString("N0", CultureInfo.InvariantCulture) ?? MissingValue;

    /// <summary>Matches each capital letter that starts a word inside a name.</summary>
    /// <returns>The expression.</returns>
    [GeneratedRegex("(?<!^)([A-Z])", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 100)]
    private static partial Regex CapitalWords();

    /// <summary>Matches the name of an in-game item link, such as <c>|h[Havoc's Call]|h</c>.</summary>
    /// <returns>The expression.</returns>
    [GeneratedRegex(@"\|h\[(?<name>[^\]]+)\]\|h", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 100)]
    private static partial Regex LinkName();
    #endregion Private Helpers
}
