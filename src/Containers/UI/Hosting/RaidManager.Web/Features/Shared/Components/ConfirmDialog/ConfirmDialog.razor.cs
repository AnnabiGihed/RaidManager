using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace RaidManager.Web.Features.Shared.Components;

/// <summary>Asks the user to confirm an action, above the dimmed page: a title, a message, Cancel and the action.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-02<br/>
/// Purpose: The confirmation dialog of the design system, such as "Reject Jainaice?" on the character review page. The
/// page shows it while a confirmation is pending; Escape cancels, and focus moves to the dialog when it opens.
/// </remarks>
public sealed partial class ConfirmDialog
{
    #region Fields
    /// <summary>Stores this dialog's own prefix for its element ids.</summary>
    private readonly string _id = $"confirm-{Guid.NewGuid():N}";

    /// <summary>Stores the dialog element, which takes focus when it opens.</summary>
    private ElementReference _dialog;
    #endregion Fields

    #region Properties
    /// <summary>Gets or sets the question, such as "Reject Jainaice?".</summary>
    [Parameter]
    [EditorRequired]
    public string Title { get; set; } = string.Empty;

    /// <summary>Gets or sets what confirming does.</summary>
    [Parameter]
    [EditorRequired]
    public string Message { get; set; } = string.Empty;

    /// <summary>Gets or sets an optional second paragraph, such as advice.</summary>
    [Parameter]
    public string? Detail { get; set; }

    /// <summary>Gets or sets the label of the action; "Confirm" by default.</summary>
    [Parameter]
    public string ConfirmText { get; set; } = "Confirm";

    /// <summary>Gets or sets the label of the way out; "Cancel" by default.</summary>
    [Parameter]
    public string CancelText { get; set; } = "Cancel";

    /// <summary>Gets or sets the look of the action, such as <see cref="ActionButtonAppearance.Danger"/> for a rejection.</summary>
    [Parameter]
    public ActionButtonAppearance ConfirmAppearance { get; set; } = ActionButtonAppearance.Primary;

    /// <summary>Gets or sets what happens when the user confirms.</summary>
    [Parameter]
    public EventCallback OnConfirm { get; set; }

    /// <summary>Gets or sets what happens when the user cancels.</summary>
    [Parameter]
    public EventCallback OnCancel { get; set; }

    /// <summary>Gets or sets attributes passed through to the dialog, such as a test id.</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    /// <summary>Gets the title's element id.</summary>
    private string TitleId => $"{_id}-title";

    /// <summary>Gets the message's element id.</summary>
    private string MessageId => $"{_id}-message";
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
