namespace RaidManager.ViewModels.Features.Communities;

/// <summary>Describes a Discord server the bot was just added to, waiting for its realm before it is linked.</summary>
/// <param name="DiscordGuildId">The Discord server snowflake, as Discord confirmed it.</param>
/// <param name="ServerName">The server name, as Discord gave it.</param>
/// <param name="UserId">The signed-in user who added the bot.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Carries what Discord confirmed from its return to the realm choice (board 2); the website protects it so a
/// visitor can't change the server or the user on the way.
/// </remarks>
public sealed record PendingCommunityLink(string DiscordGuildId, string ServerName, Guid UserId);
