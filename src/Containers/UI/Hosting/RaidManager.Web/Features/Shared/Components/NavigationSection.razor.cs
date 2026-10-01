using Microsoft.AspNetCore.Components;
using RaidManager.ViewModels.Features.Shared.Shell;

namespace RaidManager.Web.Features.Shared.Components;

/// <summary>Shows a headed list of page links, highlighting the current page.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: One sidebar section (ADR-0019), for any heading and pages; the theme's dark class styles Radzen's menu.
/// </remarks>
public sealed partial class NavigationSection
{
    #region Properties
    /// <summary>Gets or sets the section heading, for example <c>PLAYER</c>.</summary>
    [Parameter]
    [EditorRequired]
    public string Heading { get; set; } = string.Empty;

    /// <summary>Gets or sets the pages to link, in order.</summary>
    [Parameter]
    [EditorRequired]
    public IReadOnlyList<ShellEntry> Entries { get; set; } = [];
    #endregion Properties
}
