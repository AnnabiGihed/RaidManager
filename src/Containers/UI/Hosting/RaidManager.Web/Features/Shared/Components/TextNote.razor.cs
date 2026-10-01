using Microsoft.AspNetCore.Components;

namespace RaidManager.Web.Features.Shared.Components;

/// <summary>Shows a short muted note, such as a hint under a card.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: The small muted text of ADR-0019 for any side remark, aligned to the start or centered.
/// </remarks>
public sealed partial class TextNote
{
    #region Properties
    /// <summary>Gets or sets the note.</summary>
    [Parameter]
    [EditorRequired]
    public string Text { get; set; } = string.Empty;

    /// <summary>Gets or sets the text alignment.</summary>
    [Parameter]
    public ContentAlignment Alignment { get; set; } = ContentAlignment.Start;

    /// <summary>Gets the classes for the alignment.</summary>
    private string CssClass => Alignment == ContentAlignment.Center ? "text-note text-note-center" : "text-note";
    #endregion Properties
}
