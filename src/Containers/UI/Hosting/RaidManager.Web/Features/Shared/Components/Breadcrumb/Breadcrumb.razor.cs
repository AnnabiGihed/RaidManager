using Microsoft.AspNetCore.Components;

namespace RaidManager.Web.Features.Shared.Components;

/// <summary>Shows where the player is as uppercase labels separated by slashes; the last one is the current page.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: The top bar's breadcrumb (ADR-0019), for any trail of labels, such as community / page.
/// </remarks>
public sealed partial class Breadcrumb
{
    #region Properties
    /// <summary>Gets or sets the labels from the root to the current page.</summary>
    [Parameter]
    [EditorRequired]
    public IReadOnlyList<string> Items { get; set; } = [];
    #endregion Properties
}
