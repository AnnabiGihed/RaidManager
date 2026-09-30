using RaidManager.Application.Features.Characters.Queries.GetPendingCharacterClaims;

namespace RaidManager.ApiService.Features.Characters;

/// <summary>Describes one character claim awaiting the player's decision, as the website receives it.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-09-30<br/>
/// Purpose: Keeps the wire format separate from domain types: realm, class, race and claim state travel as names.
/// </remarks>
/// <param name="CharacterId">The character identifier.</param>
/// <param name="Realm">The Warmane realm name, for example <c>Icecrown</c>.</param>
/// <param name="Name">The character name.</param>
/// <param name="Class">The class name, for example <c>Mage</c>.</param>
/// <param name="Race">The race name, for example <c>Human</c>.</param>
/// <param name="Level">The character level.</param>
/// <param name="ClaimState"><c>Pending</c>, or <c>Conflict</c> when another player owns the character.</param>
/// <param name="RequestedAtUtc">When the claim was requested.</param>
public sealed record PendingCharacterClaim(
    Guid CharacterId,
    string Realm,
    string Name,
    string Class,
    string Race,
    int Level,
    string ClaimState,
    DateTimeOffset RequestedAtUtc)
{
    #region Public Methods
    /// <summary>Maps the application response to the transport record.</summary>
    /// <param name="claim">The application response.</param>
    /// <returns>The transport record.</returns>
    public static PendingCharacterClaim From(PendingCharacterClaimResponse claim)
    {
        ArgumentNullException.ThrowIfNull(claim);
        return new PendingCharacterClaim(
            claim.CharacterId,
            claim.Realm.ToString(),
            claim.Name,
            claim.Class.ToString(),
            claim.Race.ToString(),
            claim.Level,
            claim.ClaimState.ToString(),
            claim.RequestedAtUtc);
    }
    #endregion Public Methods
}
