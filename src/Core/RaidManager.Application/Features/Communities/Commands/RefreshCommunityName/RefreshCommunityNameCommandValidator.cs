using FluentValidation;

namespace RaidManager.Application.Features.Communities.Commands.RefreshCommunityName;

/// <summary>Validates the shape of a <see cref="RefreshCommunityNameCommand"/> before it reaches its handler.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Rejects missing identifiers and malformed values at the command boundary.
/// </remarks>
public sealed class RefreshCommunityNameCommandValidator : AbstractValidator<RefreshCommunityNameCommand>
{
    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="RefreshCommunityNameCommandValidator"/> class.</summary>
    public RefreshCommunityNameCommandValidator()
    {
        RuleFor(command => command.CommunityId).NotEmpty();
        RuleFor(command => command.Name).NotEmpty().MaximumLength(RaidManager.Domain.Features.Communities.Aggregates.Community.MaximumNameLength);
    }
    #endregion Constructors
}
