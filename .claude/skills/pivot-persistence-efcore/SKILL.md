---
name: pivot-persistence-efcore
description: Pivot.Framework.Infrastructure.Persistence.EntityFrameworkCore — EF Core write side. Use when creating a service DbContext on PivotDbContextBase, configuring the framework tables (outbox, inbox, event history, sagas, projections, audit log), writing a UnitOfWork<TContext> subclass or aggregate repositories on BaseAsyncCommandRepository, using EntitySpecification, TransactionManager, EF read-model repositories/stores, the audit log service, read/write SQL Server contexts, current-user audit stamping, or when modifying Src/Infrastructure/Pivot.Framework.Infrastructure.Persistence.EntityFrameworkCore.
---

# Pivot.Framework.Infrastructure.Persistence.EntityFrameworkCore

Location: `Src/Infrastructure/Pivot.Framework.Infrastructure.Persistence.EntityFrameworkCore`.
References Application + Infrastructure.Abstraction, `Microsoft.EntityFrameworkCore.SqlServer` 10, `EfCore.SchemaCompare`,
and uses Newtonsoft.Json + ASP.NET `IHttpContextAccessor`.

**Where are the `Add…` methods?** Most DI entry points for this package's classes live in the
**Messaging.EntityFrameworkCore** package (`AddEfCoreWritePersistence`, `AddEventStore`, `AddInboxSupport`,
`AddSagaSupport`, `AddIntegrationEventMapping`, …) — see `pivot-messaging-outbox`. This package itself only exposes
`AddEfCoreReadModelStore<TContext>()`, `AddAuditLog<TContext>()`, `AddDomainEventDispatcher()`,
`AddIntegrationEventDispatcher()`, `AddReadWriteDbContexts<TWrite,TRead>(…)`.

## 1. DbContext

```csharp
public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : PivotDbContextBase(options)
{
	public DbSet<Order> Orders => Set<Order>();

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		base.OnModelCreating(modelBuilder);        // REQUIRED: ApplyDomainPrimitives() (AuditInfo owned type)

		// Framework tables this service uses — they are NOT added by the base class:
		modelBuilder.Entity<OutboxMessage>().ToTable("OutboxMessages");            // no config class ships; key = Id by convention
		modelBuilder.ApplyConfiguration(new OutboxMessageConsumerConfiguration());   // inbox (needed by AddInboxSupport)
		// modelBuilder.ApplyConfiguration(new EventHistoryEntryConfiguration());    // if includeEventStore: true
		// modelBuilder.ApplyConfiguration(new ProjectionCheckpointConfiguration()); // if using projection rebuilds
		// modelBuilder.ApplyConfiguration(new ProjectionRegistrationConfiguration());
		// modelBuilder.ApplyConfiguration(new SagaInstanceConfiguration());          // + SagaStepRecordConfiguration
		// modelBuilder.ApplyConfiguration(new AuditEntryConfiguration());            // if AddAuditLog

		modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
	}
}
```

- `PivotDbContextBase : DbContext, IPersistenceContext` (file `PersistenceContext/TemplatesCoreDbContextBase.cs`; the README's `TemplatesCoreDbContextBase<T>` name is obsolete). Its `OnModelCreating` only calls `modelBuilder.ApplyDomainPrimitives()`.
- `ApplyDomainPrimitives()` declares `AuditInfo` owned and, for every entity implementing `IAuditableEntity`, maps `Audit` to columns `Audit_CreatedOnUtc`, `Audit_CreatedBy`, `Audit_ModifiedOnUtc`, `Audit_ModifiedBy` in the owner's table. It only sees entity types already in the model when it runs (types exposed as `DbSet<>` properties are). If an auditable entity enters the model only through a configuration class or navigation applied *after* `base.OnModelCreating`, call `modelBuilder.ApplyDomainPrimitives()` once more at the end of your `OnModelCreating` and check the generated migration for the `Audit_*` columns.
- It does **not** add strongly-typed-ID converters or soft-delete query filters. Configure per entity:
  ```csharp
  builder.Property(o => o.Id).HasConversion(id => id.Value, v => new OrderId(v));
  builder.HasQueryFilter(o => !o.IsDeleted);          // if you want global soft-delete filtering
  ```
- `ApplyConfigurationsFromAssembly(Persistence.EntityFrameworkCore.AssemblyReference.Assembly)` would add **all** framework tables (audit, event history, inbox, projections, sagas) — only do that if you want them all.
- Shipped configurations use SQL Server types (`nvarchar(max)` in EventHistory.Payload, AuditLog.Details, SagaInstances.SerializedData). On PostgreSQL override them — see `pivot-persistence-postgresql`.
- Migrations are owned by the consuming service (`dotnet ef migrations add … --context AppDbContext`).

## 2. Unit of work

`UnitOfWork<TContext>` is **abstract** — every service subclasses it:

```csharp
public sealed class AppUnitOfWork(
	AppDbContext dbContext,
	ICurrentUserProvider currentUserProvider,
	IDomainEventPublisher<AppDbContext> domainEventPublisher,
	IIntegrationEventMappingCoordinator<AppDbContext>? integrationEventMappingCoordinator = null)
	: UnitOfWork<AppDbContext>(dbContext, currentUserProvider, domainEventPublisher, integrationEventMappingCoordinator);
```
Forward the optional coordinator — if your subclass omits that parameter, `AddIntegrationEventMapping<TContext>()`
silently has no effect.

`SaveChangesAsync(ct)` → `Task<Result>`, in this order:
1. `UpdateAuditableEntities()` — Added → `SetAudit(AuditInfo.Create(now, actor))`; Modified → `Audit.Modify(now, actor)` (actor = `ICurrentUserProvider.GetCurrentUser()`). ⚠ A Modified entity with `Audit == null` makes the save fail (`UnexpectedError`).
2. Collect tracked `IAggregateRoot`s that have domain events.
3. `PersistDomainEventsToOutboxAsync` — for each event, `IDomainEventPublisher<TContext>.PublishAsync(event, aggregate)` → adds `OutboxMessage` (+ `EventHistoryEntry` if event store enabled) to the **same** change tracker.
4. `PublishMappedIntegrationEventsAsync` — only if a mapping coordinator was injected.
5. `DbContext.SaveChangesAsync` — aggregate changes + outbox rows commit atomically.
6. `ClearDomainEvents()` on each aggregate.
Errors become `Result.Failure` with codes `DbUpdateConcurrencyError`, `DatabaseError`, `UnexpectedError`, `DomainEventOutboxError` (or the publisher's own); `OperationCanceledException` is rethrown. All steps are `protected virtual` — override to customise (e.g. a different actor source).

Only call `SaveChangesAsync` once per command; the outbox write depends on events still being on the aggregates.

## 3. Aggregate repositories

`BaseAsyncCommandRepository<TEntity, TId>(DbContext)` implements `IAsyncCommandRepository` — subclass per aggregate:

```csharp
public interface IOrderRepository : IAsyncCommandRepository<Order, OrderId>
{
	Task<Order?> FindWithLinesAsync(OrderId id, CancellationToken ct);
}

internal sealed class OrderRepository(AppDbContext db)
	: BaseAsyncCommandRepository<Order, OrderId>(db), IOrderRepository
{
	public Task<Order?> FindWithLinesAsync(OrderId id, CancellationToken ct)
		=> DbContext.Set<Order>().Include(o => o.Lines).FirstOrDefaultAsync(o => o.Id == id, ct);
}
// services.AddScoped<IOrderRepository, OrderRepository>();
```
Semantics: `AddAsync`/`UpdateAsync`/`DeleteAsync` only change tracker state (no save). `UpdateAsync` attaches detached
entities and marks the whole entity Modified. `DeleteAsync` **hard-deletes** unless the entity is an
`ISoftDeletableEntity` already marked `IsDeleted` (then it's saved as Modified) — so soft delete = call the domain
method (`order.Cancel(actor)`) then `DeleteAsync`/`UpdateAsync`. `FindByIdAsync` does **not** filter soft-deleted rows.

## 4. Specifications (write-side queries)

```csharp
public sealed class PendingOrdersSpec : EntitySpecification<Order, OrderId>
{
	public PendingOrdersSpec() : base(o => o.Status == OrderStatus.Pending)
	{
		AddInclude(o => o.Lines);
		AddOrderBy(o => o.Audit!.CreatedOnUtc);
		ApplyPaging(0, 20);
		EnableSplitQuery();          // optional
		// EnableTracking();         // default AsNoTracking = true
		// EnableIncludeSoftDeleted();
	}
}
var orders = await EntitySpecificationEvaluator
	.GetQuery<Order, OrderId>(db.Set<Order>(), new PendingOrdersSpec())
	.ToListAsync(ct);
```
Evaluator order: AsNoTracking → exclude soft-deleted (unless enabled) → criteria → includes → order → skip/take → split query.
`ReadModelSpecificationEvaluator.GetQuery` is the read-model counterpart (always no-tracking; ignores ThenBy).

## 5. Transactions

`TransactionManager<TContext>` wraps `Database.BeginTransactionAsync/Commit/Rollback` and is idempotent
(no nested transactions; commit/rollback no-op without a current transaction). Used by `TransactionMiddleware`
(HTTP, non-GET) and `GrpcTransactionInterceptor`. With `EnableRetryOnFailure` execution strategies, user-initiated
transactions need `CreateExecutionStrategy()` — the middleware does not do that; don't enable retrying strategies
together with `TransactionMiddleware` without adapting it.

## 6. Read models on EF Core

`services.AddEfCoreReadModelStore<AppReadDbContext>()` registers `DbContext → TContext` and open generics
`IReadModelRepository<,> → EfCoreReadModelRepository<,>`, `IReadModelStore<,> → EfCoreReadModelStore<,>`.
- Repository: all queries `AsNoTracking`; `ListAsync(spec)` uses `ReadModelSpecificationEvaluator`.
- Store: `UpsertAsync` = Find → `SetValues` or `Add`, then **SaveChanges immediately**; on `DbUpdateException` (concurrent insert) it detaches **all** tracked entries and retries as update. Use a dedicated read context so this doesn't interfere with write-side tracking.
- Both depend on the non-generic `DbContext` registration — only one context can be "the" read-model context per container.

## 7. Other components

| Component | Registration | Notes |
|---|---|---|
| `AuditLogService` (`IAuditLogService`) | `services.AddAuditLog<TContext>()` | Writes `AuditEntry` and calls `SaveChangesAsync` immediately (outside UoW atomicity). `QueryAsync` filters + orders by `OccurredAtUtc` desc. Needs `AuditEntryConfiguration` (table `AuditLog`). |
| `MediatRDomainEventDispatcher` | `services.AddDomainEventDispatcher()` | Wraps event in `DomainEventNotification<T>` and `IMediator.Publish`. |
| `MediatRIntegrationEventDispatcher` | `services.AddIntegrationEventDispatcher()` | Same with `IntegrationEventNotification<T>`. |
| `HttpContextCurrentUserProvider` | via `AddEfCoreWritePersistence` (TryAdd) | `User.Identity.Name` or `"System"`. Register your own `ICurrentUserProvider` *before* to override. |
| `AddReadWriteDbContexts<TWrite,TRead>(writeCs, readCs, …)` | SQL Server | Read context: `NoTracking` + `SplitQuery`. PostgreSQL twin in `pivot-persistence-postgresql`. |
| `DomainEventPublisher<TContext>` / `IntegrationEventPublisher<TContext>` / `OutboxRepository<TContext>` / `OutboxProcessor<TContext>` / `InboxRepository` / `InboxService` / `IdempotentCommandBehavior` / `IntegrationEventMappingCoordinator` | see `pivot-messaging-outbox` | |
| `EventStoreRepository`, `ProjectionCheckpointStore`, `ProjectionRebuilder`, `ProjectionCoordinator`, `EventVersionRegistry`, `SagaRepository`, `SagaOrchestrator` | see `pivot-event-store-sagas` | |

### How `DomainEventPublisher` fills the envelope
`EventType = AssemblyQualifiedName`, `EventVersion = 1`, `ProducerService = AppDomain.CurrentDomain.FriendlyName`,
`CorrelationId/CausationId/ReplayFlag` from the ambient contexts, `AggregateType = aggregate.GetType().Name`,
`AggregateId = aggregate.Id.ToString()` (reflection), `AggregateVersion` = aggregate.Version minus the number of
pending events + index + 1. The unique index on (AggregateId, AggregateType, AggregateVersion) in `EventHistory`
therefore requires that `Version` is persisted/rehydrated correctly on the aggregate (map it as a column).

## Changing this package

- Keep implementations generic over `TContext : DbContext, IPersistenceContext` and register them from the Messaging package's extensions (or here for read-side helpers) with `TryAdd*`.
- Infrastructure methods convert exceptions to `Result.Failure(new Error("Area.Reason", ex.Message))`, rethrow cancellations, and never call `SaveChanges` from repository `Add` methods (atomicity with the UoW).
- There is no dedicated test project; outbox/inbox/saga behaviour is tested in `Tests/Pivot.Framework.Containers.API.Tests` (folders `Outbox`, `Inbox`, `Sagas`) using `UseInMemoryDatabase` test contexts that implement `IPersistenceContext`.
