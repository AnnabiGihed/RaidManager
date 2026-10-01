using Pivot.Framework.Application.Abstractions.Messaging.Queries;
using Pivot.Framework.Domain.Shared;
using RaidManager.Application.Features.Communities.Abstractions;
using RaidManager.Domain.Features.Shared.Identifiers;

namespace RaidManager.Application.Features.Communities.Queries.GetUserCommunities;

/// <summary>Handles <see cref="GetUserCommunitiesQuery"/> with the read-only community reader.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Keeps the query off the command repository (ADR-0010).
/// </remarks>
internal sealed class GetUserCommunitiesQueryHandler : IQueryHandler<GetUserCommunitiesQuery, IReadOnlyList<CommunitySummaryResponse>>
{
    #region Fields
    /// <summary>Stores the community reader.</summary>
    private readonly ICommunityReader _reader;
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="GetUserCommunitiesQueryHandler"/> class.</summary>
    /// <param name="reader">The community reader.</param>
    public GetUserCommunitiesQueryHandler(ICommunityReader reader)
    {
        _reader = reader;
    }
    #endregion Constructors

    #region Public Methods
    /// <summary>Answers the query.</summary>
    /// <param name="request">The query.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The communities the user administers, by name; empty when there are none.</returns>
    public async Task<Result<IReadOnlyList<CommunitySummaryResponse>>> Handle(GetUserCommunitiesQuery request, CancellationToken cancellationToken) =>
        Result.Success(await _reader.ListAdministeredByAsync(new UserId(request.UserId), cancellationToken));
    #endregion Public Methods
}
