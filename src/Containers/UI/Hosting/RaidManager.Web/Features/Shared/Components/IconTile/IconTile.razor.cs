using Microsoft.AspNetCore.Components;

namespace RaidManager.Web.Features.Shared.Components;

/// <summary>Shows a short glyph, such as <c>+</c> or initials, in a rounded square.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: A decorative leading tile for summary tiles and lists; the text beside it always says what it means.
/// </remarks>
public sealed partial class IconTile
{
    #region Properties
    /// <summary>Gets or sets the glyph, one or two characters.</summary>
    [Parameter]
    [EditorRequired]
    public string Glyph { get; set; } = string.Empty;

    /// <summary>Gets or sets the tile's look.</summary>
    [Parameter]
    public IconTileAppearance Appearance { get; set; } = IconTileAppearance.Neutral;

    /// <summary>Gets the classes for the look.</summary>
    private string CssClass => Appearance == IconTileAppearance.Community ? "icon-tile icon-tile-community" : "icon-tile";
    #endregion Properties
}
