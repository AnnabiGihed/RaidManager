using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace RaidManager.Web.Features.Shared.Components;

/// <summary>Shows a 24 px pill with a word, and a × to remove it when the parent allows.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: The badge of ADR-0019 as a removable tag, such as a Discord role mapped to an officer role.
/// </remarks>
public sealed partial class TagChip
{
    #region Properties
    /// <summary>Gets or sets the chip's text.</summary>
    [Parameter]
    [EditorRequired]
    public string Text { get; set; } = string.Empty;

    /// <summary>Gets or sets the tone.</summary>
    [Parameter]
    public TagChipTone Tone { get; set; } = TagChipTone.Info;

    /// <summary>Gets or sets the callback raised when the × is pressed; without one, the chip has no ×.</summary>
    [Parameter]
    public EventCallback<MouseEventArgs> OnRemove { get; set; }

    /// <summary>Gets or sets what the × does, for screen readers and the tooltip, such as "Remove @Officier".</summary>
    [Parameter]
    public string RemoveLabel { get; set; } = "Remove";

    /// <summary>Gets or sets attributes passed through to the chip, such as a test id.</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    /// <summary>Gets the classes for the tone.</summary>
    private string CssClass => $"tag-chip tag-chip-{Tone.ToString().ToLowerInvariant()}";
    #endregion Properties
}
