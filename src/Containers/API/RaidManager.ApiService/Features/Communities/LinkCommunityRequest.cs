namespace RaidManager.ApiService.Features.Communities;

/// <summary>Asks the API to link a Discord server as a community.</summary>
/// <param name="DiscordGuildId">The Discord server snowflake Discord confirmed when the bot was added.</param>
/// <param name="Name">The server name.</param>
/// <param name="Realm">The Warmane realm's name, such as <c>Icecrown</c>.</param>
/// <param name="AdministratorUserId">The signed-in user who added the bot.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: The API contract the website sends after the realm is chosen (board 2 of the community settings mockup).
/// </remarks>
public sealed record LinkCommunityRequest(string DiscordGuildId, string Name, string Realm, Guid AdministratorUserId);
