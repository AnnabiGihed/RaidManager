namespace RaidManager.ViewModels.Features.Communities;

/// <summary>Names the states of a community page.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Shared by the community view models, so each page shows loading, its content, a missing community or a retry.
/// </remarks>
public enum CommunityPageStatus
{
    /// <summary>The page is asking the API.</summary>
    Loading,

    /// <summary>The page has what it shows.</summary>
    Ready,

    /// <summary>The community, or the pending link, doesn't exist or expired.</summary>
    Missing,

    /// <summary>The API couldn't answer; the page offers a retry.</summary>
    Failed,
}
