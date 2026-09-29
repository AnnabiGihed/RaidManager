---
name: pivot-messaging-outbox
description: Pivot.Framework.Infrastructure.Messaging.EntityFrameworkCore — transactional outbox, transports and consumer side. Use when registering AddEfCoreWritePersistence, choosing RabbitMQ vs in-process transport, configuring outbox drain modes (AddOutboxDraining, UseImmediateOutboxDraining, ConfigureOutboxRetry), RabbitMQ settings/topology/routing (AddRabbitMQPublisher, AddRabbitMQTopology, IOutboxRoutingResolver), consuming events with RabbitMQReceiver, the inbox and idempotent commands, domain→integration event mapping, publishing integration events, the projection dispatcher, or debugging why events are not delivered / are duplicated / land in a DLQ. Also for changes in Src/Infrastructure/Pivot.Framework.Infrastructure.Messaging.EntityFrameworkCore.
---

# Pivot.Framework.Infrastructure.Messaging.EntityFrameworkCore

Location: `Src/Infrastructure/Pivot.Framework.Infrastructure.Messaging.EntityFrameworkCore`.
References Application + Persistence.EntityFrameworkCore, `RabbitMQ.Client` 7 (async API), `Polly` 8 (v7-style policies),
`Quartz` (referenced, unused), Newtonsoft.Json. Contains the **DI entry points** for most EF-based features and the
RabbitMQ/in-process transports.

## End-to-end flow

```
Handler → aggregate.RaiseDomainEvent → IUnitOfWork<T>.SaveChangesAsync
   ├─ DomainEventPublisher<T>  → OutboxMessage(Kind=DomainEvent) [+ EventHistoryEntry]
   ├─ IntegrationEventMappingCoordinator<T> (opt-in) → OutboxMessage(Kind=IntegrationEvent)
   └─ DbContext.SaveChanges  (aggregate + outbox rows in one transaction)
Drain (one mode per process):
   ImmediateAfterRequest: OutboxProcessingMiddleware → IOutboxProcessor<T> after a 2xx response
   BackgroundPolling:     OutboxPublisherService<T> every PollingInterval
      → IMessagePublisher.PublishAsync(OutboxMessage)
         ├─ InProcessMessagePublisher → IDomainEventDispatcher (MediatR) in a new scope
         └─ RabbitMQPublisher → GZip → AES-256 → exchange/routing key
Consumer service: RabbitMQReceiverHostedService → RabbitMQReceiver
   → decrypt → decompress → Type.GetType(EventType) → Newtonsoft → inbox check
   → IDomainEventDispatcher / IIntegrationEventDispatcher (MediatR handlers) → inbox record → ack
```

## 1. Write-side bundle

```csharp
services.AddEfCoreWritePersistence<AppDbContext, AppUnitOfWork>(includeEventStore: false);
```
Registers (TryAdd, scoped): `IHttpContextAccessor`, `ITransactionManager<T>`, `IOutboxRepository<T>`,
`IDomainEventPublisher<T>` (+ non-generic alias — prefer the generic one when several contexts exist),
`ICurrentUserProvider → HttpContextCurrentUserProvider`, `IUnitOfWork<T> → TUnitOfWork`. `includeEventStore: true`
also calls `AddEventStore<T>()` (see `pivot-event-store-sagas`; it needs a non-generic `DbContext` registration).
It does **not** register a transport, a drain mode, MediatR dispatchers, or the DbContext itself.

## 2. Transport — pick exactly one `IMessagePublisher`

### In-process: `services.AddInProcessMessagePublisher()`
Singleton. Deserializes the domain event and dispatches it through `IDomainEventDispatcher` in a fresh scope
(so also call `services.AddDomainEventDispatcher()`). Uses the inbox (consumer name `"InProcessMessagePublisher"`) if
`IInboxService` is registered. **Integration-event messages always fail** (`InProcessPublisher.IntegrationEventsNotSupported`) →
they are retried until dead-lettered. With this transport set `ConfigureOutboxRetry(o => o.EmitFailureEvent = false)`
(the failure event is itself an integration event and can never be delivered), and don't enable integration-event mapping.

### RabbitMQ: `services.AddRabbitMQPublisher(configuration)`
Registers: `RabbitMQSettings` from section `"RabbitMQ"`, `MessagingResiliencePolicies` (retry 3× with 2/4/8 s backoff
inside a circuit breaker opening after 2 failures for 1 min), `IMessageEncryptor → AesMessageEncryptor(EncryptionKey)`,
`IMessageCompressor → GZipMessageCompressor`, `IMessagePublisher → RabbitMQPublisher`, `IMessageReceiver → RabbitMQReceiver`,
**and** `RabbitMQReceiverHostedService` — i.e. every service using this call also *consumes* from `RabbitMQ:Queue`.

```json
"RabbitMQ": {
  "HostName": "localhost", "Port": 5672, "VirtualHost": "/",
  "UserName": "guest", "Password": "guest",
  "Exchange": "orders.events", "RoutingKey": "orders.#",
  "Queue": "orders-service", "ClientProvidedName": "orders-service",
  "EncryptionKey": "0123456789ABCDEF0123456789ABCDEF"
}
```
- `EncryptionKey` must be exactly 32 UTF-8 bytes (AES-256) and **identical in producer and consumers**; keep it in secrets, not appsettings.
- `ClientProvidedName` is also the **inbox consumer name** on the receiver side — make it unique per consuming service and stable across deployments.
- Wire format: body = AES-CBC(random IV prefixed) of GZip(UTF-8 JSON). Properties: `Type` = event `AssemblyQualifiedName`, `DeliveryMode` persistent, `mandatory: true`, headers `CorrelationId`, `EventId`, `Timestamp` (unix ms). Non-.NET consumers must replicate this.
- Connection: one lazily-created connection+channel per publisher (singleton), recreated if closed, guarded by a semaphore.

### Routing
Default route = `RabbitMQSettings.Exchange` + `RoutingKey`. Per-message routing:
```csharp
public sealed class OrdersRoutingResolver : IOutboxRoutingResolver
{
	public OutboxRoute Resolve(OutboxMessage message) => message.Kind switch
	{
		MessageKind.IntegrationEvent => new OutboxRoute("orders.integration", ShortName(message.EventType)),
		_ => new OutboxRoute("orders.domain", ShortName(message.EventType))
	};
	private static string ShortName(string? aqn) => aqn?.Split(',')[0].Split('.').Last() ?? "unknown";
}
services.AddOutboxRoutingResolver<OrdersRoutingResolver>();   // singleton, TryAdd
```
The resolver is used at publish time only; declare matching exchanges/queues/bindings yourself.

### Topology
```csharp
services.AddRabbitMQTopology(t => t
	.Bind(new ExchangeBinding { Exchange = "orders.integration", Queue = "billing-service", RoutingKey = "OrderShipped" })
	.Bind(new ExchangeBinding { Exchange = "orders.integration", Queue = "shipping-service", RoutingKey = "#", UseQuorumQueue = false }));
```
`TopologyHostedService` declares on startup (throws → app fails to start). Per binding: durable exchange (`topic` default);
if `EnableDeadLetterQueue`: `{Exchange}.dlx` (direct) + `{Queue}.dlq` and main-queue args `x-dead-letter-exchange`/`-routing-key`;
if `MaxRetryCount > 0 && RetryDelayMs > 0`: a `{Queue}.retry` queue with TTL that dead-letters back to the exchange; `x-queue-type: quorum` when `UseQuorumQueue`.
Caveats:
- Nothing routes messages *into* `{Queue}.retry` — rejected messages go to the DLQ. `MaxRetryCount` is not enforced by code.
- `RabbitMQReceiver` re-declares `RabbitMQ:Queue` with **no arguments**. If topology declared that same queue with quorum/DLX arguments, RabbitMQ may refuse the redeclare (`PRECONDITION_FAILED`, inequivalent args). Verify against your broker, and keep the receiver queue's declaration consistent (or subclass `RabbitMQReceiver` and override the protected `EnsureQueueExistsAsync`).
- The receiver's queue is not bound to any exchange by the receiver — binding must come from topology.

## 3. Draining the outbox (exactly one mode)

```csharp
services.AddOutboxDraining<AppDbContext>(o =>
{
	o.Mode = OutboxDrainMode.BackgroundPolling;      // recommended
	o.PollingInterval = TimeSpan.FromSeconds(5);
});
services.ConfigureOutboxRetry(o => { o.MaxRetryCount = 5; o.EmitFailureEvent = true; });
```
- Registers the `OutboxDrainRegistrationMarker`, `IOutboxProcessor<T> → OutboxProcessor<T>`, default `OutboxRetryOptions`, and for polling the `OutboxPublisherService<T>` hosted service. **A second call throws** `InvalidOperationException`.
- Immediate mode additionally needs `app.UseImmediateOutboxDraining<AppDbContext>()` (Containers.API), which throws at startup if the marker is missing or the mode is not ImmediateAfterRequest. It drains only after 2xx responses, inside the request scope, errors logged and swallowed.

Behavioural differences:
| | `OutboxProcessor` (immediate) | `OutboxPublisherService` (polling) |
|---|---|---|
| On publish failure | increments `RetryCount`, saves, **stops the pass** and returns the failure | increments, logs, **continues** with next message; saves once per cycle |
| Dead-letter at `RetryCount >= MaxRetryCount` | `Processed = true`, `FailedAtUtc`, optional `OutboxMessageFailedEvent` | same, but never emits a failure event for a failure event |
| Ordering | strict (stops at first failure) | best-effort (later messages can overtake a failing one) |

Neither takes a distributed lock — with several replicas polling, the same message can be published twice. Consumers must be idempotent (inbox). For single-drainer semantics on PostgreSQL wrap work in `PostgreSqlAdvisoryLock`.
Processed rows are never deleted — add a cleanup job (e.g. Hangfire) for `Processed = true` rows older than N days.

## 4. Consumer side (RabbitMQ receiver)

Needs: `AddRabbitMQPublisher(configuration)` (registers receiver + hosted service), MediatR with handlers,
`AddDomainEventDispatcher()` and/or `AddIntegrationEventDispatcher()`, and ideally `AddInboxSupport<TContext>()`.

Per message:
1. Correlation from header `CorrelationId` (or new Guid) → `CorrelationContext`; `CausationContext` = incoming event Id.
2. Decrypt/decompress; empty payload → nack (no requeue).
3. `Type.GetType(properties.Type)` — the **event CLR type must be loadable in the consumer** under the same assembly-qualified name (share a contracts assembly). Unknown → nack.
4. Newtonsoft deserialize; must be `IDomainEvent` or `IIntegrationEvent` else nack.
5. Inbox dedup key = event Id (or MD5(`ProjectionVersion:EventId`) when header `ProjectionVersion` is present); consumer name = `ClientProvidedName`. Already processed → ack.
6. Dispatch through MediatR; record inbox + `SaveChangesAsync`; ack.
7. **Any exception → `BasicNack(requeue: false)`** → message goes to the DLQ (if configured) or is dropped. There is no in-process retry.

⚠ Because event `Id`s are regenerated by Newtonsoft (see `pivot-domain` §4), step 5 does not deduplicate redeliveries unless your event types preserve `Id` via a `[JsonConstructor]`. Handlers that return `Result.Failure` are still acked (MediatR discards the result) — throw to trigger a nack.

The hosted service waits for `ApplicationStarted`, initializes, and runs until shutdown; startup failures are logged (the host keeps running without consuming). `RabbitMQReceiver`'s key members are `protected`/virtual-friendly for subclassing.

## 5. Inbox and idempotent commands

```csharp
services.AddInboxSupport<AppDbContext>();   // IInboxRepository<> (open generic) + IInboxService → InboxService<AppDbContext>
services.AddIdempotentCommands();           // IPipelineBehavior<,> → IdempotentCommandBehavior<,>
```
- Table: `OutboxMessageConsumers` (`OutboxMessageConsumerConfiguration`, composite key `(Id, Name)`).
- `InboxService.SaveChangesAsync` saves the whole context — if the handler left unsaved changes in the same context they are committed too.
- `IdempotentCommandBehavior` only acts on `IIdempotentCommand`; key = `IdempotencyKey`, consumer = request type name; records only on success. ⚠ Breaks (`InvalidCastException`) for `ICommand<T>` duplicates — use with non-generic commands.

## 6. Integration events

Direct publishing from a handler (same transaction as the UoW save):
```csharp
services.AddIntegrationEventPublisher<AppDbContext>();
// in a handler:
await integrationEventPublisher.PublishAsync(new OrderShippedIntegrationEvent(order.Id.Value), ct);
await unitOfWork.SaveChangesAsync(ct);   // commits the outbox row
```
`CorrelationId` = event's own or ambient `CorrelationContext`.

Mapping domain → integration events automatically inside `SaveChangesAsync`:
```csharp
public sealed class OrderShippedMapper : IIntegrationEventMapper<OrderShippedDomainEvent>
{
	public IEnumerable<IIntegrationEvent> Map(OrderShippedDomainEvent e)
		=> [new OrderShippedIntegrationEvent(e.OrderId)];
}
services.AddIntegrationEventMapping<AppDbContext>();   // also calls AddIntegrationEventPublisher<T>
services.AddScoped<IIntegrationEventMapper<OrderShippedDomainEvent>, OrderShippedMapper>();
```
Your `UnitOfWork` subclass **must** accept and forward `IIntegrationEventMappingCoordinator<TContext>?` (see
`pivot-persistence-efcore`). Mappers are resolved by the exact runtime event type (no base-type matching); a mapper
exception or null item fails the whole save. Delivery requires RabbitMQ (in-process cannot deliver integration events).

## 7. Projection dispatcher
`services.AddProjectionSupport()` → `IProjectionDispatcher → ProjectionDispatcher`: resolves all
`IProjectionHandler<TEvent>` for the event's runtime type, runs them all, aggregates failures into an
`AggregateException`. Register handlers as `services.AddScoped<IProjectionHandler<OrderCreatedDomainEvent>, OrderSummaryProjection>()`.
Remember `ProjectionHandler<T>` is also a MediatR handler — avoid running it twice (see `pivot-application`).

## Other registrations in this package
`AddEventStore<T>()`, `AddEventUpgrader<TEvent>(upgrader)`, `AddProjectionCoordinator<T>()`, `AddSagaSupport<T>()` → `pivot-event-store-sagas`.
`JsonMessageSerializer` (System.Text.Json `IMessageSerializer`) exists but is not used by the transports.

## Troubleshooting checklist
- Nothing is published → is exactly one drain mode registered? In immediate mode, is `UseImmediateOutboxDraining` in the pipeline and did the response return 2xx? Is `OutboxMessage` mapped in the DbContext?
- Messages stuck with growing `RetryCount` → check `LastError`; circuit breaker may be open (1 min); verify broker creds and that the exchange exists (mandatory publish to a missing exchange closes the channel).
- Consumer receives nothing → queue bound to the exchange with the right routing key? `RabbitMQ:Queue` set? Receiver startup error in logs?
- Everything goes to DLQ → consumer can't `Type.GetType` the event (assembly not referenced / renamed), wrong `EncryptionKey`, or a handler threw.
- Duplicates processed → inbox not registered, `ClientProvidedName` changed, or event Id not preserved (see ⚠ above).

## Changing this package
Keep registrations `TryAdd*` and generic over `TContext`. Transport classes expose `protected virtual` hooks
(`EnsureConnectionAsync`, `ResolveRoute`, `PublishToChannelAsync`, `EnsureQueueExistsAsync`) — extend via those.
`InternalsVisibleTo`: `Pivot.Framework.Infrastructure.Persistence.Tests` (does not exist yet) and `DynamicProxyGenAssembly2`.
Tests for these features live in `Tests/Pivot.Framework.Containers.API.Tests` (`Outbox`, `Inbox`, `Sagas`, `Extensions`).
