namespace RaidManager.ViewModels.Features.Characters;

/// <summary>Names the styles of a review notification.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Lets the view model choose a style without depending on the component library.
/// </remarks>
public enum ReviewNoticeKind
{
    /// <summary>The decision was recorded.</summary>
    Success,

    /// <summary>The claim changed meanwhile; the list shows its current state.</summary>
    Info,

    /// <summary>The claim went to an officer instead.</summary>
    Warning,

    /// <summary>The decision failed and nothing changed.</summary>
    Error,
}
