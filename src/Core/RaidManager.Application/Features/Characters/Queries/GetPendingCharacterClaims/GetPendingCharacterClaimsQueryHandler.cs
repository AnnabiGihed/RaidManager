using Pivot.Framework.Application.Abstractions.Messaging.Queries;
using Pivot.Framework.Domain.Shared;
using RaidManager.Application.Features.Characters.Abstractions;
using RaidManager.Domain.Features.Shared.Identifiers;

namespace RaidManager.Application.Features.Characters.Queries.GetPendingCharacterClaims;

/// <summary>Handles <see cref="GetPendingCharacterClaimsQuery"/> through the read-only claim reader.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-09-30<br/>
/// Purpose: Answers the review page's question without touching the write model.
/// </remarks>
internal sealed class GetPendingCharacterClaimsQueryHandler : IQueryHandler<GetPendingCharacterClaimsQuery, IReadOnlyList<PendingCharacterClaimResponse>>
{
    #region Fields
    /// <summary>Stores the read-only claim reader.</summary>
    private readonly ICharacterClaimReader _claims;
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="GetPendingCharacterClaimsQueryHandler"/> class.</summary>
    /// <param name="claims">The read-only claim reader.</param>
    public GetPendingCharacterClaimsQueryHandler(ICharacterClaimReader claims)
    {
        _claims = claims;
    }
    #endregion Constructors

    #region Public Methods
    /// <summary>Lists the player's claims awaiting a decision.</summary>
    /// <param name="request">The query.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The claims awaiting a decision; an empty list when there are none.</returns>
    public async Task<Result<IReadOnlyList<PendingCharacterClaimResponse>>> Handle(GetPendingCharacterClaimsQuery request, CancellationToken cancellationToken) =>
        Result.Success(await _claims.ListAwaitingDecisionAsync(new UserId(request.UserId), cancellationToken));
    #endregion Public Methods
}
