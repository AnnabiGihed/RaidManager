using RaidManager.Application.Features.Communities.Queries;
using RaidManager.Domain.Features.Shared.Identifiers;

namespace RaidManager.Application.Features.Communities.Abstractions;

/// <summary>Reads communities for queries, without loading aggregates.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Keeps queries off the command repositories: persistence implements this with read-only queries (ADR-0010).
/// </remarks>
public interface ICommunityReader
{
    #region Methods
    /// <summary>Finds a community by its identifier.</summary>
    /// <param name="communityId">The community.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The community, or <see langword="null"/> when none exists.</returns>
    Task<CommunitySummaryResponse?> FindAsync(CommunityId communityId, CancellationToken cancellationToken);

    /// <summary>Finds the community a Discord server links to.</summary>
    /// <param name="discordGuildId">The Discord server snowflake.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The community, or <see langword="null"/> when the server isn't linked.</returns>
    Task<CommunitySummaryResponse?> FindByDiscordGuildAsync(string discordGuildId, CancellationToken cancellationToken);

    /// <summary>Lists the communities a user administers, by name.</summary>
    /// <param name="userId">The user.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The communities.</returns>
    Task<IReadOnlyList<CommunitySummaryResponse>> ListAdministeredByAsync(UserId userId, CancellationToken cancellationToken);
    #endregion Methods
}
