namespace RaidManager.Web.Features.Communities;

/// <summary>Describes a bot install as Discord confirmed it.</summary>
/// <param name="GuildId">The Discord server snowflake the bot was added to.</param>
/// <param name="GuildName">The server name.</param>
/// <param name="InstallerDiscordUserId">The Discord account that added the bot.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: The trusted result of Discord's exchange, compared with the signed-in account before anything is linked.
/// </remarks>
public sealed record DiscordInstall(string GuildId, string GuildName, string InstallerDiscordUserId);
