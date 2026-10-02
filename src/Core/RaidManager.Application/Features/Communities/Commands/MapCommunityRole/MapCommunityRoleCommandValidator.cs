using FluentValidation;

namespace RaidManager.Application.Features.Communities.Commands.MapCommunityRole;

/// <summary>Validates the shape of a <see cref="MapCommunityRoleCommand"/> before it reaches its handler.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Rejects missing identifiers and malformed values at the command boundary.
/// </remarks>
public sealed class MapCommunityRoleCommandValidator : AbstractValidator<MapCommunityRoleCommand>
{
    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="MapCommunityRoleCommandValidator"/> class.</summary>
    public MapCommunityRoleCommandValidator()
    {
        RuleFor(command => command.CommunityId).NotEmpty();
        RuleFor(command => command.UserId).NotEmpty();
        RuleFor(command => command.DiscordRoleId).NotEmpty().MaximumLength(20).Matches("^[0-9]+$").WithMessage("A Discord role id is a numeric snowflake.");
        RuleFor(command => command.RoleId).NotEmpty();
    }
    #endregion Constructors
}
