using FluentValidation;

namespace RaidManager.Application.Features.Communities.Queries.GetUserCommunities;

/// <summary>Validates the shape of a <see cref="GetUserCommunitiesQuery"/> before it reaches its handler.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Rejects a missing user identifier, and more member communities than Discord's server limit allows, at the
/// query boundary.
/// </remarks>
public sealed class GetUserCommunitiesQueryValidator : AbstractValidator<GetUserCommunitiesQuery>
{
    #region Constants
    /// <summary>Defines the most servers a Discord account can be in, and so the most communities it can match.</summary>
    public const int MaximumMemberCommunities = 200;
    #endregion Constants

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="GetUserCommunitiesQueryValidator"/> class.</summary>
    public GetUserCommunitiesQueryValidator()
    {
        RuleFor(query => query.UserId).NotEmpty();
        RuleFor(query => query.MemberOf).NotNull().Must(ids => ids.Count <= MaximumMemberCommunities)
            .WithMessage($"A user is a member of at most {MaximumMemberCommunities} communities.");
        RuleForEach(query => query.MemberOf).NotEmpty();
    }
    #endregion Constructors
}
