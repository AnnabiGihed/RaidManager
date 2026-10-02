using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace RaidManager.Web.Features.Shared.Components;

/// <summary>Shows a form above the dimmed page: a title, the fields and the actions.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-02<br/>
/// Purpose: The dialog of the design system for a form, such as the role form of boards 10 and 11. The page shows it while
/// the form is open; Escape cancels, and focus moves to the dialog when it opens.
/// </remarks>
public sealed partial class FormDialog
{
    #region Fields
    /// <summary>Stores this dialog's own prefix for its element ids.</summary>
    private readonly string _id = $"form-{Guid.NewGuid():N}";

    /// <summary>Stores the dialog element, which takes focus when it opens.</summary>
    private ElementReference _dialog;
    #endregion Fields

    #region Properties
    /// <summary>Gets or sets the title, such as "Create a role".</summary>
    [Parameter]
    [EditorRequired]
    public string Title { get; set; } = string.Empty;

    /// <summary>Gets or sets the form's fields.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>Gets or sets the form's buttons.</summary>
    [Parameter]
    public RenderFragment? Actions { get; set; }

    /// <summary>Gets or sets what happens when the user presses Escape.</summary>
    [Parameter]
    public EventCallback OnCancel { get; set; }

    /// <summary>Gets or sets attributes passed through to the dialog, such as a test id.</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    /// <summary>Gets the title's element id.</summary>
    private string TitleId => $"{_id}-title";
    #endregion Properties

    #region Overrides
    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await _dialog.FocusAsync();
        }
    }
    #endregion Overrides

    #region Private Helpers
    /// <summary>Cancels when the user presses Escape.</summary>
    /// <param name="args">The key pressed.</param>
    /// <returns>A task that completes when the key is handled.</returns>
    private Task OnKeyDownAsync(KeyboardEventArgs args) =>
        string.Equals(args.Key, "Escape", StringComparison.Ordinal) ? OnCancel.InvokeAsync() : Task.CompletedTask;
    #endregion Private Helpers
}
