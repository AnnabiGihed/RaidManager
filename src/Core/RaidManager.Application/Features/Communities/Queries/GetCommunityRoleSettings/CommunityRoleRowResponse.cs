using RaidManager.Domain.Features.Communities.Enums;

namespace RaidManager.Application.Features.Communities.Queries.GetCommunityRoleSettings;

/// <summary>Describes one RaidManager role in the roles card: its Discord roles and how many members it has.</summary>
/// <param name="Role">The RaidManager role.</param>
/// <param name="DiscordRoles">The Discord roles mapped to it; empty for Administrator and Member.</param>
/// <param name="Members">How many people get it from their Discord roles; for Member, how many get no mapped role.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: One row of the roles card of board 4.
/// </remarks>
public sealed record CommunityRoleRowResponse(CommunityMemberRole Role, IReadOnlyList<MappedDiscordRoleResponse> DiscordRoles, int Members);
