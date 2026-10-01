using FluentValidation;

namespace RaidManager.Application.Features.Communities.Queries.GetCommunityMembers;

/// <summary>Validates the shape of a <see cref="GetCommunityMembersQuery"/> before it reaches its handler.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Rejects a missing community or user identifier at the query boundary.
/// </remarks>
public sealed class GetCommunityMembersQueryValidator : AbstractValidator<GetCommunityMembersQuery>
{
    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="GetCommunityMembersQueryValidator"/> class.</summary>
    public GetCommunityMembersQueryValidator()
    {
        RuleFor(query => query.CommunityId).NotEmpty();
        RuleFor(query => query.UserId).NotEmpty();
    }
    #endregion Constructors
}
