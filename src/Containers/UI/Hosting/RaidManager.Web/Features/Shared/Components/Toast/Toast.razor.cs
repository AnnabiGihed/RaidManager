using Microsoft.AspNetCore.Components;

namespace RaidManager.Web.Features.Shared.Components;

/// <summary>Shows a short confirmation at the top right of the page: a title and one line of text.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: The 380 by 72 px notification of ADR-0019, such as "Officer roles saved", placed above the page so it
/// doesn't cover the content under it.
/// </remarks>
public sealed partial class Toast
{
    #region Properties
    /// <summary>Gets or sets the title.</summary>
    [Parameter]
    [EditorRequired]
    public string Title { get; set; } = string.Empty;

    /// <summary>Gets or sets the line under the title.</summary>
    [Parameter]
    [EditorRequired]
    public string Message { get; set; } = string.Empty;

    /// <summary>Gets or sets attributes passed through to the notification, such as a test id.</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }
    #endregion Properties
}
