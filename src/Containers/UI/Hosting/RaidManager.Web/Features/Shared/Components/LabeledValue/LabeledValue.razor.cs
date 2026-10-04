using Microsoft.AspNetCore.Components;

namespace RaidManager.Web.Features.Shared.Components;

/// <summary>Shows a value under its small uppercase label, with an optional note.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: The label-and-value block of ADR-0019 cards, such as a community's server, realm and Administrator.
/// </remarks>
public sealed partial class LabeledValue
{
    #region Properties
    /// <summary>Gets or sets the label, shown in uppercase.</summary>
    [Parameter]
    [EditorRequired]
    public string Label { get; set; } = string.Empty;

    /// <summary>Gets or sets the value.</summary>
    [Parameter]
    [EditorRequired]
    public string Value { get; set; } = string.Empty;

    /// <summary>Gets or sets a value indicating whether the value is plain text rather than emphasized, as a computer name under its label.</summary>
    [Parameter]
    public bool Plain { get; set; }

    /// <summary>Gets or sets the optional line under the value.</summary>
    [Parameter]
    public string? Note { get; set; }

    /// <summary>Gets or sets attributes passed through to the block, such as a test id.</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    /// <summary>Gets the value's classes.</summary>
    private string ValueClass => Plain ? "labeled-value-value labeled-value-plain" : "labeled-value-value";
    #endregion Properties
}
