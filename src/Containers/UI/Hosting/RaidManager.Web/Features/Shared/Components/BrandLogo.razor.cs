using Microsoft.AspNetCore.Components;

namespace RaidManager.Web.Features.Shared.Components;

/// <summary>Shows the RaidManager logo as a link.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: The design system's logo (ADR-0019): the accent tile and the two-color word mark, used in the sidebar and the signed-out frame.
/// </remarks>
public sealed partial class BrandLogo
{
    #region Properties
    /// <summary>Gets or sets where the logo links to.</summary>
    [Parameter]
    public string Href { get; set; } = "/";

    /// <summary>Gets or sets extra attributes for the link, such as a test id.</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }
    #endregion Properties
}
