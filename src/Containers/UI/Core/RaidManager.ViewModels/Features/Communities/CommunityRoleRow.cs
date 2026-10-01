namespace RaidManager.ViewModels.Features.Communities;

/// <summary>Describes one RaidManager role in the roles card.</summary>
/// <param name="Role">The RaidManager role's name: Administrator, Officer, RaidLeader or Member.</param>
/// <param name="DiscordRoles">The Discord roles mapped to it.</param>
/// <param name="Members">How many people in the server have it as their highest role.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: One row of the roles card of board 4.
/// </remarks>
public sealed record CommunityRoleRow(string Role, IReadOnlyList<MappedDiscordRole> DiscordRoles, int Members);
