using FluentValidation;

namespace RaidManager.Application.Features.Communities.Commands.UnmapCommunityRole;

/// <summary>Validates the shape of a <see cref="UnmapCommunityRoleCommand"/> before it reaches its handler.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Rejects missing identifiers and malformed values at the command boundary.
/// </remarks>
public sealed class UnmapCommunityRoleCommandValidator : AbstractValidator<UnmapCommunityRoleCommand>
{
    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="UnmapCommunityRoleCommandValidator"/> class.</summary>
    public UnmapCommunityRoleCommandValidator()
    {
        RuleFor(command => command.CommunityId).NotEmpty();
        RuleFor(command => command.UserId).NotEmpty();
        RuleFor(command => command.DiscordRoleId).NotEmpty().MaximumLength(20).Matches("^[0-9]+$").WithMessage("A Discord role id is a numeric snowflake.");
    }
    #endregion Constructors
}
