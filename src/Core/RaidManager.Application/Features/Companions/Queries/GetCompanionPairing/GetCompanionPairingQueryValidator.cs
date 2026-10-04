using FluentValidation;
using RaidManager.Domain.Features.Companions.ValueObjects;

namespace RaidManager.Application.Features.Companions.Queries.GetCompanionPairing;

/// <summary>Validates the shape of a <see cref="GetCompanionPairingQuery"/> before it reaches its handler.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: Refuses a missing player or a value that can't be a pairing code before any lookup.
/// </remarks>
public sealed class GetCompanionPairingQueryValidator : AbstractValidator<GetCompanionPairingQuery>
{
    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="GetCompanionPairingQueryValidator"/> class.</summary>
    public GetCompanionPairingQueryValidator()
    {
        RuleFor(query => query.UserId).NotEmpty();
        RuleFor(query => query.PairingCode)
            .Must(code => PairingCode.TryCreate(code, out _))
            .WithMessage($"A pairing code is {PairingCode.Length} characters from {PairingCode.Alphabet}.");
    }
    #endregion Constructors
}
