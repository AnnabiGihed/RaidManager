using Microsoft.AspNetCore.Components;

namespace RaidManager.Web.Features.Shared.Components;

/// <summary>Shows a message box with a mark, a title and an optional line of text.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: The notice of ADR-0019 for any page: information, or a refusal or failure the user should read.
/// </remarks>
public sealed partial class Notice
{
    #region Properties
    /// <summary>Gets or sets the title.</summary>
    [Parameter]
    [EditorRequired]
    public string Title { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional line under the title.</summary>
    [Parameter]
    public string? Message { get; set; }

    /// <summary>Gets or sets the tone.</summary>
    [Parameter]
    public NoticeTone Tone { get; set; } = NoticeTone.Info;

    /// <summary>Gets or sets attributes passed through to the notice, such as a test id.</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    /// <summary>Gets the classes for the tone.</summary>
    private string CssClass => Tone == NoticeTone.Danger ? "notice notice-danger" : "notice notice-info";

    /// <summary>Gets the mark that names the tone without color.</summary>
    private string Mark => Tone == NoticeTone.Danger ? "!" : "i";

    /// <summary>Gets the role: an alert interrupts a screen reader, a status doesn't.</summary>
    private string Role => Tone == NoticeTone.Danger ? "alert" : "status";
    #endregion Properties
}
