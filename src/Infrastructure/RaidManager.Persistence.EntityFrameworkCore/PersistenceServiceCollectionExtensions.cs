using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Pivot.Framework.Infrastructure.Abstraction.UnitOfWork;
using Pivot.Framework.Infrastructure.Messaging.EntityFrameworkCore.Extensions;
using RaidManager.Application.Features.Characters.Abstractions;
using RaidManager.Domain.Features.Characters.Repositories;
using RaidManager.Domain.Features.Identity.Repositories;
using RaidManager.Domain.Features.Raids.Repositories;
using RaidManager.Persistence.EntityFrameworkCore.Features.Characters.Queries;
using RaidManager.Persistence.EntityFrameworkCore.Features.Characters.Repositories;
using RaidManager.Persistence.EntityFrameworkCore.Features.Identity.Repositories;
using RaidManager.Persistence.EntityFrameworkCore.Features.Raids.Repositories;
using DomainUnitOfWork = Pivot.Framework.Domain.Repositories.IUnitOfWork;

namespace RaidManager.Persistence.EntityFrameworkCore;

/// <summary>Registers RaidManager's SQL Server persistence in a dependency-injection container.</summary>
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
    /// <param name="connectionString">The SQL Server connection string.</param>
    /// <returns>The same service collection.</returns>
    /// <remarks>No outbox drain mode or transport is registered: domain events are recorded but not yet delivered.</remarks>
    public static IServiceCollection AddRaidManagerPersistence(this IServiceCollection services, string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        services.AddDbContext<RaidManagerDbContext>(options => options.UseSqlServer(connectionString));
        services.AddEfCoreWritePersistence<RaidManagerDbContext, RaidManagerUnitOfWork>(includeEventStore: false);

        // Application handlers depend on the domain IUnitOfWork, so it resolves to the same scoped unit of work.
        services.AddScoped<DomainUnitOfWork>(provider => (RaidManagerUnitOfWork)provider.GetRequiredService<IUnitOfWork<RaidManagerDbContext>>());
        services.AddScoped<ICharacterRepository, CharacterRepository>();
        services.AddScoped<ICharacterClaimReader, CharacterClaimReader>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRaidRepository, RaidRepository>();
        return services;
    }
    #endregion Public Methods
}
