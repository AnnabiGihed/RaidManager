namespace RaidManager.ViewModels.Features.Companions;

/// <summary>Describes how the API answered a revocation.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: Separates a recorded revocation from one the API refused because the companion changed meanwhile.
/// </remarks>
public enum RevokeOutcome
{
    /// <summary>The companion was revoked.</summary>
    Revoked,

    /// <summary>The API refused: the companion was already revoked or isn't the player's.</summary>
    Refused,
}
