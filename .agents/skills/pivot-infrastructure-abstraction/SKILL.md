---
name: pivot-infrastructure-abstraction
description: Pivot.Framework.Infrastructure.Abstraction — the pure-interface contract layer (IPersistenceContext, IUnitOfWork<TContext>, ITransactionManager, outbox models/repository/processor/publishers/routing/retry/drain modes, inbox, message broker interfaces and RabbitMQSettings/topology options, event store models and repositories, projection coordinator, sagas models, scheduling, BFF, audit log, search, object storage, service client options). Use when a consuming layer needs one of these abstractions, when implementing a new adapter for one of them, or when changing contracts in Src/Infrastructure/Pivot.Framework.Infrastructure.Abstraction.
---

# Pivot.Framework.Infrastructure.Abstraction

Location: `Src/Infrastructure/Pivot.Framework.Infrastructure.Abstraction`. References **only** `Pivot.Framework.Domain`.
Rule: interfaces, options and plain models only. No EF Core, RabbitMQ, Redis, Hangfire, HTTP types. If a contract
needs one of those, it belongs in a concrete infrastructure package instead.

## Persistence plumbing

| Contract | Notes |
|---|---|
| `Persistence/IPersistenceContext` | Empty marker. Every write `DbContext` implements it (via `PivotDbContextBase`). All context-bound contracts are generic over `TContext : class, IPersistenceContext` so several contexts can live in one process with distinct DI registrations. |
| `UnitOfWork/IUnitOfWork<TContext> : Domain.Repositories.IUnitOfWork` | No extra members; the generic parameter exists only for DI keying. Inject `IUnitOfWork<AppDbContext>` in handlers. |
| `Transaction/ITransactionManager<TContext>` | `BeginTransactionAsync`, `CommitTransactionAsync`, `RollbackTransactionAsync` (all take `CancellationToken`). EF impl is no-op when a transaction already exists / doesn't exist. |

## Outbox (namespace `…Abstraction.Outbox.*`)

- `Models/OutboxMessage` — `Id` (= event Id), `Payload` (JSON), `EventType` (assembly-qualified), `CreatedAtUtc`, `RetryCount`, `Processed`, `ProcessedAtUtc`, `CorrelationId`, `Kind` (`MessageKind.DomainEvent = 0` / `IntegrationEvent = 1`), `FailedAtUtc`, `LastError`.
- `Models/OutboxMessageConsumer` — `(Id, Name)` composite key; reused as the **inbox** table (`OutboxMessageConsumers`).
- `Repositories/IOutboxRepository<TContext>` — `AddAsync`, `MarkAsProcessedAsync`, `GetUnprocessedMessagesAsync` (ordered by `CreatedAtUtc`). Implementations must **not** call `SaveChanges` in `AddAsync` (the unit of work commits it atomically with the aggregate).
- `DomainEventPublisher/IDomainEventPublisher` (+ `IDomainEventPublisher<TContext>`) — `PublishAsync(domainEvent, ct)` and `PublishAsync(domainEvent, aggregate, ct)` (aggregate-aware: fills AggregateType/Id/Version). "Publish" means **enqueue into the outbox**, not send to a broker.
- `IntegrationEventPublisher/IIntegrationEventPublisher` (+ `<TContext>`) — enqueue an `IIntegrationEvent` as `MessageKind.IntegrationEvent`.
- `IntegrationEventMapping/IIntegrationEventMapper<in TDomainEvent>` — `IEnumerable<IIntegrationEvent> Map(TDomainEvent)`; `IIntegrationEventMappingCoordinator<TContext>` — `PublishMappedIntegrationEventsAsync(domainEvents)`.
- `Processor/IOutboxProcessor<TContext>` — `ProcessOutboxMessagesAsync(ct)` (one drain pass).
- `Publishing/IOutboxRoutingResolver` — `OutboxRoute Resolve(OutboxMessage)`; `OutboxRoute(string Exchange, string RoutingKey)` record.
- `Retry/OutboxRetryOptions` — `MaxRetryCount = 5`, `EmitFailureEvent = true`.
- `DrainMode/OutboxDrainMode` — `ImmediateAfterRequest = 1`, `BackgroundPolling = 2`; `OutboxDrainOptions { Mode, PollingInterval = 5s }`; `OutboxDrainRegistrationMarker(Mode)` singleton used to enforce one mode per process.

## Inbox

- `Inbox/IInboxService` — `HasBeenProcessedAsync(messageId, consumerName)`, `RecordConsumptionAsync(messageId, consumerName)`, `SaveChangesAsync()`.
- `Inbox/Repositories/IInboxRepository<TContext>` — same first two methods, context-bound.

## Message brokers

- `MessageBrokers/Shared/*`: `IMessagePublisher : IDisposable` (`Task<Result> PublishAsync(OutboxMessage)`), `IMessageReceiver : IDisposable` (`InitializeAsync`, `StartListeningAsync`, default-implemented `StopListeningAsync`), `IMessageSerializer`, `IMessageCompressor`, `IMessageEncryptor` (byte[] in/out).
- `MessageBrokers/RabbitMQ/Models/RabbitMQSettings` — all `required`: `Port`, `Queue`, `HostName`, `UserName`, `Password`, `Exchange`, `RoutingKey`, `VirtualHost`, `EncryptionKey` (32 ASCII chars for AES-256), `ClientProvidedName`. Bound from config section `"RabbitMQ"`.
- `MessageBrokers/RabbitMQ/Topology`: `IRabbitMQTopologyManager.DeclareTopologyAsync`; `TopologyOptions.Bind(ExchangeBinding)`; `ExchangeBinding { Exchange, ExchangeType = "topic", Queue, RoutingKey, ConsumerName?, EnableDeadLetterQueue = true, MaxRetryCount = 3, RetryDelayMs = 5000, UseQuorumQueue = true }`.

## Event store (`EventStore/*`)

- `Models/EventEnvelope` — transient envelope: EventId, EventType, EventVersion (=1 today), OccurredOnUtc, ProducerService, CorrelationId, CausationId, AggregateType/Id/Version, ReplayFlag, Payload.
- `Models/EventHistoryEntry` — persisted row (same fields + `Id`, `CreatedAtUtc`).
- `Models/ProjectionCheckpoint` — `ProjectionName`, `ProjectionVersion`, `LastProcessedPosition`, `LastProcessedEventId`, `LastUpdatedUtc`.
- `Repositories/IEventStoreRepository<TContext>` — `AppendAsync`, `GetByAggregateIdAsync`, `GetByAggregateIdFromVersionAsync`, `GetFromPositionAsync(from, max)`, `GetCurrentPositionAsync`.
- `Repositories/IProjectionCheckpointStore` — `GetCheckpointAsync(name, version)`, `SaveCheckpointAsync`.
- `Projections/IProjectionHandler` (**different** from the Application one!) — `ProjectionName`, `ProjectionVersion`, `HandleAsync(EventHistoryEntry)`; `IProjectionRebuilder.RebuildAsync(handler, batchSize = 1000)`.
- `Versioning/IEventUpgrader<TEvent>` (`FromVersion`, `ToVersion`, `TEvent Upgrade(string payload)`), `IEventVersionRegistry` (`GetCurrentVersion`, `DeserializeAndUpgrade`).
- `Coordinator/IProjectionCoordinator` + `ProjectionRegistration` + `ProjectionState` (Active → RebuildPlanned → Rebuilding → ParityChecking → PromotionReady → Active; Deprecated) + `ProjectionVersionPromotedEvent` (integration event).

Details and usage: `pivot-event-store-sagas`.

## Sagas
`Sagas/Models/SagaInstance` (Id, SagaType, State, CurrentStepIndex, SerializedData, CorrelationId, StartedAtUtc, CompletedAtUtc, FailureReason, `Version` = concurrency token), `SagaStepRecord`, `Sagas/Repositories/ISagaRepository<TContext>` (Get/Add/Update instance, Get/Add/Update step records, `SaveChangesAsync`).

## Other service contracts (no framework implementation unless noted)

| Contract | Implementation in framework |
|---|---|
| `Scheduling/Services/IRecurringJobService<TIdentifier, TParams, TValue>` + `Configurations/RecurrenceConfig { Type, Interval, ToCronExpression() }` + `Enums/RecurrenceType` | `RecurringJobService` (Hangfire) — `pivot-scheduling-hangfire` |
| `BFF/IBffCacheService`, `BffCacheOptions` (`AddCacheable(key, ttl)`, `AddNeverCached(key)`), `BffResponse<T>` (`Ok`, `Degraded`, `Unavailable(retryAfter)`), `DataAvailability`, `DegradedComponent` | `InMemoryBffCacheService`, `BffResponseFilter` — `pivot-containers-api` |
| `Audit/IAuditLogService` + `AuditEntry`/`AuditQuery` | `AuditLogService` (EF) — `pivot-persistence-efcore` |
| `ServiceClients/IServiceClient` + `ServiceClientOptions` (BaseUrl, RetryCount 3, RetryBaseDelaySeconds 1, CircuitBreakerThreshold 5, CircuitBreakerDurationSeconds 30, TimeoutSeconds 30) | `AddServiceClient<TClient,TImpl>` — `pivot-containers-api` |
| `Search/ISearchService<TDocument>` + `SearchRequest`/`SearchResult<T>` | none (implement per backend, e.g. Elasticsearch/OpenSearch) |
| `ObjectStorage/IObjectStorageService` + `ObjectInfo` | none (implement per backend, e.g. MinIO/S3/Azure Blob) |

`RecurrenceConfig.ToCronExpression()`: Hourly → `0 */N * * *`, Daily → `0 0 */N * *`, Weekly → `0 0 * * 0` (N must be 1), Monthly → `0 0 1 */N *`, Yearly → `0 0 1 1 *` (N must be 1); otherwise `NotSupportedException`. Note: the file is `RecurrenceConfiguration.cs` but the class is `RecurrenceConfig`.

## Implementing a new adapter (e.g. `IObjectStorageService` for MinIO)

1. Create a new package `Src/Infrastructure/Pivot.Framework.Infrastructure.ObjectStorage.Minio` referencing Abstraction (+ the SDK). Don't put SDK code into Abstraction.
2. Return `Result`/`Result<T>` with dotted error codes (`"ObjectStorage.UploadFailed"`), catch SDK exceptions, rethrow `OperationCanceledException`.
3. Add `AddMinioObjectStorage(this IServiceCollection, …)` using `TryAdd*`.
4. Follow header-comment/regions/tabs conventions; add to the `.sln`; add tests.

## Changing contracts

Every interface here has implementations in other packages and possibly in consuming services. Adding a member is a
breaking change for implementers — prefer a new interface or a default interface method (as done for
`IMessageReceiver.StopListeningAsync`). Update `Tests/Pivot.Framework.Infrastructure.Abstraction.Tests`
(folders Audit, BFF, EventStore, MessageBrokers, ObjectStorage, Outbox, Sagas, Scheduling, Search, ServiceClients, Topology).
