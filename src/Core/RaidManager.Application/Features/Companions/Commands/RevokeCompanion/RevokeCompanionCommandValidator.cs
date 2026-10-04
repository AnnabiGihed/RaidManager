using FluentValidation;

namespace RaidManager.Application.Features.Companions.Commands.RevokeCompanion;

/// <summary>Validates the shape of a <see cref="RevokeCompanionCommand"/> before it reaches its handler.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: Rejects missing identifiers at the command boundary.
/// </remarks>
public sealed class RevokeCompanionCommandValidator : AbstractValidator<RevokeCompanionCommand>
{
    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="RevokeCompanionCommandValidator"/> class.</summary>
    public RevokeCompanionCommandValidator()
    {
        RuleFor(command => command.UserId).NotEmpty();
        RuleFor(command => command.CompanionId).NotEmpty();
    }
    #endregion Constructors
}
