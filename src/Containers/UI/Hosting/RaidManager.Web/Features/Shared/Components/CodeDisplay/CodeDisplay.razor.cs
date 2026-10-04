using Microsoft.AspNetCore.Components;

namespace RaidManager.Web.Features.Shared.Components;

/// <summary>Shows a short code to compare by eye, large and spaced, under its label and above a caption.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: The pairing code of the companion pairing mockup (board 1): a code the player checks against another screen, such as K7M-4QX with Expires in 9 minutes.
/// </remarks>
public sealed partial class CodeDisplay
{
    #region Properties
    /// <summary>Gets or sets the label, shown in uppercase.</summary>
    [Parameter]
    [EditorRequired]
    public string Label { get; set; } = string.Empty;

    /// <summary>Gets or sets the code.</summary>
    [Parameter]
    [EditorRequired]
    public string Code { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional line under the code, such as its expiry.</summary>
    [Parameter]
    public string? Caption { get; set; }

    /// <summary>Gets or sets a value indicating whether the code is shown muted, as one that can't be used anymore.</summary>
    [Parameter]
    public bool Muted { get; set; }

    /// <summary>Gets or sets attributes passed through to the block, such as a test id.</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    /// <summary>Gets the block's classes.</summary>
    private string CssClass => Muted ? "code-display code-display-muted" : "code-display";
    #endregion Properties
}
