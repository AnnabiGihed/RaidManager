using Microsoft.AspNetCore.Components;

namespace RaidManager.Web.Features.Shared.Components;

/// <summary>Shows a World of Warcraft class by name, with a dot in the class's color.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-02<br/>
/// Purpose: Names a class the way players read it ("Death Knight") whatever form the API sends ("DeathKnight"). The
/// color only repeats the name; an unknown class shows its name with a neutral dot.
/// </remarks>
public sealed partial class WowClassLabel
{
    #region Fields
    /// <summary>Stores the readable name of each WotLK class, by its name without spaces.</summary>
    private static readonly Dictionary<string, string> Names = new(StringComparer.OrdinalIgnoreCase)
    {
        ["DeathKnight"] = "Death Knight",
        ["Druid"] = "Druid",
        ["Hunter"] = "Hunter",
        ["Mage"] = "Mage",
        ["Paladin"] = "Paladin",
        ["Priest"] = "Priest",
        ["Rogue"] = "Rogue",
        ["Shaman"] = "Shaman",
        ["Warlock"] = "Warlock",
        ["Warrior"] = "Warrior",
    };
    #endregion Fields

    #region Properties
    /// <summary>Gets or sets the class, such as <c>DeathKnight</c> or <c>Death Knight</c>.</summary>
    [Parameter]
    [EditorRequired]
    public string ClassName { get; set; } = string.Empty;

    /// <summary>Gets or sets attributes passed through to the label, such as a test id.</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    /// <summary>Gets the class's name without spaces, the key of <see cref="Names"/>.</summary>
    private string Key => ClassName.Replace(" ", string.Empty, StringComparison.Ordinal);

    /// <summary>Gets the readable name, or the given one for an unknown class.</summary>
    private string Label => Names.TryGetValue(Key, out var name) ? name : ClassName;

    /// <summary>Gets the classes: the class's color for a known class.</summary>
    private string CssClass => Names.ContainsKey(Key) ? $"wow-class wow-class-{Key.ToLowerInvariant()}" : "wow-class";
    #endregion Properties
}
