using Pivot.Framework.Application.Abstractions.Messaging.Commands;

namespace RaidManager.Application.Features.Communities.Commands.CreateCommunityRole;

/// <summary>Creates a role in a community, with a name and the permissions it allows.</summary>
/// <param name="CommunityId">The community.</param>
/// <param name="UserId">The signed-in user: the Administrator or a member whose role manages roles.</param>
/// <param name="Name">The role name.</param>
/// <param name="Permissions">What the role allows, by permission name.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-02<br/>
/// Purpose: Board 10 of the community settings mockup (story #308).
/// </remarks>
public sealed record CreateCommunityRoleCommand(Guid CommunityId, Guid UserId, string Name, IReadOnlyList<string> Permissions) : ICommand<Guid>;
