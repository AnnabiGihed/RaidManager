using RaidManager.ViewModels.Features.Characters;

namespace RaidManager.Web.Tests.Support;

/// <summary>Stands in for the API's character claim endpoints during website tests.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Lets the tests choose the player's claims, fail the API, and see the decisions the website sent.
/// </remarks>
public sealed class FakeCharacterClaimsApiClient : ICharacterClaimsApiClient
{
    #region Properties
    /// <summary>Gets or sets the claims the API returns.</summary>
    public List<CharacterClaim> Claims { get; set; } = [];

    /// <summary>Gets or sets a value indicating whether listing the claims fails as an unavailable API would.</summary>
    public bool Fails { get; set; }

    /// <summary>Gets the decisions received, as <c>approve</c> or <c>reject</c> with the user and character.</summary>
    public List<(string Decision, Guid UserId, Guid CharacterId)> Decisions { get; } = [];
    #endregion Properties

    #region Public Methods
    /// <summary>Builds a claim for a test.</summary>
    /// <param name="name">The character name.</param>
    /// <param name="state">The claim state.</param>
    /// <returns>The claim.</returns>
    public static CharacterClaim Claim(string name, string state = CharacterClaim.PendingState) =>
        new(Guid.NewGuid(), "Icecrown", name, "Death Knight", "Human", 80, state, DateTimeOffset.UtcNow);

    /// <inheritdoc />
    public Task<IReadOnlyList<CharacterClaim>> GetPendingAsync(Guid userId, CancellationToken cancellationToken) =>
        Fails
            ? throw new HttpRequestException("The API is unavailable.")
            : Task.FromResult<IReadOnlyList<CharacterClaim>>([.. Claims]);

    /// <inheritdoc />
    public Task<ClaimDecisionOutcome> ApproveAsync(Guid userId, Guid characterId, CancellationToken cancellationToken) =>
        Decide("approve", userId, characterId);

    /// <inheritdoc />
    public Task<ClaimDecisionOutcome> RejectAsync(Guid userId, Guid characterId, CancellationToken cancellationToken) =>
        Decide("reject", userId, characterId);
    #endregion Public Methods

    #region Private Helpers
    /// <summary>Records a decision and removes the claim, as the API would.</summary>
    /// <param name="decision">The decision name.</param>
    /// <param name="userId">The player.</param>
    /// <param name="characterId">The character.</param>
    /// <returns>A recorded outcome.</returns>
    private Task<ClaimDecisionOutcome> Decide(string decision, Guid userId, Guid characterId)
    {
        Decisions.Add((decision, userId, characterId));
        Claims.RemoveAll(claim => claim.CharacterId == characterId);
        return Task.FromResult(ClaimDecisionOutcome.Recorded);
    }
    #endregion Private Helpers
}
