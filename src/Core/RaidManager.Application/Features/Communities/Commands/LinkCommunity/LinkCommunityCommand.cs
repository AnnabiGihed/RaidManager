using Pivot.Framework.Application.Abstractions.Messaging.Commands;

namespace RaidManager.Application.Features.Communities.Commands.LinkCommunity;

/// <summary>Links a Discord server, to which the bot was just added, as a RaidManager community.</summary>
/// <param name="DiscordGuildId">The Discord server snowflake Discord confirmed.</param>
/// <param name="Name">The server name.</param>
/// <param name="Realm">The Warmane realm's name, such as <c>Icecrown</c>.</param>
/// <param name="AdministratorUserId">The signed-in user who added the bot; they become the Administrator.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: The last step of adding RaidManager to a server (story #14): the realm is chosen, and the community exists.
/// </remarks>
public sealed record LinkCommunityCommand(string DiscordGuildId, string Name, string Realm, Guid AdministratorUserId) : ICommand<Guid>;
