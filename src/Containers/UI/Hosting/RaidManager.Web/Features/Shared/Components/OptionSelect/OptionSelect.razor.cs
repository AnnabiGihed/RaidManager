using System.Globalization;
using Microsoft.AspNetCore.Components;

namespace RaidManager.Web.Features.Shared.Components;

/// <summary>Lets the user pick one option from a compact drop-down list.</summary>
/// <typeparam name="TValue">The type of the options' values.</typeparam>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: The 36 px select of ADR-0019, such as the Discord role picker. Built on the native select, so the keyboard and screen readers work as for any select; Radzen's drop-down renders its list outside the dark theme.
/// </remarks>
public sealed partial class OptionSelect<TValue>
{
    #region Properties
    /// <summary>Gets or sets the label read by screen readers.</summary>
    [Parameter]
    [EditorRequired]
    public string Label { get; set; } = string.Empty;

    /// <summary>Gets or sets the options, in order.</summary>
    [Parameter]
    [EditorRequired]
    public IReadOnlyList<ChoiceOption<TValue>> Options { get; set; } = [];

    /// <summary>Gets or sets the chosen value.</summary>
    [Parameter]
    public TValue? Value { get; set; }

    /// <summary>Gets or sets the callback raised with the newly chosen value.</summary>
    [Parameter]
    public EventCallback<TValue> ValueChanged { get; set; }

    /// <summary>Gets or sets attributes passed through to the select, such as a test id.</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }
    #endregion Properties

    #region Private Helpers
    /// <summary>Tells whether an option is the chosen one.</summary>
    /// <param name="option">The option.</param>
    /// <returns><see langword="true"/> when it is chosen.</returns>
    private bool IsSelected(ChoiceOption<TValue> option) => EqualityComparer<TValue>.Default.Equals(option.Value, Value);

    /// <summary>Raises the value of the option the user chose.</summary>
    /// <param name="args">The change, carrying the option's position.</param>
    /// <returns>A task that completes when the callback has run.</returns>
    private Task SelectAsync(ChangeEventArgs args) =>
        int.TryParse(args.Value?.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var index) && index >= 0 && index < Options.Count
            ? ValueChanged.InvokeAsync(Options[index].Value)
            : Task.CompletedTask;
    #endregion Private Helpers
}
