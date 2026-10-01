using FluentValidation;
using RaidManager.Domain.Features.Communities.Aggregates;
using RaidManager.Domain.Features.Shared.Enums;

namespace RaidManager.Application.Features.Communities.Commands.LinkCommunity;

/// <summary>Validates the shape of a <see cref="LinkCommunityCommand"/> before it reaches its handler.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Rejects a malformed server id, a missing name, an unknown realm or a missing user at the command boundary.
/// </remarks>
public sealed class LinkCommunityCommandValidator : AbstractValidator<LinkCommunityCommand>
{
    #region Constants
    /// <summary>Defines the maximum decimal length of a Discord snowflake.</summary>
    private const int MaximumSnowflakeLength = 20;
    #endregion Constants

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="LinkCommunityCommandValidator"/> class.</summary>
    public LinkCommunityCommandValidator()
    {
        RuleFor(command => command.DiscordGuildId)
            .NotEmpty()
            .MaximumLength(MaximumSnowflakeLength)
            .Matches("^[0-9]+$").WithMessage("A Discord server id is a numeric snowflake.");
        RuleFor(command => command.Name).NotEmpty().MaximumLength(Community.MaximumNameLength);
        RuleFor(command => command.Realm)
            .Must(realm => Enum.GetNames<WarmaneRealm>().Contains(realm, StringComparer.Ordinal))
            .WithMessage("Choose one of the Warmane realms.");
        RuleFor(command => command.AdministratorUserId).NotEmpty();
    }
    #endregion Constructors
}
