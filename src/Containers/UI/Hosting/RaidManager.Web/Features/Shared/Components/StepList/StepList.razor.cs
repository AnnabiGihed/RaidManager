using Microsoft.AspNetCore.Components;

namespace RaidManager.Web.Features.Shared.Components;

/// <summary>Shows numbered steps, each with a title and a line of detail.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Explains a short process on any page, such as adding RaidManager to a Discord server.
/// </remarks>
public sealed partial class StepList
{
    #region Properties
    /// <summary>Gets or sets the steps, in order.</summary>
    [Parameter]
    [EditorRequired]
    public IReadOnlyList<StepListItem> Steps { get; set; } = [];

    /// <summary>Gets or sets attributes passed through to the list, such as a test id.</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }
    #endregion Properties
}
