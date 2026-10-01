using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Radzen;

namespace RaidManager.Web.Features.Shared.Components;

/// <summary>Shows a secondary button styled for the design system's dark surfaces.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Radzen's outlined button reads dark gray on the dark palette; this wraps it with the design system's secondary look so no page restyles a Radzen button itself.
/// </remarks>
public sealed partial class SurfaceButton
{
    #region Properties
    /// <summary>Gets or sets the button label.</summary>
    [Parameter]
    [EditorRequired]
    public string Text { get; set; } = string.Empty;

    /// <summary>Gets or sets the button type; <see cref="ButtonType.Submit"/> submits the surrounding form.</summary>
    [Parameter]
    public ButtonType ButtonType { get; set; } = ButtonType.Button;

    /// <summary>Gets or sets a value indicating whether the button is disabled.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>Gets or sets the callback raised when the button is clicked.</summary>
    [Parameter]
    public EventCallback<MouseEventArgs> Click { get; set; }

    /// <summary>Gets or sets extra attributes for the button, such as a test id.</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }
    #endregion Properties
}
