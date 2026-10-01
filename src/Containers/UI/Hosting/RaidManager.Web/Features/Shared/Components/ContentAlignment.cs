namespace RaidManager.Web.Features.Shared.Components;

/// <summary>Names how a block of text is aligned.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Signed-in pages align text to the start; signed-out pages center it (ADR-0019).
/// </remarks>
public enum ContentAlignment
{
    /// <summary>Aligned to the start of the line.</summary>
    Start,

    /// <summary>Centered.</summary>
    Center,
}
