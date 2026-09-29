---
name: pivot-event-store-sagas
description: Pivot.Framework event store, projection rebuilds, projection versioning/promotion, event upcasting, and orchestrated sagas (EF Core implementations). Use when enabling includeEventStore/AddEventStore, reading an aggregate's event stream, rebuilding or blue/green-promoting a projection with IProjectionRebuilder/IProjectionCoordinator, versioning events with IEventUpgrader/EventVersionRegistry, or defining and running sagas with ISagaDefinition/ISagaStep/ISagaOrchestrator (AddSagaSupport), and when changing those classes.
---

# Event store, projections and sagas

Contracts: `Pivot.Framework.Infrastructure.Abstraction.EventStore.*`, `.Sagas.*`, `Pivot.Framework.Application.Abstractions.Sagas`, `.Replay`.
Implementations: `Pivot.Framework.Infrastructure.Persistence.EntityFrameworkCore` (`EventStore/*`, `Sagas/*`).
Registrations: `Pivot.Framework.Infrastructure.Messaging.EntityFrameworkCore.Extensions` (`EventStoreExtensions`, `ProjectionCoordinatorExtensions`) and `.Sagas.Extensions`.

This is **not** full event sourcing: aggregates are still persisted as state by EF Core. The event store is an
append-only *history* (`EventHistory` table) written in the same transaction as the outbox, used for audit,
aggregate stream inspection and projection rebuilds.

## 1. Enabling the event store

```csharp
services.AddEfCoreWritePersistence<AppDbContext, AppUnitOfWork>(includeEventStore: true);
// or separately: services.AddEventStore<AppDbContext>();
services.AddScoped<DbContext>(sp => sp.GetRequiredService<AppDbContext>());   // REQUIRED, see below
```
`AddEventStore<T>` registers (TryAdd): `IEventStoreRepository<T> → EventStoreRepository<T>`,
`IProjectionCheckpointStore → ProjectionCheckpointStore`, `IEventVersionRegistry → EventVersionRegistry` (singleton),
`IProjectionRebuilder → ProjectionRebuilder<T>`.
- `ProjectionCheckpointStore` depends on the **non-generic `DbContext`** — register the mapping above (or `AddEfCoreReadModelStore<T>()`, which does it) or resolution fails.
- Model: `modelBuilder.ApplyConfiguration(new EventHistoryEntryConfiguration())` (SQL Server) or `ApplyPostgreSqlConfigurations()` (PostgreSQL), plus `ProjectionCheckpointConfiguration` if you rebuild, `ProjectionRegistrationConfiguration` if you use the coordinator.
- `EventHistory` has a **unique** index on `(AggregateId, AggregateType, AggregateVersion)`. The version comes from the aggregate's `Version` (see `pivot-persistence-efcore`), so persist `Version` as a column on your aggregates; otherwise the second save of a reloaded aggregate collides.
- Once `IEventStoreRepository<T>` is registered, `DomainEventPublisher<T>` writes an `EventHistoryEntry` for every domain event (same `Id` as the outbox message); failure to append fails the save.

## 2. Reading streams

```csharp
var entries = await eventStore.GetByAggregateIdAsync(order.Id.ToString(), nameof(Order), ct);          // ordered by AggregateVersion
var since   = await eventStore.GetByAggregateIdFromVersionAsync(order.Id.ToString(), nameof(Order), 5, ct);
var page    = await eventStore.GetFromPositionAsync(fromPosition: 0, maxCount: 500, ct);                  // global, ordered by CreatedAtUtc, Id
long head   = await eventStore.GetCurrentPositionAsync(ct);                                             // = row count
```
`AggregateType` is the aggregate's simple class name; `AggregateId` its `Id.ToString()`. "Position" is an
**offset** (row count), not a sequence number — rows inserted with earlier `CreatedAtUtc` after a checkpoint
shift positions. Keep that in mind for live catch-up subscriptions.

## 3. Event versioning (upcasting)

`EventHistoryEntry.EventVersion` is always written as `1` today. `EventVersionRegistry` can upgrade old payloads when
you read history:
```csharp
var registry = new EventVersionRegistry();
registry.RegisterEventType<OrderCreatedDomainEvent>(currentVersion: 2);
registry.RegisterUpgrader(new OrderCreatedV1ToV2Upgrader());   // IEventUpgrader<OrderCreatedDomainEvent> { FromVersion = 1; ToVersion = 2; Upgrade(payload) }
services.AddSingleton<IEventVersionRegistry>(registry);         // BEFORE AddEventStore (TryAdd keeps yours)

// in a projection handler:
var evt = (OrderCreatedDomainEvent)registry.DeserializeAndUpgrade(entry.EventType, entry.EventVersion, entry.Payload);
```
- Keys are `AssemblyQualifiedName`s (what `EventType` stores). Missing upgrader in the chain → `InvalidOperationException`.
- Each step's output is re-serialized (Newtonsoft) as input to the next.
- `services.AddEventUpgrader(upgrader)` only adds the upgrader as a singleton — it does **not** register it with the registry. Configure the registry instance yourself as above.
- Nothing in the framework calls `DeserializeAndUpgrade` automatically (the RabbitMQ receiver deserializes directly).

## 4. Projection rebuilds

Two different `IProjectionHandler` interfaces exist — rebuilds use the **Infrastructure.Abstraction** one:
```csharp
public sealed class OrderSummaryProjectionV2(IReadModelStore<OrderSummary, Guid> store, IEventVersionRegistry versions)
	: Pivot.Framework.Infrastructure.Abstraction.EventStore.Projections.IProjectionHandler
{
	public string ProjectionName => "OrderSummary";
	public int ProjectionVersion => 2;

	public async Task HandleAsync(EventHistoryEntry entry, CancellationToken ct = default)
	{
		if (!entry.EventType.StartsWith(typeof(OrderCreatedDomainEvent).FullName!)) return;
		var e = (OrderCreatedDomainEvent)versions.DeserializeAndUpgrade(entry.EventType, entry.EventVersion, entry.Payload);
		await store.UpsertAsync(new OrderSummary(e.OrderId) { Status = "Pending" }, ct);
	}
}

var result = await rebuilder.RebuildAsync(new OrderSummaryProjectionV2(store, versions), batchSize: 1000, ct);
```
`ProjectionRebuilder<T>`: loads checkpoint `(name, version)` → resumes from `LastProcessedPosition` → inside
`ReplayContext.BeginReplayScope()` reads batches via `GetFromPositionAsync`, calls `HandleAsync` for every entry,
saves a checkpoint after each batch → stops when a batch is empty or cancellation is requested. Returns
`Projection.RebuildFailed` on exceptions (cancellation rethrown). Handlers must be idempotent (a crash mid-batch
replays that batch) and should skip side effects when `ReplayContext.IsReplaying`. Checkpoint saves call
`SaveChangesAsync` on the shared `DbContext`.
Run rebuilds from a background job/CLI, not an HTTP request (long-running, and `TransactionMiddleware` would wrap it).

## 5. Projection version promotion (`IProjectionCoordinator`)

`services.AddProjectionCoordinator<AppDbContext>()` → `ProjectionCoordinator<T>` (table `ProjectionRegistrations`).
State machine (each call validates the current state, else `Projection.InvalidState`):
```
RegisterProjectionAsync(name, v1)            → Active (ActiveVersion = 1)
PlanRebuildAsync(name, 2)        Active       → RebuildPlanned (RebuildTargetVersion = 2)
StartRebuildAsync(name, 2)       RebuildPlanned → Rebuilding
CompleteRebuildAsync(name, 2, finalPosition)  Rebuilding → ParityChecking
RecordParityCheckAsync(name, 2, passed)       ParityChecking → PromotionReady (passed) | RebuildPlanned (failed)
PromoteVersionAsync(name, 2)     PromotionReady → Active (ActiveVersion = 2, rebuild fields cleared)
```
The coordinator records state only: *you* run the rebuilder between Start and Complete, implement the parity check
(e.g. compare counts/checksums between v1 and v2 read models), switch readers to the new version, and publish
`ProjectionVersionPromotedEvent` if other services care (it is defined but never emitted by the framework).
`targetVersion` arguments after `PlanRebuildAsync` are not cross-checked against `RebuildTargetVersion`.
The RabbitMQ receiver namespaces inbox keys by the `ProjectionVersion` header so replay traffic doesn't collide with live dedup keys.

## 6. Sagas (orchestration)

```csharp
services.AddSagaSupport<AppDbContext>();     // ISagaRepository<> → SagaRepository<>, ISagaOrchestrator → SagaOrchestrator<AppDbContext>
// model: SagaInstanceConfiguration + SagaStepRecordConfiguration (tables SagaInstances, SagaStepRecords)
```
Define data + steps + definition:
```csharp
public sealed class PlaceOrderSagaData
{
	public Guid OrderId { get; set; }
	public Guid? PaymentId { get; set; }        // filled by a step, used by its compensation
}

public sealed class ChargePaymentStep(IPaymentsClient payments) : ISagaStep<PlaceOrderSagaData>
{
	public string StepName => "ChargePayment";
	public async Task<Result> ExecuteAsync(PlaceOrderSagaData d, CancellationToken ct)
	{
		var r = await payments.ChargeAsync(d.OrderId, ct);
		if (r.IsFailure) return r;
		d.PaymentId = r.Value;                   // persisted after the step succeeds
		return Result.Success();
	}
	public Task<Result> CompensateAsync(PlaceOrderSagaData d, CancellationToken ct)
		=> d.PaymentId is null ? Task.FromResult(Result.Success()) : payments.RefundAsync(d.PaymentId.Value, ct);
}

public sealed class PlaceOrderSaga(ReserveStockStep reserve, ChargePaymentStep charge) : ISagaDefinition<PlaceOrderSagaData>
{
	public string SagaType => "PlaceOrder";
	public IReadOnlyList<ISagaStep<PlaceOrderSagaData>> Steps => [reserve, charge];
}

Result<Guid> started = await orchestrator.StartAsync(saga, new PlaceOrderSagaData { OrderId = id }, CorrelationContext.CorrelationId, ct);
```

Semantics of `SagaOrchestrator<T>` (all synchronous within the calling request/job):
- `StartAsync`: no steps → `Saga.NoSteps`. Creates `SagaInstance` (InProgress, Version 1, JSON data) and one `Pending` `SagaStepRecord` per step, saves, then executes.
- For each step from `CurrentStepIndex`: mark Executing + save → `ExecuteAsync` → success: step Completed, data re-serialized, save → next. After the last step: `Completed`, returns `Result.Success()` (Start returns the saga Id).
- Step failure: step Failed, instance `Compensating` with `FailureReason`, then compensates **previously Completed steps in reverse** (the failed step itself is not compensated). If all compensations succeed: state `Compensated` and the call returns **failure** `Saga.Compensated`. A compensation failure: state `Failed`, `Saga.CompensationFailed` — manual intervention required.
- `ResumeAsync(sagaId, definition)`: `Saga.NotFound` / `Saga.AlreadyTerminated` (Completed/Compensated/Failed) / `Saga.DeserializationFailed`; Compensating → continue compensation; otherwise re-run from `CurrentStepIndex` (that step is executed **again**).
- Every state change bumps `SagaInstance.Version` (EF concurrency token) and calls `SaveChangesAsync` on the **whole** context — any other pending changes in that context are committed at step boundaries.

Design rules:
- Steps must be **idempotent** (resume re-executes the current step) and compensations must tolerate "nothing to undo".
- Store everything compensation needs in the saga data during `ExecuteAsync`.
- Saga data must be Newtonsoft-serializable (public setters or a `[JsonConstructor]`).
- Caveat: if the process dies while compensating step *i*, `ResumeAsync` continues from *i − 1* (it uses `CurrentStepIndex - 1`), so step *i*'s compensation may be skipped — check `SagaStepRecords` with status `Compensating` when recovering.
- There is no built-in timeout/recovery scheduler: find stuck sagas (`State` InProgress/Compensating older than X, index `IX_SagaInstances_SagaType_State`) with a scheduled job and call `ResumeAsync`.
- Avoid starting sagas inside a `TransactionMiddleware`-wrapped HTTP request if external side effects occur — a later rollback would undo the saga's own bookkeeping but not the external calls.

## Tests
`Tests/Pivot.Framework.Containers.API.Tests/Sagas`, `Tests/Pivot.Framework.Infrastructure.Abstraction.Tests/EventStore` and `/Sagas`, `Tests/Pivot.Framework.Domain.Tests/Sagas`. Use EF InMemory contexts implementing `IPersistenceContext` with the relevant configurations applied.
