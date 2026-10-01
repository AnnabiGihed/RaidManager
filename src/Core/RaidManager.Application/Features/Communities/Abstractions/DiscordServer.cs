namespace RaidManager.Application.Features.Communities.Abstractions;

/// <summary>Describes a Discord server as Discord reports it now: its name and the roles that can give a RaidManager role.</summary>
/// <param name="Name">The server's current name.</param>
/// <param name="Roles">The roles people are given, highest first: not @everyone and not roles managed by an integration.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Feeds the community's roles card and refreshes the stored server name (owner decision on #14).
/// </remarks>
public sealed record DiscordServer(string Name, IReadOnlyList<DiscordServerRole> Roles);
