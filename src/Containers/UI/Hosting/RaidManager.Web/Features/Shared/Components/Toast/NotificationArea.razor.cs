namespace RaidManager.Web.Features.Shared.Components;

/// <summary>Shows the page's <see cref="Toast"/> outside the page's content, where the layout places it.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-09<br/>
/// Purpose: Radzen's page body is transformed, which makes it the box a fixed notification is placed in: inside it,
/// a notification sat 64 px too low and scrolled with the page (#577). The layout puts this area outside that body,
/// so a notification is placed against the window, 16 px under the top bar.
/// </remarks>
public sealed partial class NotificationArea
{
    #region Constants
    /// <summary>Names the section a <see cref="Toast"/> renders into.</summary>
    public const string SectionName = "rm-notifications";
    #endregion Constants
}
