using FluentValidation;
using RaidManager.Application.Features.Companions.Abstractions;

namespace RaidManager.Application.Features.Companions.Commands.CollectCompanionToken;

/// <summary>Validates the shape of a <see cref="CollectCompanionTokenCommand"/> before it reaches its handler.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: Refuses an empty or oversized device code before it is hashed.
/// </remarks>
public sealed class CollectCompanionTokenCommandValidator : AbstractValidator<CollectCompanionTokenCommand>
{
    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="CollectCompanionTokenCommandValidator"/> class.</summary>
    public CollectCompanionTokenCommandValidator()
    {
        RuleFor(command => command.DeviceCode).NotEmpty().MaximumLength(CompanionPairingDefaults.MaximumSecretLength);
    }
    #endregion Constructors
}
