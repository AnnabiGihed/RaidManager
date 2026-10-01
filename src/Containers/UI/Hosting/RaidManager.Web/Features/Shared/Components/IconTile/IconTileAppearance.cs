namespace RaidManager.Web.Features.Shared.Components;

/// <summary>Names the looks of an <see cref="IconTile"/>.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Lets the same tile show a neutral glyph or a community's initials in the community color of ADR-0019.
/// </remarks>
public enum IconTileAppearance
{
    /// <summary>A raised, outlined tile with a muted glyph, such as "+".</summary>
    Neutral,

    /// <summary>A filled tile in the community color, for a community's initials.</summary>
    Community,
}
