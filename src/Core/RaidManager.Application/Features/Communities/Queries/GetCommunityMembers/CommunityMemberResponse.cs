using RaidManager.Application.Features.Communities.Abstractions;

namespace RaidManager.Application.Features.Communities.Queries.GetCommunityMembers;

/// <summary>Describes one person in a community's Discord server.</summary>
/// <param name="DiscordUserId">The Discord user snowflake.</param>
/// <param name="DisplayName">The name shown in the server.</param>
/// <param name="AvatarUrl">The Discord picture's address, or <see langword="null"/>.</param>
/// <param name="DiscordRoles">The person's Discord roles people are given, highest first.</param>
/// <param name="Roles">The roles they have: Administrator alone for the Administrator, otherwise every role their Discord roles give, in list order, otherwise Member.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: One row of the members page (boards 5 and 13).
/// </remarks>
public sealed record CommunityMemberResponse(
    string DiscordUserId,
    string DisplayName,
    string? AvatarUrl,
    IReadOnlyList<DiscordServerRole> DiscordRoles,
    IReadOnlyList<string> Roles);
