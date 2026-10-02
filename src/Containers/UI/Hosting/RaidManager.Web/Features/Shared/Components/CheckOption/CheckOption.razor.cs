using Microsoft.AspNetCore.Components;

namespace RaidManager.Web.Features.Shared.Components;

/// <summary>Shows a checkbox with a title and one line saying what it means.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-02<br/>
/// Purpose: One permission of the role form (board 10): the whole label toggles the box, and a disabled option stays
/// visible so the user sees what they can't give.
/// </remarks>
public sealed partial class CheckOption
{
    #region Properties
    /// <summary>Gets or sets the option's title, such as "Manage raids".</summary>
    [Parameter]
    [EditorRequired]
    public string Title { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional line under the title.</summary>
    [Parameter]
    public string? Detail { get; set; }

    /// <summary>Gets or sets a value indicating whether the box is ticked.</summary>
    [Parameter]
    public bool Checked { get; set; }

    /// <summary>Gets or sets what happens when the box is ticked or cleared.</summary>
    [Parameter]
    public EventCallback<bool> CheckedChanged { get; set; }

    /// <summary>Gets or sets a value indicating whether the box can't be changed.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>Gets or sets attributes passed through to the option, such as a test id.</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    /// <summary>Gets the classes: dimmed when disabled.</summary>
    private string CssClass => Disabled ? "check-option check-option-disabled" : "check-option";
    #endregion Properties

    #region Private Helpers
    /// <summary>Reports the box's new state.</summary>
    /// <param name="args">The change event.</param>
    /// <returns>A task that completes when the change is reported.</returns>
    private Task ChangeAsync(ChangeEventArgs args) => CheckedChanged.InvokeAsync(args.Value is true);
    #endregion Private Helpers
}
