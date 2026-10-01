using FluentValidation;

namespace RaidManager.Application.Features.Communities.Queries.GetCommunityRoleSettings;

/// <summary>Validates the shape of a <see cref="GetCommunityRoleSettingsQuery"/> before it reaches its handler.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Rejects a missing community or user identifier at the query boundary.
/// </remarks>
public sealed class GetCommunityRoleSettingsQueryValidator : AbstractValidator<GetCommunityRoleSettingsQuery>
{
    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="GetCommunityRoleSettingsQueryValidator"/> class.</summary>
    public GetCommunityRoleSettingsQueryValidator()
    {
        RuleFor(query => query.CommunityId).NotEmpty();
        RuleFor(query => query.UserId).NotEmpty();
    }
    #endregion Constructors
}
