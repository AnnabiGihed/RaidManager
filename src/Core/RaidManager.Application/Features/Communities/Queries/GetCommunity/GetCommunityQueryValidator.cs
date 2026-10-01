using FluentValidation;

namespace RaidManager.Application.Features.Communities.Queries.GetCommunity;

/// <summary>Validates the shape of a <see cref="GetCommunityQuery"/> before it reaches its handler.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Rejects a missing community identifier at the query boundary.
/// </remarks>
public sealed class GetCommunityQueryValidator : AbstractValidator<GetCommunityQuery>
{
    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="GetCommunityQueryValidator"/> class.</summary>
    public GetCommunityQueryValidator()
    {
        RuleFor(query => query.CommunityId).NotEmpty();
    }
    #endregion Constructors
}
