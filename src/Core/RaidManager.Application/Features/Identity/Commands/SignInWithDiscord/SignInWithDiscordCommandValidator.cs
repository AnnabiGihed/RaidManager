using FluentValidation;

namespace RaidManager.Application.Features.Identity.Commands.SignInWithDiscord;

/// <summary>Validates the shape of a <see cref="SignInWithDiscordCommand"/> before it reaches its handler.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-09-30<br/>
/// Purpose: Rejects identifiers and profile values Discord never issues, before the aggregate would throw on them.
/// </remarks>
public sealed class SignInWithDiscordCommandValidator : AbstractValidator<SignInWithDiscordCommand>
{
    #region Constants
    /// <summary>Defines the longest Discord snowflake, in digits.</summary>
    private const int MaximumSnowflakeLength = 20;

    /// <summary>Defines the longest stored display name.</summary>
    private const int MaximumDisplayNameLength = 100;

    /// <summary>Defines the longest stored avatar URL.</summary>
    private const int MaximumAvatarUrlLength = 512;
    #endregion Constants

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="SignInWithDiscordCommandValidator"/> class.</summary>
    public SignInWithDiscordCommandValidator()
    {
        RuleFor(command => command.DiscordUserId)
            .NotEmpty()
            .MaximumLength(MaximumSnowflakeLength)
            .Matches("^[0-9]+$").WithMessage("A Discord user id is a numeric snowflake.");
        RuleFor(command => command.DisplayName).NotEmpty().MaximumLength(MaximumDisplayNameLength);
        RuleFor(command => command.AvatarUrl).MaximumLength(MaximumAvatarUrlLength);
    }
    #endregion Constructors
}
