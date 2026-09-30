using Microsoft.EntityFrameworkCore;
using RaidManager.Application.Features.Characters.Abstractions;
using RaidManager.Application.Features.Characters.Queries.GetPendingCharacterClaims;
using RaidManager.Domain.Features.Characters.Enums;
using RaidManager.Domain.Features.Shared.Identifiers;

namespace RaidManager.Persistence.EntityFrameworkCore.Features.Characters.Queries;

/// <summary>Reads character claims with no-tracking queries against the write database.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-09-30<br/>
/// Purpose: Implements the read side decided in ADR-0010: always current, no projections, and nothing attached to the change tracker.
/// </remarks>
internal sealed class CharacterClaimReader : ICharacterClaimReader
{
    #region Fields
    /// <summary>Stores the RaidManager database context.</summary>
    private readonly RaidManagerDbContext _dbContext;
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="CharacterClaimReader"/> class.</summary>
    /// <param name="dbContext">The RaidManager database context.</param>
    public CharacterClaimReader(RaidManagerDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    #endregion Constructors

    #region Public Methods
    /// <inheritdoc />
    public async Task<IReadOnlyList<PendingCharacterClaimResponse>> ListAwaitingDecisionAsync(UserId userId, CancellationToken cancellationToken)
    {
        var rows = await _dbContext.Characters
            .AsNoTracking()
            .Where(character => !character.IsDeleted)
            .SelectMany(
                character => character.Claims.Where(claim =>
                    claim.RequestedByUserId == userId
                    && (claim.State == CharacterClaimState.Pending || claim.State == CharacterClaimState.Conflict)),
                (character, claim) => new { character, claim })
            .OrderBy(row => row.claim.RequestedAtUtc)
            .Select(row => new
            {
                row.character.Id,
                row.character.Realm,
                row.character.Name,
                row.character.Class,
                row.character.Race,
                row.character.Level,
                row.claim.State,
                row.claim.RequestedAtUtc,
            })
            .ToListAsync(cancellationToken);

        return rows.ConvertAll(row => new PendingCharacterClaimResponse(
            row.Id.Value, row.Realm, row.Name.Value, row.Class, row.Race, row.Level, row.State, row.RequestedAtUtc));
    }
    #endregion Public Methods
}
