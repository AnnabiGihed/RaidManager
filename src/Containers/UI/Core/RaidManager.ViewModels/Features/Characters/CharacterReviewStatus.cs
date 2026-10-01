namespace RaidManager.ViewModels.Features.Characters;

/// <summary>Names the states of the character review page.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Lets the page show a progress indicator, the list, or the load error.
/// </remarks>
public enum CharacterReviewStatus
{
    /// <summary>The claims are loading.</summary>
    Loading,

    /// <summary>The claims are shown.</summary>
    Ready,

    /// <summary>The claims could not be loaded.</summary>
    Failed,
}
