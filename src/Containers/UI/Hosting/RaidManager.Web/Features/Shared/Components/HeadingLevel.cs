namespace RaidManager.Web.Features.Shared.Components;

/// <summary>Names the HTML heading level a component renders its title with.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Keeps the page outline right: the page's main title is an h1, titles under it are h2 or h3.
/// </remarks>
public enum HeadingLevel
{
    /// <summary>The page's main heading.</summary>
    H1,

    /// <summary>A section heading.</summary>
    H2,

    /// <summary>A subsection heading.</summary>
    H3,
}
