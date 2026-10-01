using Pivot.Framework.Infrastructure.Persistence.EntityFrameworkCore.Repositories;
using RaidManager.Domain.Features.Communities.Aggregates;
using RaidManager.Domain.Features.Communities.Repositories;
using RaidManager.Domain.Features.Shared.Identifiers;

namespace RaidManager.Persistence.EntityFrameworkCore.Features.Communities.Repositories;

/// <summary>Loads and tracks <see cref="Community"/> aggregates in the RaidManager database.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Implements the domain repository contract on Pivot's command repository; the owned role mappings load with the community.
/// </remarks>
internal sealed class CommunityRepository : BaseAsyncCommandRepository<Community, CommunityId>, ICommunityRepository
{
    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="CommunityRepository"/> class.</summary>
    /// <param name="dbContext">The RaidManager database context.</param>
    public CommunityRepository(RaidManagerDbContext dbContext)
        : base(dbContext)
    {
    }
    #endregion Constructors
}
