using FluentValidation;

namespace RaidManager.Application.Features.Characters.Commands.ApproveCharacterClaim;

/// <summary>Validates the shape of a <see cref="ApproveCharacterClaimCommand"/> before it reaches its handler.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-09-30<br/>
/// Purpose: Rejects missing identifiers at the command boundary; the claim rules themselves stay in the Character aggregate.
/// </remarks>
public sealed class ApproveCharacterClaimCommandValidator : AbstractValidator<ApproveCharacterClaimCommand>
{
    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="ApproveCharacterClaimCommandValidator"/> class.</summary>
    public ApproveCharacterClaimCommandValidator()
    {
        RuleFor(command => command.CharacterId).NotEmpty();
        RuleFor(command => command.UserId).NotEmpty();
    }
    #endregion Constructors
}
