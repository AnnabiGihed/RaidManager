namespace RaidManager.Application.Features.Communities.Abstractions;

/// <summary>Describes a role of a Discord server, as Discord reports it.</summary>
/// <param name="Id">The role snowflake.</param>
/// <param name="Name">The role name.</param>
/// <param name="Position">The role's position; higher roles come first in Discord.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Lists the roles a community's Administrator can map; only roles people are given can be mapped (checked on 2026-10-01: Discord also returns @everyone and roles managed by other bots).
/// </remarks>
public sealed record DiscordServerRole(string Id, string Name, int Position);
