using Pivot.Framework.Application.Abstractions.Messaging.Queries;
using Pivot.Framework.Domain.Shared;
using RaidManager.Application.Features.Communities.Abstractions;
using RaidManager.Domain.Features.Communities.Errors;

namespace RaidManager.Application.Features.Communities.Queries.GetCommunityByDiscordServer;

/// <summary>Handles <see cref="GetCommunityByDiscordServerQuery"/> with the read-only community reader.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Keeps the query off the command repository (ADR-0010).
/// </remarks>
internal sealed class GetCommunityByDiscordServerQueryHandler : IQueryHandler<GetCommunityByDiscordServerQuery, CommunitySummaryResponse>
{
    #region Fields
    /// <summary>Stores the community reader.</summary>
    private readonly ICommunityReader _reader;
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="GetCommunityByDiscordServerQueryHandler"/> class.</summary>
    /// <param name="reader">The community reader.</param>
    public GetCommunityByDiscordServerQueryHandler(ICommunityReader reader)
    {
        _reader = reader;
    }
    #endregion Constructors

    #region Public Methods
    /// <summary>Answers the query.</summary>
    /// <param name="request">The query.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The community, or <see cref="CommunityErrors.NotFound"/> when the server is not linked.</returns>
    public async Task<Result<CommunitySummaryResponse>> Handle(GetCommunityByDiscordServerQuery request, CancellationToken cancellationToken)
    {
        var community = await _reader.FindByDiscordGuildAsync(request.DiscordGuildId, cancellationToken);
        return community is null
            ? Result.Failure<CommunitySummaryResponse>(CommunityErrors.NotFound, ResultExceptionType.NotFound)
            : Result.Success(community);
    }
    #endregion Public Methods
}
