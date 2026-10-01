using Pivot.Framework.Application.Abstractions.Messaging.Commands;

namespace RaidManager.Application.Features.Communities.Commands.UnmapCommunityRole;

/// <summary>Removes a Discord role's mapping in a community.</summary>
/// <param name="CommunityId">The community.</param>
/// <param name="UserId">The signed-in user; only the Administrator may.</param>
/// <param name="DiscordRoleId">The Discord role snowflake.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Lets the Administrator stop a Discord role giving a RaidManager role, including a role deleted in Discord since.
/// </remarks>
public sealed record UnmapCommunityRoleCommand(Guid CommunityId, Guid UserId, string DiscordRoleId) : ICommand;
