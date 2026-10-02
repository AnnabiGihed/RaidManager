using Pivot.Framework.Application.Abstractions.Messaging.Commands;

namespace RaidManager.Application.Features.Communities.Commands.MapCommunityRole;

/// <summary>Maps a Discord role to Officer or Raid leader in a community.</summary>
/// <param name="CommunityId">The community.</param>
/// <param name="UserId">The signed-in user; only the Administrator may.</param>
/// <param name="DiscordRoleId">The Discord role snowflake.</param>
/// <param name="RoleId">The community role.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Lets the Administrator choose which Discord roles give RaidManager permissions (board 4); the role must be one of the server's mappable roles now.
/// </remarks>
public sealed record MapCommunityRoleCommand(Guid CommunityId, Guid UserId, string DiscordRoleId, Guid RoleId) : ICommand;
