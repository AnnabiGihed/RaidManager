using RaidManager.Application.Features.Communities.Abstractions;
using RaidManager.Domain.Features.Communities.Enums;

namespace RaidManager.Application.Features.Communities.Queries.GetCommunityMembers;

/// <summary>Describes one person in a community's Discord server.</summary>
/// <param name="DiscordUserId">The Discord user snowflake.</param>
/// <param name="DisplayName">The name shown in the server.</param>
/// <param name="AvatarUrl">The Discord picture's address, or <see langword="null"/>.</param>
/// <param name="DiscordRoles">The person's Discord roles people are given, highest first.</param>
/// <param name="Role">The RaidManager role they get: the Administrator, otherwise the highest mapped role, otherwise Member.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: One row of the members page (board 5).
/// </remarks>
public sealed record CommunityMemberResponse(
    string DiscordUserId,
    string DisplayName,
    string? AvatarUrl,
    IReadOnlyList<DiscordServerRole> DiscordRoles,
    CommunityMemberRole Role);
