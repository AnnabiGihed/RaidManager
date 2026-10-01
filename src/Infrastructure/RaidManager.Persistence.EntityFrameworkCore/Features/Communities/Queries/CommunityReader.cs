using Microsoft.EntityFrameworkCore;
using RaidManager.Application.Features.Communities.Abstractions;
using RaidManager.Application.Features.Communities.Queries;
using RaidManager.Domain.Features.Communities.Aggregates;
using RaidManager.Domain.Features.Shared.Identifiers;

namespace RaidManager.Persistence.EntityFrameworkCore.Features.Communities.Queries;

/// <summary>Reads communities with no-tracking queries against the write tables.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Implements <see cref="ICommunityReader"/> as ADR-0010 describes, joining the Administrator's display name
/// from the users table.
/// </remarks>
internal sealed class CommunityReader : ICommunityReader
{
    #region Fields
    /// <summary>Stores the RaidManager database context.</summary>
    private readonly RaidManagerDbContext _dbContext;
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="CommunityReader"/> class.</summary>
    /// <param name="dbContext">The RaidManager database context.</param>
    public CommunityReader(RaidManagerDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    #endregion Constructors

    #region Public Methods
    /// <inheritdoc />
    public async Task<CommunitySummaryResponse?> FindAsync(CommunityId communityId, CancellationToken cancellationToken) =>
        (await SummariesAsync(community => community.Id == communityId, cancellationToken)).SingleOrDefault();

    /// <inheritdoc />
    public async Task<CommunitySummaryResponse?> FindByDiscordGuildAsync(string discordGuildId, CancellationToken cancellationToken) =>
        (await SummariesAsync(community => community.DiscordGuildId == discordGuildId, cancellationToken)).SingleOrDefault();

    /// <inheritdoc />
    public async Task<IReadOnlyList<CommunitySummaryResponse>> ListAdministeredByAsync(UserId userId, CancellationToken cancellationToken) =>
        await SummariesAsync(community => community.AdministratorId == userId, cancellationToken);
    #endregion Public Methods

    #region Private Helpers
    /// <summary>Reads the summaries of the communities that match a filter, by name.</summary>
    /// <param name="filter">The filter on communities.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The matching communities.</returns>
    private async Task<List<CommunitySummaryResponse>> SummariesAsync(
        System.Linq.Expressions.Expression<Func<Community, bool>> filter,
        CancellationToken cancellationToken)
    {
        var rows = await _dbContext.Communities
            .AsNoTracking()
            .Where(community => !community.IsDeleted)
            .Where(filter)
            .Join(
                _dbContext.Users.AsNoTracking(),
                community => community.AdministratorId,
                user => user.Id,
                (community, user) => new
                {
                    community.Id,
                    community.DiscordGuildId,
                    community.Name,
                    community.Realm,
                    community.AdministratorId,
                    AdministratorName = user.DisplayName,
                })
            .OrderBy(row => row.Name)
            .ToListAsync(cancellationToken);

        return rows.ConvertAll(row => new CommunitySummaryResponse(
            row.Id.Value, row.DiscordGuildId, row.Name, row.Realm, row.AdministratorId.Value, row.AdministratorName));
    }
    #endregion Private Helpers
}
