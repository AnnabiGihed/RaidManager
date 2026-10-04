using FluentValidation;

namespace RaidManager.Application.Features.Companions.Queries.GetCompanions;

/// <summary>Validates the shape of a <see cref="GetCompanionsQuery"/> before it reaches its handler.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: Rejects a missing player identifier at the query boundary.
/// </remarks>
public sealed class GetCompanionsQueryValidator : AbstractValidator<GetCompanionsQuery>
{
    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="GetCompanionsQueryValidator"/> class.</summary>
    public GetCompanionsQueryValidator()
    {
        RuleFor(query => query.UserId).NotEmpty();
    }
    #endregion Constructors
}
