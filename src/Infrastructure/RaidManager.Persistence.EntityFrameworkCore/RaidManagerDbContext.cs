using Microsoft.EntityFrameworkCore;
using Pivot.Framework.Infrastructure.Abstraction.Outbox.Models;
using Pivot.Framework.Infrastructure.Persistence.EntityFrameworkCore.PersistenceContext;
using RaidManager.Domain.Features.Characters.Aggregates;
using RaidManager.Domain.Features.Communities.Aggregates;
using RaidManager.Domain.Features.Identity.Aggregates;
using RaidManager.Domain.Features.Raids.Aggregates;
using RaidManager.Persistence.EntityFrameworkCore.Features.Shared.Conversions;

namespace RaidManager.Persistence.EntityFrameworkCore;

/// <summary>Represents the RaidManager write-side database session on SQL Server.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-09-30<br/>
/// Purpose: Maps the aggregates and the outbox table that Pivot's unit of work writes domain events to, in one transaction.
/// </remarks>
public sealed class RaidManagerDbContext : PivotDbContextBase
{
    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="RaidManagerDbContext"/> class.</summary>
    /// <param name="options">The context options.</param>
    public RaidManagerDbContext(DbContextOptions<RaidManagerDbContext> options)
        : base(options)
    {
    }
    #endregion Constructors

    #region Properties
    /// <summary>Gets the Warmane characters.</summary>
    public DbSet<Character> Characters => Set<Character>();

    /// <summary>Gets the local users, one per Discord account.</summary>
    public DbSet<User> Users => Set<User>();

    /// <summary>Gets the Discord servers linked as communities.</summary>
    public DbSet<Community> Communities => Set<Community>();

    /// <summary>Gets the scheduled raids with their signups and rosters.</summary>
    public DbSet<Raid> Raids => Set<Raid>();

    /// <summary>Gets the domain events recorded for later delivery.</summary>
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    #endregion Properties

    #region Overrides
    /// <inheritdoc />
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
        configurationBuilder.Properties<DateTime?>().HaveConversion<UtcDateTimeConverter>();
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // The base maps Pivot's AuditInfo columns for every auditable entity already in the model.
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<OutboxMessage>().ToTable("OutboxMessages");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(RaidManagerDbContext).Assembly);
    }
    #endregion Overrides
}
