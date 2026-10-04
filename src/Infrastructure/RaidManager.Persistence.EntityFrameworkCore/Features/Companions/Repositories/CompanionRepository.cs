using Microsoft.EntityFrameworkCore;
using Pivot.Framework.Infrastructure.Persistence.EntityFrameworkCore.Repositories;
using RaidManager.Domain.Features.Companions.Aggregates;
using RaidManager.Domain.Features.Companions.Repositories;
using RaidManager.Domain.Features.Companions.ValueObjects;
using RaidManager.Domain.Features.Shared.Identifiers;

namespace RaidManager.Persistence.EntityFrameworkCore.Features.Companions.Repositories;

/// <summary>Loads and tracks <see cref="Companion"/> aggregates in the RaidManager database.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: Implements the domain repository contract on Pivot's command repository, adding the token lookup and the
/// player's list, read without tracking (ADR-0010).
/// </remarks>
internal sealed class CompanionRepository : BaseAsyncCommandRepository<Companion, CompanionId>, ICompanionRepository
{
    #region Fields
    /// <summary>Stores the RaidManager database context.</summary>
    private readonly RaidManagerDbContext _dbContext;
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="CompanionRepository"/> class.</summary>
    /// <param name="dbContext">The RaidManager database context.</param>
    public CompanionRepository(RaidManagerDbContext dbContext)
        : base(dbContext)
    {
        _dbContext = dbContext;
    }
    #endregion Constructors

    #region Public Methods
    /// <inheritdoc />
    public Task<Companion?> FindByTokenHashAsync(CredentialHash tokenHash, CancellationToken cancellationToken) =>
        _dbContext.Companions.FirstOrDefaultAsync(companion => companion.TokenHash == tokenHash, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Companion>> ListByUserAsync(UserId userId, CancellationToken cancellationToken) =>
        await _dbContext.Companions
            .AsNoTracking()
            .Where(companion => companion.UserId == userId)
            .OrderBy(companion => companion.PairedAtUtc)
            .ToListAsync(cancellationToken);
    #endregion Public Methods
}
