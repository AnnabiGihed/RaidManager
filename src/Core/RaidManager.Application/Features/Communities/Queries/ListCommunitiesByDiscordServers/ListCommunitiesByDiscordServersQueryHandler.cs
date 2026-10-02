using Pivot.Framework.Application.Abstractions.Messaging.Queries;
using Pivot.Framework.Domain.Shared;
using RaidManager.Application.Features.Communities.Abstractions;

namespace RaidManager.Application.Features.Communities.Queries.ListCommunitiesByDiscordServers;

/// <summary>Handles <see cref="ListCommunitiesByDiscordServersQuery"/> with the read-only community reader.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-02<br/>
/// Purpose: Keeps the query off the command repository (ADR-0010).
/// </remarks>
internal sealed class ListCommunitiesByDiscordServersQueryHandler : IQueryHandler<ListCommunitiesByDiscordServersQuery, IReadOnlyList<CommunitySummaryResponse>>
{
    #region Fields
    /// <summary>Stores the community reader.</summary>
    private readonly ICommunityReader _reader;
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="ListCommunitiesByDiscordServersQueryHandler"/> class.</summary>
    /// <param name="reader">The community reader.</param>
    public ListCommunitiesByDiscordServersQueryHandler(ICommunityReader reader)
    {
        _reader = reader;
    }
    #endregion Constructors

    #region Public Methods
    /// <summary>Answers the query.</summary>
    /// <param name="request">The query.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The linked communities, by name; empty when none of the servers is linked.</returns>
    public async Task<Result<IReadOnlyList<CommunitySummaryResponse>>> Handle(ListCommunitiesByDiscordServersQuery request, CancellationToken cancellationToken) =>
        Result.Success(await _reader.ListByDiscordGuildsAsync([.. request.DiscordGuildIds.Distinct(StringComparer.Ordinal)], cancellationToken));
    #endregion Public Methods
}
