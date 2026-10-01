using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Radzen;

namespace RaidManager.Web.Features.Shared.Components;

/// <summary>Shows a button in one of the design system's styles.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Wraps Radzen's button with the 40 px, 8 px-radius, sentence-case buttons of ADR-0019, so no page restyles a Radzen button itself.
/// </remarks>
public sealed partial class ActionButton
{
    #region Properties
    /// <summary>Gets or sets the button label.</summary>
    [Parameter]
    [EditorRequired]
    public string Text { get; set; } = string.Empty;

    /// <summary>Gets or sets the button style.</summary>
    [Parameter]
    public ActionButtonAppearance Appearance { get; set; } = ActionButtonAppearance.Primary;

    /// <summary>Gets or sets the button type; <see cref="ButtonType.Submit"/> submits the surrounding form.</summary>
    [Parameter]
    public ButtonType ButtonType { get; set; } = ButtonType.Button;

    /// <summary>Gets or sets a value indicating whether the button fills the width of its container.</summary>
    [Parameter]
    public bool FullWidth { get; set; }

    /// <summary>Gets or sets a value indicating whether the button is disabled.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>Gets or sets the callback raised when the button is clicked.</summary>
    [Parameter]
    public EventCallback<MouseEventArgs> Click { get; set; }

    /// <summary>Gets or sets extra attributes for the button, such as a test id.</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    /// <summary>Gets the Radzen style closest to the appearance, for its focus and disabled states.</summary>
    private ButtonStyle RadzenStyle => Appearance switch
    {
        ActionButtonAppearance.Danger => ButtonStyle.Danger,
        ActionButtonAppearance.Secondary => ButtonStyle.Base,
        _ => ButtonStyle.Primary,
    };

    /// <summary>Gets the classes of the host element.</summary>
    private string HostClass => FullWidth ? "action-button-host action-button-full" : "action-button-host";

    /// <summary>Gets the classes of the button.</summary>
    private string ButtonClass => $"action-button action-button-{Appearance.ToString().ToLowerInvariant()}";
    #endregion Properties
}
