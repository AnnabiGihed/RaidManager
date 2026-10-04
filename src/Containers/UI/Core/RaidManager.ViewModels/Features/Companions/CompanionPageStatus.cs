namespace RaidManager.ViewModels.Features.Companions;

/// <summary>Identifies whether a companion page's data is loading, shown, or could not be loaded.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: Drives the loading text and the error notice of the companion pages.
/// </remarks>
public enum CompanionPageStatus
{
    /// <summary>The data is loading.</summary>
    Loading,

    /// <summary>The data is shown.</summary>
    Ready,

    /// <summary>The API couldn't be reached or refused the call.</summary>
    Failed,
}
