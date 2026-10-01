using FluentValidation;

namespace RaidManager.Application.Features.Communities.Queries.GetCommunityByDiscordServer;

/// <summary>Validates the shape of a <see cref="GetCommunityByDiscordServerQuery"/> before it reaches its handler.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Rejects a server id that is not a Discord snowflake at the query boundary.
/// </remarks>
public sealed class GetCommunityByDiscordServerQueryValidator : AbstractValidator<GetCommunityByDiscordServerQuery>
{
    #region Constants
    /// <summary>Defines the maximum decimal length of a Discord snowflake.</summary>
    private const int MaximumSnowflakeLength = 20;
    #endregion Constants

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="GetCommunityByDiscordServerQueryValidator"/> class.</summary>
    public GetCommunityByDiscordServerQueryValidator()
    {
        RuleFor(query => query.DiscordGuildId)
            .NotEmpty()
            .MaximumLength(MaximumSnowflakeLength)
            .Matches("^[0-9]+$").WithMessage("A Discord server id is a numeric snowflake.");
    }
    #endregion Constructors
}
