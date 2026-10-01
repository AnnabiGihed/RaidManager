using Pivot.Framework.Domain.Shared;

namespace RaidManager.Application.Features.Communities.Abstractions;

/// <summary>Asks Discord whether a user is in a server and which roles they have there.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Keeps Discord's REST API behind one application contract, so every role check reads current Discord data
/// the same way (ADR-0022). Infrastructure implements it with the bot token and a short cache.
/// </remarks>
public interface IDiscordServerMembers
{
    #region Methods
    /// <summary>Finds a user's membership in a Discord server.</summary>
    /// <param name="discordGuildId">The Discord server snowflake.</param>
    /// <param name="discordUserId">The Discord user snowflake.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// The membership, which says whether the user is in the server and with which roles, or
    /// <see cref="DiscordErrors.Unavailable"/> when Discord couldn't answer.
    /// </returns>
    Task<Result<DiscordMembership>> FindAsync(string discordGuildId, string discordUserId, CancellationToken cancellationToken);
    #endregion Methods
}
