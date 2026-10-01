using Microsoft.AspNetCore.Components;
using RaidManager.ViewModels.Features.Shared.Shell;

namespace RaidManager.Web.Features.Shared.Components;

/// <summary>Shows the player's Discord avatar, or their initials when they have none.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: The round avatar of the shell's top bar and user card (ADR-0019). It is decorative: the name is always
/// shown beside it.
/// </remarks>
public sealed partial class UserAvatar
{
    #region Properties
    /// <summary>Gets or sets the player's display name, used for the initials.</summary>
    [Parameter]
    [EditorRequired]
    public string? Name { get; set; }

    /// <summary>Gets or sets the Discord avatar URL, if the player has one.</summary>
    [Parameter]
    public string? ImageUrl { get; set; }

    /// <summary>Gets or sets the diameter in pixels.</summary>
    [Parameter]
    public int Size { get; set; } = 32;

    /// <summary>Gets a value indicating whether an avatar image is shown.</summary>
    private bool HasImage => !string.IsNullOrWhiteSpace(ImageUrl);

    /// <summary>Gets the initials shown without an image.</summary>
    private string Initials => ShellViewModel.Initials(Name);

    /// <summary>Gets the inline size style.</summary>
    private string SizeStyle => $"width: {Size}px; height: {Size}px;";
    #endregion Properties
}
