using Microsoft.Extensions.DependencyInjection;
using Pivot.Framework.Infrastructure.Abstraction.UnitOfWork;
using Pivot.Framework.Infrastructure.Messaging.EntityFrameworkCore.Extensions;
using Pivot.Framework.Infrastructure.Persistence.PostgreSQL.Extensions;
using RaidManager.Application.Features.Characters.Abstractions;
using RaidManager.Application.Features.Communities.Abstractions;
using RaidManager.Domain.Features.Characters.Repositories;
using RaidManager.Domain.Features.Communities.Repositories;
using RaidManager.Domain.Features.Companions.Repositories;
using RaidManager.Domain.Features.Identity.Repositories;
using RaidManager.Domain.Features.Raids.Repositories;
using RaidManager.Persistence.EntityFrameworkCore.Features.Characters.Queries;
using RaidManager.Persistence.EntityFrameworkCore.Features.Characters.Repositories;
using RaidManager.Persistence.EntityFrameworkCore.Features.Communities.Queries;
using RaidManager.Persistence.EntityFrameworkCore.Features.Communities.Repositories;
using RaidManager.Persistence.EntityFrameworkCore.Features.Companions.Repositories;
using RaidManager.Persistence.EntityFrameworkCore.Features.Identity.Repositories;
using RaidManager.Persistence.EntityFrameworkCore.Features.Raids.Repositories;
using DomainUnitOfWork = Pivot.Framework.Domain.Repositories.IUnitOfWork;

namespace RaidManager.Persistence.EntityFrameworkCore;

/// <summary>Registers RaidManager's PostgreSQL persistence in a dependency-injection container.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-09-30<br/>
/// Purpose: Gives hosts one call that wires the database context, Pivot's write-side persistence, the unit of work and the repositories.
/// </remarks>
public static class PersistenceServiceCollectionExtensions
{
    #region Public Methods
    /// <summary>Adds the RaidManager database context, unit of work, outbox writing, aggregate repositories and query readers.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="connectionString">The PostgreSQL connection string.</param>
    /// <returns>The same service collection.</returns>
    /// <remarks>No outbox drain mode or transport is registered: domain events are recorded but not yet delivered.</remarks>
    public static IServiceCollection AddRaidManagerPersistence(this IServiceCollection services, string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        services.AddPostgreSqlContext<RaidManagerDbContext>(connectionString);
        services.AddEfCoreWritePersistence<RaidManagerDbContext, RaidManagerUnitOfWork>(includeEventStore: false);

        // Application handlers depend on the domain IUnitOfWork, so it resolves to the same scoped unit of work.
        services.AddScoped<DomainUnitOfWork>(provider => (RaidManagerUnitOfWork)provider.GetRequiredService<IUnitOfWork<RaidManagerDbContext>>());
        services.AddScoped<ICharacterRepository, CharacterRepository>();
        services.AddScoped<ICharacterClaimReader, CharacterClaimReader>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ICommunityRepository, CommunityRepository>();
        services.AddScoped<ICommunityReader, CommunityReader>();
        services.AddScoped<IRaidRepository, RaidRepository>();
        services.AddScoped<ICompanionPairingRepository, CompanionPairingRepository>();
        services.AddScoped<ICompanionRepository, CompanionRepository>();
        return services;
    }
    #endregion Public Methods
}
