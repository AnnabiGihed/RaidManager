namespace RaidManager.ViewModels.Features.Companions;

/// <summary>Identifies how a companion page's notification or notice is styled.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: Keeps the wording in the view models; the pages only map the kind to a style.
/// </remarks>
public enum CompanionNoticeKind
{
    /// <summary>Something was done as asked.</summary>
    Success,

    /// <summary>Something to know, nothing went wrong.</summary>
    Info,

    /// <summary>Something can't go on as asked, and the player can fix it.</summary>
    Warning,

    /// <summary>Something failed; nothing changed.</summary>
    Error,
}
