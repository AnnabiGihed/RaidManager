namespace RaidManager.ViewModels.Features.Characters;

/// <summary>Describes one character claim awaiting the player's decision, as the claims API returns it.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Mirrors the API's transport record, so the website never depends on domain types.
/// </remarks>
/// <param name="CharacterId">The character identifier.</param>
/// <param name="Realm">The Warmane realm name, for example <c>Icecrown</c>.</param>
/// <param name="Name">The character name.</param>
/// <param name="Class">The class name, for example <c>Death Knight</c>.</param>
/// <param name="Race">The race name, for example <c>Human</c>.</param>
/// <param name="Level">The character level.</param>
/// <param name="ClaimState"><see cref="PendingState"/>, or <see cref="ConflictState"/> when another player owns the character.</param>
/// <param name="RequestedAtUtc">When the companion found the character and the claim was requested.</param>
public sealed record CharacterClaim(
    Guid CharacterId,
    string Realm,
    string Name,
    string Class,
    string Race,
    int Level,
    string ClaimState,
    DateTimeOffset RequestedAtUtc)
{
    #region Constants
    /// <summary>Defines the state of a claim awaiting the player's decision.</summary>
    public const string PendingState = "Pending";

    /// <summary>Defines the state of a claim waiting for an officer because another player owns the character.</summary>
    public const string ConflictState = "Conflict";
    #endregion Constants

    #region Properties
    /// <summary>Gets a value indicating whether the player can approve or reject the claim.</summary>
    public bool IsPending => string.Equals(ClaimState, PendingState, StringComparison.Ordinal);
    #endregion Properties
}
