using FluentValidation;

namespace RaidManager.Application.Features.Communities.Queries.GetUserCommunities;

/// <summary>Validates the shape of a <see cref="GetUserCommunitiesQuery"/> before it reaches its handler.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Rejects a missing user identifier at the query boundary.
/// </remarks>
public sealed class GetUserCommunitiesQueryValidator : AbstractValidator<GetUserCommunitiesQuery>
{
    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="GetUserCommunitiesQueryValidator"/> class.</summary>
    public GetUserCommunitiesQueryValidator()
    {
        RuleFor(query => query.UserId).NotEmpty();
    }
    #endregion Constructors
}
