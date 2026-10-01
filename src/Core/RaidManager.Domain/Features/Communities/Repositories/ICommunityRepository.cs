using Pivot.Framework.Domain.Repositories;
using RaidManager.Domain.Features.Communities.Aggregates;
using RaidManager.Domain.Features.Shared.Identifiers;

namespace RaidManager.Domain.Features.Communities.Repositories;

/// <summary>Defines persistence operations for the raiding community aggregate root.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-09-29<br/>
/// Purpose: Keeps aggregate persistence contracts in the Domain layer on Pivot's command repository, while implementations remain in EF Core infrastructure.
/// </remarks>
public interface ICommunityRepository : IAsyncCommandRepository<Community, CommunityId>
{
    #region Methods
    /// <summary>Tells whether a Discord server is already linked to a community.</summary>
    /// <param name="discordGuildId">The Discord server snowflake.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns><see langword="true"/> when a community links that server.</returns>
    Task<bool> IsDiscordGuildLinkedAsync(string discordGuildId, CancellationToken cancellationToken);
    #endregion Methods
}
