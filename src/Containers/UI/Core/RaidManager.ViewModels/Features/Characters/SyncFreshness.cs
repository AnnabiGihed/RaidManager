namespace RaidManager.ViewModels.Features.Characters;

/// <summary>Says how fresh a character's synchronized data is.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-05<br/>
/// Purpose: Chooses the tone of the last sync badge on board 1: data older than three days may be out of date.
/// </remarks>
public enum SyncFreshness
{
    /// <summary>The latest sync is at most three days old.</summary>
    Fresh,

    /// <summary>The latest sync is older than three days.</summary>
    Stale,

    /// <summary>The character was never synchronized.</summary>
    Never,
}
