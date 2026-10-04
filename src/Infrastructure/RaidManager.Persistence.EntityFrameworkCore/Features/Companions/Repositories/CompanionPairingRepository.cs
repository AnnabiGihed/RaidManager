using Microsoft.EntityFrameworkCore;
using Pivot.Framework.Infrastructure.Persistence.EntityFrameworkCore.Repositories;
using RaidManager.Domain.Features.Companions.Aggregates;
using RaidManager.Domain.Features.Companions.Enums;
using RaidManager.Domain.Features.Companions.Repositories;
using RaidManager.Domain.Features.Companions.ValueObjects;
using RaidManager.Domain.Features.Shared.Identifiers;

namespace RaidManager.Persistence.EntityFrameworkCore.Features.Companions.Repositories;

/// <summary>Loads and tracks <see cref="CompanionPairing"/> aggregates in the RaidManager database.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: Implements the domain repository contract on Pivot's command repository, adding the lookups by device code and code.
/// </remarks>
internal sealed class CompanionPairingRepository : BaseAsyncCommandRepository<CompanionPairing, CompanionPairingId>, ICompanionPairingRepository
{
    #region Fields
    /// <summary>Stores the RaidManager database context.</summary>
    private readonly RaidManagerDbContext _dbContext;
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="CompanionPairingRepository"/> class.</summary>
    /// <param name="dbContext">The RaidManager database context.</param>
    public CompanionPairingRepository(RaidManagerDbContext dbContext)
        : base(dbContext)
    {
        _dbContext = dbContext;
    }
    #endregion Constructors

    #region Public Methods
    /// <inheritdoc />
    public Task<CompanionPairing?> FindByDeviceCodeHashAsync(CredentialHash deviceCodeHash, CancellationToken cancellationToken) =>
        _dbContext.CompanionPairings.FirstOrDefaultAsync(pairing => pairing.DeviceCodeHash == deviceCodeHash, cancellationToken);

    /// <inheritdoc />
    public Task<CompanionPairing?> FindLatestUncollectedByCodeAsync(PairingCode code, CancellationToken cancellationToken) =>
        _dbContext.CompanionPairings
            .Where(pairing => pairing.Code == code && pairing.State != CompanionPairingState.Completed)
            .OrderByDescending(pairing => pairing.RequestedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

    /// <inheritdoc />
    public Task<bool> IsCodeInUseAsync(PairingCode code, DateTimeOffset nowUtc, CancellationToken cancellationToken) =>
        _dbContext.CompanionPairings.AnyAsync(
            pairing => pairing.Code == code && pairing.State != CompanionPairingState.Completed && pairing.ExpiresAtUtc > nowUtc,
            cancellationToken);
    #endregion Public Methods
}
