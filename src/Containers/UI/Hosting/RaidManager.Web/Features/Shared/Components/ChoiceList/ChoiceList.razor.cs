using Microsoft.AspNetCore.Components;

namespace RaidManager.Web.Features.Shared.Components;

/// <summary>Lets the user pick one option from a list of full-width, selectable rows.</summary>
/// <typeparam name="TValue">The type of the options' values.</typeparam>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: The single-choice rows of ADR-0019, such as the Warmane realms. Built on native radio buttons, so the keyboard and screen readers work as for any radio group; Radzen's radio list can't render a whole selectable row.
/// </remarks>
public sealed partial class ChoiceList<TValue>
{
    #region Properties
    /// <summary>Gets or sets the group's visible legend.</summary>
    [Parameter]
    [EditorRequired]
    public string Legend { get; set; } = string.Empty;

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

    /// <summary>Gets or sets attributes passed through to the group, such as a test id.</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    /// <summary>Gets the radio group's name, unique to this list.</summary>
    private string Name { get; } = $"choice-{Guid.NewGuid():N}";
    #endregion Properties

    #region Private Helpers
    /// <summary>Tells whether an option is the chosen one.</summary>
    /// <param name="option">The option.</param>
    /// <returns><see langword="true"/> when it is chosen.</returns>
    private bool IsSelected(ChoiceOption<TValue> option) => EqualityComparer<TValue>.Default.Equals(option.Value, Value);

    /// <summary>Gives an option's classes.</summary>
    /// <param name="option">The option.</param>
    /// <returns>The classes.</returns>
    private string OptionClass(ChoiceOption<TValue> option) => IsSelected(option) ? "choice-list-option choice-list-option-selected" : "choice-list-option";

    /// <summary>Raises the chosen value.</summary>
    /// <param name="option">The chosen option.</param>
    /// <returns>A task that completes when the callback has run.</returns>
    private Task SelectAsync(ChoiceOption<TValue> option) => ValueChanged.InvokeAsync(option.Value);
    #endregion Private Helpers
}
