using Microsoft.AspNetCore.Components;

namespace RaidManager.Web.Features.Shared.Components;

/// <summary>Shows a leading visual with a title and an optional subtitle, such as a community or a signed-in player.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: One component for every icon, name and detail block of the design system: the shell's community card and user card today.
/// </remarks>
public sealed partial class SummaryTile
{
    #region Properties
    /// <summary>Gets or sets the title, such as a name.</summary>
    [Parameter]
    [EditorRequired]
    public string Title { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional line under the title, such as a role or a hint.</summary>
    [Parameter]
    public string? Subtitle { get; set; }

    /// <summary>Gets or sets the visual before the text, such as an avatar or an icon tile.</summary>
    [Parameter]
    public RenderFragment? Leading { get; set; }

    /// <summary>Gets or sets how the tile is framed.</summary>
    [Parameter]
    public SummaryTileAppearance Appearance { get; set; } = SummaryTileAppearance.Card;

    /// <summary>Gets or sets a value indicating whether the tile leads somewhere: a › chevron, a pointer and a hover highlight.</summary>
    [Parameter]
    public bool ShowsLink { get; set; }

    /// <summary>Gets or sets extra attributes for the tile, such as a test id.</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    /// <summary>Gets the CSS classes for the chosen appearance.</summary>
    private string CssClass => (Appearance == SummaryTileAppearance.Card ? "summary-tile summary-tile-card" : "summary-tile")
        + (ShowsLink ? " summary-tile-link" : string.Empty);
    #endregion Properties
}
