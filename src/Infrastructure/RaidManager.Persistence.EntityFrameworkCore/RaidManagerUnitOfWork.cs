using Pivot.Framework.Application.Abstractions;
using Pivot.Framework.Infrastructure.Abstraction.Outbox.DomainEventPublisher;
using Pivot.Framework.Infrastructure.Abstraction.Outbox.IntegrationEventMapping;
using Pivot.Framework.Infrastructure.Persistence.EntityFrameworkCore.UnitOfWork;

namespace RaidManager.Persistence.EntityFrameworkCore;

/// <summary>Commits RaidManager aggregate changes together with their domain events.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-09-30<br/>
/// Purpose: Uses Pivot's unit of work, which stamps audit columns and writes each domain event to the outbox in the same transaction.
/// Nothing delivers outbox messages yet; delivery gets its own decision when the first event handler is needed.
/// </remarks>
public sealed class RaidManagerUnitOfWork : UnitOfWork<RaidManagerDbContext>
{
    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="RaidManagerUnitOfWork"/> class.</summary>
    /// <param name="dbContext">The RaidManager database context.</param>
    /// <param name="currentUserProvider">The source of the actor stamped into audit columns.</param>
    /// <param name="domainEventPublisher">The publisher that writes domain events to the outbox.</param>
    /// <param name="integrationEventMappingCoordinator">The optional mapper from domain events to integration events.</param>
    public RaidManagerUnitOfWork(
        RaidManagerDbContext dbContext,
        ICurrentUserProvider currentUserProvider,
        IDomainEventPublisher<RaidManagerDbContext> domainEventPublisher,
        IIntegrationEventMappingCoordinator<RaidManagerDbContext>? integrationEventMappingCoordinator = null)
        : base(dbContext, currentUserProvider, domainEventPublisher, integrationEventMappingCoordinator)
    {
    }
    #endregion Constructors
}
