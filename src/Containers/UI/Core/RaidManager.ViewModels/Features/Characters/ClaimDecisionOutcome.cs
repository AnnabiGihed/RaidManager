namespace RaidManager.ViewModels.Features.Characters;

/// <summary>Describes how the API answered an approve or reject request.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Separates a recorded decision from one the API refused because the claim changed meanwhile.
/// </remarks>
public enum ClaimDecisionOutcome
{
    /// <summary>The decision was recorded.</summary>
    Recorded,

    /// <summary>The API refused the decision: the claim is no longer pending, or another player owns the character.</summary>
    Refused,
}
