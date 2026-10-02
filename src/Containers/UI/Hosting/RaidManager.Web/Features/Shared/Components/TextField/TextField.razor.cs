using Microsoft.AspNetCore.Components;

namespace RaidManager.Web.Features.Shared.Components;

/// <summary>Shows a labelled one-line text input.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-02<br/>
/// Purpose: The text input of the design system's forms (the name field of the role form, board 10), with its label tied
/// to the input for screen readers.
/// </remarks>
public sealed partial class TextField
{
    #region Fields
    /// <summary>Stores the input's own element id, which the label points at.</summary>
    private readonly string _id = $"text-{Guid.NewGuid():N}";
    #endregion Fields

    #region Properties
    /// <summary>Gets or sets the label above the input.</summary>
    [Parameter]
    [EditorRequired]
    public string Label { get; set; } = string.Empty;

    /// <summary>Gets or sets the text.</summary>
    [Parameter]
    public string Value { get; set; } = string.Empty;

    /// <summary>Gets or sets what happens when the text changes, as it is typed.</summary>
    [Parameter]
    public EventCallback<string> ValueChanged { get; set; }

    /// <summary>Gets or sets the longest text the input accepts; 524288 by default, the browser's own limit.</summary>
    [Parameter]
    public int MaxLength { get; set; } = 524288;

    /// <summary>Gets or sets attributes passed through to the input, such as a test id.</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }
    #endregion Properties

    #region Private Helpers
    /// <summary>Reports the typed text.</summary>
    /// <param name="args">The input event.</param>
    /// <returns>A task that completes when the change is reported.</returns>
    private Task ChangeAsync(ChangeEventArgs args) => ValueChanged.InvokeAsync(args.Value?.ToString() ?? string.Empty);
    #endregion Private Helpers
}
