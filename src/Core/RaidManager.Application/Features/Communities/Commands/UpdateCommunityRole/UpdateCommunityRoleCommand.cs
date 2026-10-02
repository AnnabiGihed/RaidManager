using Pivot.Framework.Application.Abstractions.Messaging.Commands;

namespace RaidManager.Application.Features.Communities.Commands.UpdateCommunityRole;

/// <summary>Renames one of a community's roles and changes what it allows.</summary>
/// <param name="CommunityId">The community.</param>
/// <param name="UserId">The signed-in user: the Administrator or a member whose role manages roles.</param>
/// <param name="RoleId">The role.</param>
/// <param name="Name">The new name.</param>
/// <param name="Permissions">What the role allows now, by permission name.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-02<br/>
/// Purpose: Board 11 of the community settings mockup (story #308).
/// </remarks>
public sealed record UpdateCommunityRoleCommand(Guid CommunityId, Guid UserId, Guid RoleId, string Name, IReadOnlyList<string> Permissions) : ICommand;
