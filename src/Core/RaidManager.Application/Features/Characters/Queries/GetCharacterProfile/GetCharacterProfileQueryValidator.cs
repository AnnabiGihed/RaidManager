using FluentValidation;

namespace RaidManager.Application.Features.Characters.Queries.GetCharacterProfile;

/// <summary>Validates the shape of a <see cref="GetCharacterProfileQuery"/> before it reaches its handler.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-05<br/>
/// Purpose: Rejects a missing player or character identifier at the query boundary.
/// </remarks>
public sealed class GetCharacterProfileQueryValidator : AbstractValidator<GetCharacterProfileQuery>
{
    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="GetCharacterProfileQueryValidator"/> class.</summary>
    public GetCharacterProfileQueryValidator()
    {
        RuleFor(query => query.UserId).NotEmpty();
        RuleFor(query => query.CharacterId).NotEmpty();
    }
    #endregion Constructors
}
