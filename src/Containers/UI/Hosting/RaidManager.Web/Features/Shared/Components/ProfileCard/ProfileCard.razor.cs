using Microsoft.AspNetCore.Components;

namespace RaidManager.Web.Features.Shared.Components;

/// <summary>Shows a person in a card: their avatar, their name and one line about them.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-02<br/>
/// Purpose: The account card of the signed-in Overview (sign-in mockup, board 2), such as the player's name with
/// "Signed in with Discord".
/// </remarks>
public sealed partial class ProfileCard
{
    #region Properties
    /// <summary>Gets or sets the person's name; the avatar shows its initials when there is no picture.</summary>
    [Parameter]
    [EditorRequired]
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional line under the name.</summary>
    [Parameter]
    public string? Detail { get; set; }

    /// <summary>Gets or sets the optional picture's URL.</summary>
    [Parameter]
    public string? ImageUrl { get; set; }

    /// <summary>Gets or sets attributes passed through to the card, such as a test id.</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }
    #endregion Properties
}
