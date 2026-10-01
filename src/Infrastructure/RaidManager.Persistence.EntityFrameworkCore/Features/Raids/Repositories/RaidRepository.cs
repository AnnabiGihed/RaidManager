using Pivot.Framework.Infrastructure.Persistence.EntityFrameworkCore.Repositories;
using RaidManager.Domain.Features.Raids.Aggregates;
using RaidManager.Domain.Features.Raids.Repositories;
using RaidManager.Domain.Features.Shared.Identifiers;

namespace RaidManager.Persistence.EntityFrameworkCore.Features.Raids.Repositories;

/// <summary>Loads and tracks <see cref="Raid"/> aggregates in the RaidManager database.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Implements the domain repository contract on Pivot's command repository; the owned targets, signups and roster selections load with the raid.
/// </remarks>
internal sealed class RaidRepository : BaseAsyncCommandRepository<Raid, RaidId>, IRaidRepository
{
    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="RaidRepository"/> class.</summary>
    /// <param name="dbContext">The RaidManager database context.</param>
    public RaidRepository(RaidManagerDbContext dbContext)
        : base(dbContext)
    {
    }
    #endregion Constructors
}
