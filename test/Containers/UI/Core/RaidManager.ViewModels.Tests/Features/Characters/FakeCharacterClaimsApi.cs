using RaidManager.ViewModels.Features.Characters;

namespace RaidManager.ViewModels.Tests.Features.Characters;

/// <summary>Stands in for the claims API and records the decisions it receives.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Lets the view model tests choose what the API returns, refuses, or fails.
/// </remarks>
internal sealed class FakeCharacterClaimsApi : ICharacterClaimsApiClient
{
    #region Properties
    /// <summary>Gets or sets the claims the next load returns.</summary>
    public List<CharacterClaim> Claims { get; set; } = [];

    /// <summary>Gets or sets the exception the next load throws, if any.</summary>
    public Exception? LoadFailure { get; set; }

    /// <summary>Gets or sets how the API answers decisions.</summary>
    public ClaimDecisionOutcome Outcome { get; set; } = ClaimDecisionOutcome.Recorded;

    /// <summary>Gets or sets the exception decisions throw, if any.</summary>
    public Exception? DecisionFailure { get; set; }

    /// <summary>Gets or sets the claims a load returns after a refused decision, if they differ.</summary>
    public List<CharacterClaim>? ClaimsAfterRefusal { get; set; }

    /// <summary>Gets the number of loads.</summary>
    public int Loads { get; private set; }

    /// <summary>Gets the decisions received, as <c>approve</c> or <c>reject</c> with the user and character.</summary>
    public List<(string Decision, Guid UserId, Guid CharacterId)> Decisions { get; } = [];
    #endregion Properties

    #region Public Methods
    /// <inheritdoc />
    public Task<IReadOnlyList<CharacterClaim>> GetPendingAsync(Guid userId, CancellationToken cancellationToken)
    {
        Loads++;
        if (LoadFailure is not null)
        {
            throw LoadFailure;
        }

        return Task.FromResult<IReadOnlyList<CharacterClaim>>([.. Claims]);
    }

    /// <inheritdoc />
    public Task<ClaimDecisionOutcome> ApproveAsync(Guid userId, Guid characterId, CancellationToken cancellationToken) =>
        Decide("approve", userId, characterId);

    /// <inheritdoc />
    public Task<ClaimDecisionOutcome> RejectAsync(Guid userId, Guid characterId, CancellationToken cancellationToken) =>
        Decide("reject", userId, characterId);
    #endregion Public Methods

    #region Private Helpers
    /// <summary>Records a decision and answers it.</summary>
    /// <param name="decision">The decision name.</param>
    /// <param name="userId">The player.</param>
    /// <param name="characterId">The character.</param>
    /// <returns>The configured outcome.</returns>
    private Task<ClaimDecisionOutcome> Decide(string decision, Guid userId, Guid characterId)
    {
        Decisions.Add((decision, userId, characterId));
        if (DecisionFailure is not null)
        {
            throw DecisionFailure;
        }

        if (Outcome == ClaimDecisionOutcome.Refused && ClaimsAfterRefusal is not null)
        {
            Claims = ClaimsAfterRefusal;
        }

        return Task.FromResult(Outcome);
    }
    #endregion Private Helpers
}
