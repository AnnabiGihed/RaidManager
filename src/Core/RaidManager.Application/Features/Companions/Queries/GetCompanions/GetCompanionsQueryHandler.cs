using Pivot.Framework.Application.Abstractions.Messaging.Queries;
using Pivot.Framework.Domain.Shared;
using RaidManager.Domain.Features.Companions.Repositories;
using RaidManager.Domain.Features.Shared.Identifiers;

namespace RaidManager.Application.Features.Companions.Queries.GetCompanions;

/// <summary>Handles <see cref="GetCompanionsQuery"/>: lists the player's companions with their current status.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: Takes the status from the companion itself, so the list and the token check never disagree.
/// </remarks>
internal sealed class GetCompanionsQueryHandler : IQueryHandler<GetCompanionsQuery, IReadOnlyList<CompanionResponse>>
{
    #region Fields
    /// <summary>Stores the companion repository.</summary>
    private readonly ICompanionRepository _companions;

    /// <summary>Stores the clock.</summary>
    private readonly TimeProvider _timeProvider;
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="GetCompanionsQueryHandler"/> class.</summary>
    /// <param name="companions">The companion repository.</param>
    /// <param name="timeProvider">The clock.</param>
    public GetCompanionsQueryHandler(ICompanionRepository companions, TimeProvider timeProvider)
    {
        _companions = companions;
        _timeProvider = timeProvider;
    }
    #endregion Constructors

    #region Public Methods
    /// <summary>Lists the companions.</summary>
    /// <param name="request">The query.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The companions, oldest pairing first; an empty list when there are none.</returns>
    public async Task<Result<IReadOnlyList<CompanionResponse>>> Handle(GetCompanionsQuery request, CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow();
        var companions = await _companions.ListByUserAsync(new UserId(request.UserId), cancellationToken);
        IReadOnlyList<CompanionResponse> rows = [.. companions.Select(companion => new CompanionResponse(
            companion.Id.Value, companion.Label, companion.PairedAtUtc, companion.LastUsedAtUtc, companion.StatusAt(now), companion.RevokedAtUtc, companion.LastUploadAtUtc))];
        return Result.Success(rows);
    }
    #endregion Public Methods
}
