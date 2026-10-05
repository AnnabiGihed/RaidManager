using Microsoft.AspNetCore.Components;

namespace RaidManager.Web.Features.Shared.Components;

/// <summary>Shows content on a card of the design system's dark surface, with an optional title, accent and actions.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: The card of ADR-0019 for any content: the sign-in card and the failure cards today, any page's card later.
/// </remarks>
public sealed partial class SurfaceCard
{
    #region Properties
    /// <summary>Gets or sets the optional title.</summary>
    [Parameter]
    public string? Title { get; set; }

    /// <summary>Gets or sets the optional line under the title.</summary>
    [Parameter]
    public string? Subtitle { get; set; }

    /// <summary>Gets or sets the heading level of the title.</summary>
    [Parameter]
    public HeadingLevel HeadingLevel { get; set; } = HeadingLevel.H2;

    /// <summary>Gets or sets the accent bar on the left edge.</summary>
    [Parameter]
    public CardAccent Accent { get; set; } = CardAccent.None;

    /// <summary>Gets or sets a value indicating whether the card uses the smaller title of boards with many cards, such as a profile.</summary>
    [Parameter]
    public bool Compact { get; set; }

    /// <summary>Gets or sets the card's body.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>Gets or sets the actions shown in a row under the body, such as buttons.</summary>
    [Parameter]
    public RenderFragment? Actions { get; set; }

    /// <summary>Gets or sets optional actions at the end of the title row, such as "Create role".</summary>
    [Parameter]
    public RenderFragment? HeaderActions { get; set; }

    /// <summary>Gets or sets extra attributes for the card, such as a test id.</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    /// <summary>Gets the classes of the accent bar.</summary>
    private string CssClass => Compact ? "surface-card surface-card-compact" : "surface-card";

    /// <summary>Gets the accent bar's classes.</summary>
    private string AccentClass => $"surface-card-accent surface-card-accent-{Accent.ToString().ToLowerInvariant()}";
    #endregion Properties
}
