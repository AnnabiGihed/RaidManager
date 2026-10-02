using Pivot.Framework.Application.Abstractions.Messaging.Commands;

namespace RaidManager.Application.Features.Communities.Commands.DeleteCommunityRole;

/// <summary>Deletes one of a community's roles; the Discord roles mapped to it stop giving it.</summary>
/// <param name="CommunityId">The community.</param>
/// <param name="UserId">The signed-in user: the Administrator or a member whose role manages roles.</param>
/// <param name="RoleId">The role.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-02<br/>
/// Purpose: Board 12 of the community settings mockup (story #308).
/// </remarks>
public sealed record DeleteCommunityRoleCommand(Guid CommunityId, Guid UserId, Guid RoleId) : ICommand;
