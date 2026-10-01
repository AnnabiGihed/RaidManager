using Microsoft.AspNetCore.Components;

namespace RaidManager.Web.Features.Shared.Components;

/// <summary>Shows a page's main heading: an optional uppercase eyebrow, the title and an optional subtitle.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: The page header of ADR-0019 for every page: aligned to the start on signed-in pages, centered on signed-out ones.
/// </remarks>
public sealed partial class PageHeading
{
    #region Properties
    /// <summary>Gets or sets the page title.</summary>
    [Parameter]
    [EditorRequired]
    public string Title { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional line under the title.</summary>
    [Parameter]
    public string? Subtitle { get; set; }

    /// <summary>Gets or sets the optional small label above the title, shown in uppercase.</summary>
    [Parameter]
    public string? Eyebrow { get; set; }

    /// <summary>Gets or sets the text alignment.</summary>
    [Parameter]
    public ContentAlignment Alignment { get; set; } = ContentAlignment.Start;

    /// <summary>Gets the classes for the alignment.</summary>
    private string CssClass => Alignment == ContentAlignment.Center ? "page-heading page-heading-center" : "page-heading";
    #endregion Properties
}
