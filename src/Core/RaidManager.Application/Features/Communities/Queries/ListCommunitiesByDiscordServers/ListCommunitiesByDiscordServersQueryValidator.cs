using FluentValidation;
using RaidManager.Application.Features.Communities.Queries.GetUserCommunities;

namespace RaidManager.Application.Features.Communities.Queries.ListCommunitiesByDiscordServers;

/// <summary>Validates the shape of a <see cref="ListCommunitiesByDiscordServersQuery"/> before it reaches its handler.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-02<br/>
/// Purpose: Rejects more servers than a Discord account can be in, and server ids that aren't Discord snowflakes.
/// </remarks>
public sealed class ListCommunitiesByDiscordServersQueryValidator : AbstractValidator<ListCommunitiesByDiscordServersQuery>
{
    #region Constants
    /// <summary>Defines the maximum decimal length of a Discord snowflake.</summary>
    private const int MaximumSnowflakeLength = 20;
    #endregion Constants

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="ListCommunitiesByDiscordServersQueryValidator"/> class.</summary>
    public ListCommunitiesByDiscordServersQueryValidator()
    {
        RuleFor(query => query.DiscordGuildIds).NotNull().Must(ids => ids.Count <= GetUserCommunitiesQueryValidator.MaximumMemberCommunities)
            .WithMessage($"A Discord account is in at most {GetUserCommunitiesQueryValidator.MaximumMemberCommunities} servers.");
        RuleForEach(query => query.DiscordGuildIds)
            .NotEmpty()
            .MaximumLength(MaximumSnowflakeLength)
            .Matches("^[0-9]+$").WithMessage("A Discord server id is a numeric snowflake.");
    }
    #endregion Constructors
}
