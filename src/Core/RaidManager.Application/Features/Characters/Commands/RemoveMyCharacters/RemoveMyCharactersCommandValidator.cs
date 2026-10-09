using FluentValidation;

namespace RaidManager.Application.Features.Characters.Commands.RemoveMyCharacters;

/// <summary>Validates <see cref="RemoveMyCharactersCommand"/>.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-09<br/>
/// Purpose: Refuses an empty player before anything is loaded.
/// </remarks>
public sealed class RemoveMyCharactersCommandValidator : AbstractValidator<RemoveMyCharactersCommand>
{
    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="RemoveMyCharactersCommandValidator"/> class.</summary>
    public RemoveMyCharactersCommandValidator()
    {
        RuleFor(command => command.UserId).NotEmpty();
    }
    #endregion Constructors
}
