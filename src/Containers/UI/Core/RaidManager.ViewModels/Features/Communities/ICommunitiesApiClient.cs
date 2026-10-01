namespace RaidManager.ViewModels.Features.Communities;

/// <summary>Calls the API's community endpoints for the signed-in user.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Keeps the community view models free of HTTP; the website implements it with the service-key client.
/// </remarks>
public interface ICommunitiesApiClient
{
    #region Methods
    /// <summary>Lists the communities a user belongs to.</summary>
    /// <param name="userId">The signed-in user.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The communities, by name; empty when the user has none.</returns>
    Task<IReadOnlyList<CommunitySummary>> GetUserCommunitiesAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>Gets a community.</summary>
    /// <param name="communityId">The community.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The community, or <see langword="null"/> when it doesn't exist.</returns>
    Task<CommunitySummary?> GetAsync(Guid communityId, CancellationToken cancellationToken);

    /// <summary>Gets the community a Discord server links to.</summary>
    /// <param name="discordGuildId">The Discord server snowflake.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The community, or <see langword="null"/> when the server isn't linked.</returns>
    Task<CommunitySummary?> FindByDiscordServerAsync(string discordGuildId, CancellationToken cancellationToken);

    /// <summary>Links a Discord server as a community, with the user as its Administrator.</summary>
    /// <param name="link">The server the bot was added to and the user who added it.</param>
    /// <param name="realm">The chosen Warmane realm's name.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The new community's identifier, or <see langword="null"/> when the server was linked in the meantime.</returns>
    Task<Guid?> LinkAsync(PendingCommunityLink link, string realm, CancellationToken cancellationToken);
    #endregion Methods
}
