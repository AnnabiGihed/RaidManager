using Pivot.Framework.Application.Abstractions.Messaging.Queries;
using Pivot.Framework.Domain.Shared;
using RaidManager.Application.Features.Communities.Abstractions;
using RaidManager.Domain.Features.Communities.Errors;
using RaidManager.Domain.Features.Shared.Identifiers;

namespace RaidManager.Application.Features.Communities.Queries.GetCommunity;

/// <summary>Handles <see cref="GetCommunityQuery"/> with the read-only community reader.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Keeps the query off the command repository (ADR-0010).
/// </remarks>
internal sealed class GetCommunityQueryHandler : IQueryHandler<GetCommunityQuery, CommunitySummaryResponse>
{
    #region Fields
    /// <summary>Stores the community reader.</summary>
    private readonly ICommunityReader _reader;
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="GetCommunityQueryHandler"/> class.</summary>
    /// <param name="reader">The community reader.</param>
    public GetCommunityQueryHandler(ICommunityReader reader)
    {
        _reader = reader;
    }
    #endregion Constructors

    #region Public Methods
    /// <summary>Answers the query.</summary>
    /// <param name="request">The query.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The community, or <see cref="CommunityErrors.NotFound"/>.</returns>
    public async Task<Result<CommunitySummaryResponse>> Handle(GetCommunityQuery request, CancellationToken cancellationToken)
    {
        var community = await _reader.FindAsync(new CommunityId(request.CommunityId), cancellationToken);
        return community is null
            ? Result.Failure<CommunitySummaryResponse>(CommunityErrors.NotFound, ResultExceptionType.NotFound)
            : Result.Success(community);
    }
    #endregion Public Methods
}
