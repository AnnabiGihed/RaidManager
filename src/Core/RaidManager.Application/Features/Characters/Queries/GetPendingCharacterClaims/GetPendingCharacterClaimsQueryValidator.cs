using FluentValidation;

namespace RaidManager.Application.Features.Characters.Queries.GetPendingCharacterClaims;

/// <summary>Validates the shape of a <see cref="GetPendingCharacterClaimsQuery"/> before it reaches its handler.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-09-30<br/>
/// Purpose: Rejects a missing player identifier at the query boundary.
/// </remarks>
public sealed class GetPendingCharacterClaimsQueryValidator : AbstractValidator<GetPendingCharacterClaimsQuery>
{
    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="GetPendingCharacterClaimsQueryValidator"/> class.</summary>
    public GetPendingCharacterClaimsQueryValidator()
    {
        RuleFor(query => query.UserId).NotEmpty();
    }
    #endregion Constructors
}
