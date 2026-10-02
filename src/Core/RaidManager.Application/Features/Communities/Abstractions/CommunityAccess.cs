using Pivot.Framework.Domain.Shared;
using RaidManager.Domain.Features.Communities.Aggregates;
using RaidManager.Domain.Features.Communities.Enums;
using RaidManager.Domain.Features.Communities.Errors;
using RaidManager.Domain.Features.Identity.Aggregates;

namespace RaidManager.Application.Features.Communities.Abstractions;

/// <summary>Checks with Discord that a user is in a community's server, and what they may do there.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Keeps one membership check for the community settings: the user must be in the Discord server now
/// (ADR-0022), and only the Administrator or a member whose role grants Manage community roles may change the roles
/// (ADR-0024). Discord not answering refuses.
/// </remarks>
internal static class CommunityAccess
{
    #region Public Methods
    /// <summary>Checks that the user is in the community's Discord server, and gives what they may do there.</summary>
    /// <param name="community">The community.</param>
    /// <param name="user">The user, or <see langword="null"/> when RaidManager doesn't know them.</param>
    /// <param name="discordMembers">The Discord membership lookup.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The user's permissions, <see cref="CommunityErrors.NotAMember"/>, or Discord's failure.</returns>
    public static async Task<Result<CommunityPermissions>> PermissionsAsync(
        Community community,
        User? user,
        IDiscordServerMembers discordMembers,
        CancellationToken cancellationToken)
    {
        // A user RaidManager doesn't know has no Discord account to check, so they can't be in the server.
        if (user is null)
        {
            return Result.Failure<CommunityPermissions>(CommunityErrors.NotAMember, ResultExceptionType.AccessDenied);
        }

        var membership = await discordMembers.FindAsync(community.DiscordGuildId, user.DiscordUserId.Value, cancellationToken);
        if (membership.IsFailure)
        {
            return Result.Failure<CommunityPermissions>(membership.Error);
        }

        return membership.Value.IsMember
            ? Result.Success(community.PermissionsFor(user.Id, membership.Value.RoleIds))
            : Result.Failure<CommunityPermissions>(CommunityErrors.NotAMember, ResultExceptionType.AccessDenied);
    }

    /// <summary>Checks that the user is in the community's Discord server.</summary>
    /// <param name="community">The community.</param>
    /// <param name="user">The user, or <see langword="null"/> when RaidManager doesn't know them.</param>
    /// <param name="discordMembers">The Discord membership lookup.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>Success, <see cref="CommunityErrors.NotAMember"/>, or Discord's failure.</returns>
    public static async Task<Result> EnsureMemberAsync(
        Community community,
        User? user,
        IDiscordServerMembers discordMembers,
        CancellationToken cancellationToken)
    {
        var permissions = await PermissionsAsync(community, user, discordMembers, cancellationToken);
        return permissions.IsSuccess ? Result.Success() : Result.Failure(permissions.Error, permissions.ResultExceptionType);
    }

    /// <summary>Checks that the user may change the community's roles: the Administrator, or a member whose role manages roles.</summary>
    /// <param name="community">The community.</param>
    /// <param name="user">The user, or <see langword="null"/> when RaidManager doesn't know them.</param>
    /// <param name="discordMembers">The Discord membership lookup.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// Whether the user is the Administrator, or <see cref="CommunityErrors.NotRoleManager"/>,
    /// <see cref="CommunityErrors.NotAMember"/>, or Discord's failure.
    /// </returns>
    public static async Task<Result<bool>> EnsureRoleManagerAsync(
        Community community,
        User? user,
        IDiscordServerMembers discordMembers,
        CancellationToken cancellationToken)
    {
        var permissions = await PermissionsAsync(community, user, discordMembers, cancellationToken);
        if (permissions.IsFailure)
        {
            return Result.Failure<bool>(permissions.Error, permissions.ResultExceptionType);
        }

        if (user!.Id == community.AdministratorId)
        {
            return Result.Success(true);
        }

        return permissions.Value.HasFlag(CommunityPermissions.ManageCommunityRoles)
            ? Result.Success(false)
            : Result.Failure<bool>(CommunityErrors.NotRoleManager, ResultExceptionType.AccessDenied);
    }
    #endregion Public Methods
}
