using RaidManager.Application.Features.Communities.Queries;

namespace RaidManager.ApiService.Features.Communities;

/// <summary>Describes a linked community to the website.</summary>
/// <param name="CommunityId">The community identifier.</param>
/// <param name="DiscordGuildId">The linked Discord server snowflake.</param>
/// <param name="Name">The community name, taken from the Discord server.</param>
/// <param name="Realm">The Warmane realm's name, such as <c>Icecrown</c>.</param>
/// <param name="AdministratorId">The user who added the bot.</param>
/// <param name="AdministratorName">The Administrator's display name.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: The API contract for a community; enums travel as names, like the other website contracts.
/// </remarks>
public sealed record CommunitySummary(
    Guid CommunityId,
    string DiscordGuildId,
    string Name,
    string Realm,
    Guid AdministratorId,
    string AdministratorName)
{
    #region Public Methods
    /// <summary>Maps a query response to the contract.</summary>
    /// <param name="community">The query response.</param>
    /// <returns>The contract.</returns>
    public static CommunitySummary From(CommunitySummaryResponse community)
    {
        ArgumentNullException.ThrowIfNull(community);
        return new CommunitySummary(
            community.CommunityId,
            community.DiscordGuildId,
            community.Name,
            community.Realm.ToString(),
            community.AdministratorId,
            community.AdministratorName);
    }
    #endregion Public Methods
}
