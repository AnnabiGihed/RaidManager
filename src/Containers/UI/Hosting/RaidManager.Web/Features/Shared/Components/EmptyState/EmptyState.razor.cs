using Microsoft.AspNetCore.Components;

namespace RaidManager.Web.Features.Shared.Components;

/// <summary>Says, in a centered card with an optional check mark, that nothing is left to do or there is nothing yet.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-02<br/>
/// Purpose: The "all done" state of the design system, such as "You're all set" on the character review page.
/// </remarks>
public sealed partial class EmptyState
{
    #region Properties
    /// <summary>Gets or sets the title, such as "You're all set".</summary>
    [Parameter]
    [EditorRequired]
    public string Title { get; set; } = string.Empty;

    /// <summary>Gets or sets the line under the title.</summary>
    [Parameter]
    [EditorRequired]
    public string Message { get; set; } = string.Empty;

    /// <summary>Gets or sets a value indicating whether the check mark shows; off for "nothing yet" rather than "all done".</summary>
    [Parameter]
    public bool ShowsMark { get; set; } = true;

    /// <summary>Gets or sets optional extra content under the message, such as reminders.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>Gets or sets the optional actions, such as Continue.</summary>
    [Parameter]
    public RenderFragment? Actions { get; set; }

    /// <summary>Gets or sets attributes passed through to the card, such as a test id.</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }
    #endregion Properties
}
