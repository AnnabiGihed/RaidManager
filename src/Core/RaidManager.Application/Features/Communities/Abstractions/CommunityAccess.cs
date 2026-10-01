using Pivot.Framework.Domain.Shared;
using RaidManager.Domain.Features.Communities.Aggregates;
using RaidManager.Domain.Features.Communities.Errors;
using RaidManager.Domain.Features.Identity.Aggregates;

namespace RaidManager.Application.Features.Communities.Abstractions;

/// <summary>Checks with Discord that a user is in a community's server, and whether they administer it.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Keeps one membership check for the community settings: the user must be in the Discord server now (ADR-0022), and only its Administrator may change it. Discord not answering refuses.
/// </remarks>
internal static class CommunityAccess
{
    #region Public Methods
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
        if (user is null)
        {
            return Result.Failure(CommunityErrors.NotAMember, ResultExceptionType.AccessDenied);
        }

        var membership = await discordMembers.FindAsync(community.DiscordGuildId, user.DiscordUserId.Value, cancellationToken);
        if (membership.IsFailure)
        {
            return Result.Failure(membership.Error);
        }

        return membership.Value.IsMember
            ? Result.Success()
            : Result.Failure(CommunityErrors.NotAMember, ResultExceptionType.AccessDenied);
    }

    /// <summary>Checks that the user administers the community and is still in its Discord server.</summary>
    /// <param name="community">The community.</param>
    /// <param name="user">The user, or <see langword="null"/> when RaidManager doesn't know them.</param>
    /// <param name="discordMembers">The Discord membership lookup.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>Success, <see cref="CommunityErrors.NotAdministrator"/>, <see cref="CommunityErrors.NotAMember"/>, or Discord's failure.</returns>
    public static async Task<Result> EnsureAdministratorAsync(
        Community community,
        User? user,
        IDiscordServerMembers discordMembers,
        CancellationToken cancellationToken) =>
        user is not null && user.Id != community.AdministratorId
            ? Result.Failure(CommunityErrors.NotAdministrator, ResultExceptionType.AccessDenied)
            : await EnsureMemberAsync(community, user, discordMembers, cancellationToken);
    #endregion Public Methods
}
