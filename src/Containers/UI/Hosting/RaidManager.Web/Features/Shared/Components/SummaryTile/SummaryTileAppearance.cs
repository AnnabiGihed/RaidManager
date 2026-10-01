namespace RaidManager.Web.Features.Shared.Components;

/// <summary>Names how a summary tile is framed.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Lets the same tile show as a bordered card (the community card) or plain (the user card).
/// </remarks>
public enum SummaryTileAppearance
{
    /// <summary>A bordered card on the card surface.</summary>
    Card,

    /// <summary>No frame; the surrounding surface shows.</summary>
    Plain,
}
