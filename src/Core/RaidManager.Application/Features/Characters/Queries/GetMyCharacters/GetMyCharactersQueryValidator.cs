using FluentValidation;

namespace RaidManager.Application.Features.Characters.Queries.GetMyCharacters;

/// <summary>Validates the shape of a <see cref="GetMyCharactersQuery"/> before it reaches its handler.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-05<br/>
/// Purpose: Rejects a missing player identifier at the query boundary.
/// </remarks>
public sealed class GetMyCharactersQueryValidator : AbstractValidator<GetMyCharactersQuery>
{
    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="GetMyCharactersQueryValidator"/> class.</summary>
    public GetMyCharactersQueryValidator()
    {
        RuleFor(query => query.UserId).NotEmpty();
    }
    #endregion Constructors
}
