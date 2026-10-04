namespace RaidManager.Web.Features.Shared.Components;

/// <summary>Names the tones a <see cref="Notice"/> takes; each also shows its own mark, so status is never color alone.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Gives notices the design system's message tones (ADR-0019).
/// </remarks>
public enum NoticeTone
{
    /// <summary>Information, marked "i".</summary>
    Info,

    /// <summary>Something went wrong or was refused, marked "!".</summary>
    Danger,

    /// <summary>Something can't go on as asked and the reader can fix it, marked "!".</summary>
    Warning,
}
