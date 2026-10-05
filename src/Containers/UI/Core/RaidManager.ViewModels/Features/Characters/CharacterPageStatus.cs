namespace RaidManager.ViewModels.Features.Characters;

/// <summary>Identifies what a character page shows.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-05<br/>
/// Purpose: Lets the My characters and profile pages choose between loading, the data, a missing character and an error.
/// </remarks>
public enum CharacterPageStatus
{
    /// <summary>The data is loading.</summary>
    Loading,

    /// <summary>The data is shown.</summary>
    Ready,

    /// <summary>The character doesn't exist or isn't the player's.</summary>
    NotFound,

    /// <summary>The data could not be loaded.</summary>
    Failed,
}
