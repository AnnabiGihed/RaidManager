using Pivot.Framework.Domain.Shared;

namespace RaidManager.Application.Features.Communities.Abstractions;

/// <summary>Reads a Discord server's name, roles and members with the bot.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Keeps Discord's server endpoints behind one application contract, so the community settings read current Discord data (ADR-0022). Infrastructure implements it with the bot token.
/// </remarks>
public interface IDiscordServers
{
    #region Methods
    /// <summary>Reads a server's current name and the roles that can be mapped.</summary>
    /// <param name="discordGuildId">The Discord server snowflake.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// The server; <see cref="DiscordErrors.BotNotInServer"/> when the bot was removed; or
    /// <see cref="DiscordErrors.Unavailable"/> when Discord couldn't answer.
    /// </returns>
    Task<Result<DiscordServer>> GetAsync(string discordGuildId, CancellationToken cancellationToken);

    /// <summary>Lists the people in a server, leaving bots out.</summary>
    /// <param name="discordGuildId">The Discord server snowflake.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// The members; <see cref="DiscordErrors.BotNotInServer"/> when the bot was removed; or
    /// <see cref="DiscordErrors.Unavailable"/> when Discord couldn't answer.
    /// </returns>
    Task<Result<IReadOnlyList<DiscordServerMember>>> ListMembersAsync(string discordGuildId, CancellationToken cancellationToken);
    #endregion Methods
}
