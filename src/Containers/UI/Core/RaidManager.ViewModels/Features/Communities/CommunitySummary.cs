namespace RaidManager.ViewModels.Features.Communities;

/// <summary>Describes a linked community as the API returns it.</summary>
/// <param name="CommunityId">The community identifier.</param>
/// <param name="DiscordGuildId">The linked Discord server snowflake.</param>
/// <param name="Name">The community name, taken from the Discord server.</param>
/// <param name="Realm">The Warmane realm's name, such as <c>Icecrown</c>.</param>
/// <param name="AdministratorId">The user who added the bot.</param>
/// <param name="AdministratorName">The Administrator's display name.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: The website's copy of the API's community contract, read by the community pages and the app shell.
/// </remarks>
public sealed record CommunitySummary(
    Guid CommunityId,
    string DiscordGuildId,
    string Name,
    string Realm,
    Guid AdministratorId,
    string AdministratorName);
