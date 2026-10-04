using FluentValidation;
using RaidManager.Application.Features.Companions.Abstractions;

namespace RaidManager.Application.Features.Companions.Commands.StartCompanionPairing;

/// <summary>Validates the shape of a <see cref="StartCompanionPairingCommand"/> before it reaches its handler.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: Bounds the only input an unauthenticated caller sends.
/// </remarks>
public sealed class StartCompanionPairingCommandValidator : AbstractValidator<StartCompanionPairingCommand>
{
    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="StartCompanionPairingCommandValidator"/> class.</summary>
    public StartCompanionPairingCommandValidator()
    {
        RuleFor(command => command.ComputerLabel).MaximumLength(CompanionPairingDefaults.MaximumComputerLabelLength);
    }
    #endregion Constructors
}
