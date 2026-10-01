namespace RaidManager.Web.Features.Shared.Components;

/// <summary>Names the tones of a <see cref="TagChip"/>; the chip's text says the same, so status is never color alone.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Gives chips the design system's badge tones (ADR-0019).
/// </remarks>
public enum TagChipTone
{
    /// <summary>An ordinary tag, such as a mapped Discord role.</summary>
    Info,

    /// <summary>A tag that needs attention, such as a Discord role deleted since it was mapped.</summary>
    Danger,

    /// <summary>A tag that confirms something, such as an officer role.</summary>
    Success,

    /// <summary>A plain tag, such as the Member role.</summary>
    Neutral,
}
