namespace RaidManager.ApiService.Features.Communities;

/// <summary>Describes one person in a community's Discord server.</summary>
/// <param name="DiscordUserId">The Discord user snowflake.</param>
/// <param name="DisplayName">The name shown in the server.</param>
/// <param name="AvatarUrl">The Discord picture's address, or <see langword="null"/>.</param>
/// <param name="DiscordRoles">The person's Discord roles, highest first.</param>
/// <param name="Roles">The roles they have: <c>Administrator</c> alone for the Administrator, otherwise every role their Discord roles give, otherwise <c>Member</c>.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: One row of the members page's contract.
/// </remarks>
public sealed record CommunityMember(string DiscordUserId, string DisplayName, string? AvatarUrl, IReadOnlyList<DiscordRoleOption> DiscordRoles, IReadOnlyList<string> Roles);
