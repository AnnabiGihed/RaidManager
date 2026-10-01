using FluentValidation;

namespace RaidManager.Application.Features.Communities.Queries.GetCommunityRole;

/// <summary>Validates the shape of a <see cref="GetCommunityRoleQuery"/> before it reaches its handler.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Rejects a missing community or user identifier at the query boundary.
/// </remarks>
public sealed class GetCommunityRoleQueryValidator : AbstractValidator<GetCommunityRoleQuery>
{
    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="GetCommunityRoleQueryValidator"/> class.</summary>
    public GetCommunityRoleQueryValidator()
    {
        RuleFor(query => query.CommunityId).NotEmpty();
        RuleFor(query => query.UserId).NotEmpty();
    }
    #endregion Constructors
}
