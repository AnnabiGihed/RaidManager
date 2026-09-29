---
name: pivot-application
description: Pivot.Framework.Application — CQRS application layer on MediatR. Use when writing commands, queries, handlers, FluentValidation validators, the ValidationPipelineBehavior, domain/integration event handlers, projection handlers, read models and ReadModelSpecification, idempotent commands, saga definitions/steps, correlation/causation/replay context, or application exceptions (NotFound/BadRequest/Validation) — in a consuming service or inside Src/Core/Pivot.Framework.Application.
---

# Pivot.Framework.Application

Location: `Src/Core/Pivot.Framework.Application`. Depends on Domain + MediatR 14, FluentValidation 12
(+ DI extensions), AutoMapper 16, Scrutor, `Microsoft.Extensions.Logging.Abstractions`.
It defines **contracts and behaviours only** — no infrastructure.

All handlers return the framework `Result`/`Result<T>` from `Pivot.Framework.Domain.Shared` (see `pivot-domain`).

## 1. Commands and queries (`Abstractions/Messaging`)

```csharp
public interface ICommand : IRequest<Result>;
public interface ICommand<TResponse> : IRequest<Result<TResponse>>;
public interface ICommandHandler<in TCommand> : IRequestHandler<TCommand, Result> where TCommand : ICommand;
public interface ICommandHandler<in TCommand, TResponse> : IRequestHandler<TCommand, Result<TResponse>> where TCommand : ICommand<TResponse>;
public interface IQuery<TResponse> : IRequest<Result<TResponse>>;
public interface IQueryHandler<in TQuery, TResponse> : IRequestHandler<TQuery, Result<TResponse>> where TQuery : IQuery<TResponse>;
```

Command handler pattern (write side):

```csharp
public sealed record CreateOrderCommand(Guid CustomerId) : ICommand<Guid>;

internal sealed class CreateOrderCommandHandler(
	IOrderRepository orders,                      // your BaseAsyncCommandRepository subclass
	IUnitOfWork<AppDbContext> unitOfWork)          // Pivot.Framework.Infrastructure.Abstraction.UnitOfWork
	: ICommandHandler<CreateOrderCommand, Guid>
{
	public async Task<Result<Guid>> Handle(CreateOrderCommand command, CancellationToken ct)
	{
		var orderResult = Order.Create(new CustomerId(command.CustomerId));
		if (orderResult.IsFailure)
			return Result.Failure<Guid>(orderResult.Error, orderResult.ResultExceptionType);

		await orders.AddAsync(orderResult.Value, ct);

		var save = await unitOfWork.SaveChangesAsync(ct);   // audit + outbox + SaveChanges
		return save.IsFailure
			? Result.Failure<Guid>(save.Error, save.ResultExceptionType)
			: Result.Success(orderResult.Value.Id.Value);
	}
}
```
- Always propagate `ResultExceptionType` when converting failures — it drives the HTTP/gRPC status.
- Return `Result.Failure(..., ResultExceptionType.NotFound)` for missing aggregates rather than throwing.
- Query handlers read from `IReadModelRepository<,>` (or a read `DbContext`), never from command repositories.

## 2. Validation pipeline (`Behaviors/ValidationPipelineBehavior<TRequest,TResponse>`)

- Constraint: `TResponse : Result`. Runs **all** `IValidator<TRequest>` concurrently (`Task.WhenAll`), maps each failure to `new Error(code: PropertyName, message: ErrorMessage)`, de-duplicates (by code+message), and **returns** (does not throw) a `ValidationResult` / `ValidationResult<T>` built via a cached reflection factory. No validators → straight to `next()`.
- Registration:
  ```csharp
  services.AddMediatR(cfg =>
  {
  	cfg.RegisterServicesFromAssembly(Application.AssemblyReference.Assembly);
  	cfg.AddOpenBehavior(typeof(ValidationPipelineBehavior<,>));
  });
  services.AddValidatorsFromAssembly(Application.AssemblyReference.Assembly);
  ```
- Because failures are *returned*, controllers must go through `ApiController.HandleResult/HandleFailure` (→ 400 with `validationErrors`), not rely on exception middleware.

## 3. Idempotent commands

`IIdempotentCommand { Guid IdempotencyKey { get; } }` marks a command for deduplication by
`IdempotentCommandBehavior` (lives in Persistence.EntityFrameworkCore, registered by `services.AddInboxSupport<TContext>().AddIdempotentCommands()` — see `pivot-messaging-outbox`). The consumer name is `typeof(TRequest).Name`; consumption is recorded only when the handler succeeds.
**Limitation:** on a duplicate the behaviour returns `(TResponse)Result.Success()` → `InvalidCastException` for `ICommand<T>`. Use it only on non-generic `ICommand` until fixed.

## 4. Event handlers (`Abstractions/Messaging/Events`)

- MediatR notifications wrap events: `DomainEventNotification<TDomainEvent>` and `IntegrationEventNotification<TIntegrationEvent>` (null-checked `DomainEvent`/`IntegrationEvent` property).
- Handler contracts use a default interface method to adapt MediatR:
  ```csharp
  public interface IDomainEventHandler<TEvent> : INotificationHandler<DomainEventNotification<TEvent>> where TEvent : IDomainEvent
  { Task<Result> HandleWithResultAsync(TEvent domainEvent, CancellationToken ct); }
  ```
  The MediatR path **discards** the returned `Result`. If failure must stop processing (e.g. to get a RabbitMQ nack/redelivery), throw instead of returning a failure.
- Implement via the abstract bases `DomainEventHandlerBase<TEvent>` (`Pivot.Framework.Application.DomainEventHandler`) and `IntegrationEventHandlerBase<TEvent>` (`Pivot.Framework.Application.IntegrationEventHandler`):
  ```csharp
  internal sealed class SendConfirmationOnOrderCreated(IEmailSender email)
  	: DomainEventHandlerBase<OrderCreatedDomainEvent>
  {
  	public override async Task<Result> HandleWithResultAsync(OrderCreatedDomainEvent e, CancellationToken ct)
  	{
  		await email.SendAsync(e.CustomerId, ct);
  		return Result.Success();
  	}
  }
  ```
- Dispatchers: `IDomainEventDispatcher.DispatchAsync(IDomainEvent)` and `IIntegrationEventDispatcher.DispatchAsync(IIntegrationEvent)`; MediatR implementations are registered with `AddDomainEventDispatcher()` / `AddIntegrationEventDispatcher()` (Persistence.EntityFrameworkCore). Handlers are found by MediatR's assembly scan — no manual registration.
- **When do domain-event handlers run?** Not inside `SaveChangesAsync`. Events go to the outbox; handlers run when the outbox is drained — in-process transport (same service) or RabbitMQ receiver (other service). See `pivot-messaging-outbox`.

## 5. Read models (`Abstractions/ReadModels`)

- `IReadModel<TId> { TId Id { get; } }` (TId unconstrained) and the convenience base `ReadModel<TId>` (protected setter, null-checked ctor, parameterless ctor for ORMs).
- `IReadModelRepository<TReadModel, in TId>`: `GetByIdAsync`, `GetAllAsync(predicate?)`, `ExistsAsync(predicate)`, `CountAsync(predicate?)`, `ListAsync(ReadModelSpecification<TReadModel>)`.
- `IReadModelStore<TReadModel, in TId>`: `UpsertAsync(model)`, `DeleteAsync(id)` — used by projections.
- Implementations: EF Core (`AddEfCoreReadModelStore<TContext>()`, `pivot-persistence-efcore`) or MongoDB (`AddMongoReadModelStore(...)`, `pivot-readstore-mongodb`). Both register **open generics**, so you inject `IReadModelRepository<OrderSummary, Guid>` directly.
- `ReadModelSpecification<TReadModel>`:
  ```csharp
  public sealed class PendingOrdersSpec : ReadModelSpecification<OrderSummary>
  {
  	public PendingOrdersSpec(int page, int size) : base(o => o.Status == "Pending")
  	{
  		AddOrderByDescending(o => o.CreatedOnUtc);
  		AddThenBy(o => o.Id);
  		ApplyPaging((page - 1) * size, size);   // skip >= 0, take > 0 else ArgumentOutOfRangeException
  	}
  }
  ```
  `AddOrderBy`/`AddOrderByDescending` are mutually exclusive and clear ThenBy entries. **Note:** the EF and Mongo evaluators currently ignore `ThenByExpressions` — only primary ordering, criteria and paging are applied.

### Projections
- `IProjectionHandler<in TEvent> { Task ProjectAsync(TEvent e, CancellationToken ct); }`.
- `ProjectionHandler<TEvent>` is also a `DomainEventHandlerBase<TEvent>`: its sealed `HandleWithResultAsync` calls `ProjectAsync`, so a projection runs **both** via MediatR domain-event dispatch **and** via `IProjectionDispatcher` (`AddProjectionSupport()`), which resolves `IProjectionHandler<TEvent>` from DI. Pick one path to avoid double projection: MediatR scanning finds it as a notification handler automatically; the dispatcher only finds it if you register it as `IProjectionHandler<TEvent>`.
  ```csharp
  internal sealed class OrderSummaryProjection(IReadModelStore<OrderSummary, Guid> store)
  	: ProjectionHandler<OrderCreatedDomainEvent>
  {
  	public override Task ProjectAsync(OrderCreatedDomainEvent e, CancellationToken ct)
  		=> store.UpsertAsync(new OrderSummary(e.OrderId) { Status = "Pending" }, ct);
  }
  ```
- Projections must be idempotent (upsert, not insert) — redelivery happens.
- Check `ReplayContext.IsReplaying` to skip side effects (emails, external calls) during rebuilds.

## 6. Ambient context (AsyncLocal statics)

| Type | Members | Set by |
|---|---|---|
| `CorrelationContext` | `CorrelationId`, `EnsureCorrelationId()` | `RabbitMQReceiver` (from `CorrelationId` header), `InProcessMessagePublisher`; read by outbox publishers and `RabbitMQPublisher` |
| `CausationContext` | `CausationId` | `RabbitMQReceiver` (incoming EventId); read into `EventEnvelope.CausationId` |
| `ReplayContext` | `IsReplaying`, `BeginReplayScope()` (restores previous value on dispose) | `ProjectionRebuilder`; stamped into `EventHistoryEntry.ReplayFlag` |

For HTTP requests nothing sets `CorrelationContext` automatically — add a small middleware that copies an
`X-Correlation-Id` header (or `EnsureCorrelationId()`) if you need end-to-end correlation.

`ICurrentUserProvider { string GetCurrentUser(); }` supplies the actor for audit stamping; default
`HttpContextCurrentUserProvider` returns `User.Identity.Name` or `"System"`. Replace it for background jobs or
to use the `sub` claim.

## 7. Sagas (`Abstractions/Sagas`)

```csharp
public interface ISagaStep<TData> where TData : class
{
	string StepName { get; }
	Task<Result> ExecuteAsync(TData data, CancellationToken ct);
	Task<Result> CompensateAsync(TData data, CancellationToken ct);
}
public interface ISagaDefinition<TData> where TData : class
{ string SagaType { get; } IReadOnlyList<ISagaStep<TData>> Steps { get; } }
public interface ISagaOrchestrator
{
	Task<Result<Guid>> StartAsync<TData>(ISagaDefinition<TData> def, TData data, string? correlationId = null, CancellationToken ct = default) where TData : class;
	Task<Result> ResumeAsync<TData>(Guid sagaId, ISagaDefinition<TData> def, CancellationToken ct = default) where TData : class;
}
```
Execution semantics and persistence: `pivot-event-store-sagas`.

## 8. Application exceptions (`Exceptions`)

| Exception | Ctor | HTTP (middleware) | gRPC |
|---|---|---|---|
| `NotFoundException` | `(string name, object key)`; `NotFoundException.ToError(name, key)` → `Error("Error.NotFound", …)` | 404 | NotFound |
| `BadRequestException` | `(Error)` or `(Error, IValidationResult)`; `PrimaryError`, `ValidationErrors` | 400 | InvalidArgument (+ trailers) |
| `ValidationException` | same shape as BadRequest | 400 | InvalidArgument (+ trailers) |

These are **sealed** and distinct from FluentValidation's `ValidationException` — watch your `using`s.

`BaseCommandResponse` (`Responses/BadCommandResponse.cs` — file name differs from type) is a legacy
`Ok(message)` / `Fail(message, errors)` DTO; prefer `Result` + `ApiController`.

## Changing this package

- Keep it free of EF Core/ASP.NET dependencies. New pipeline behaviours must keep the `TResponse : Result` constraint and create failures via factories that work for both `Result` and `Result<T>` (see `CreateValidationResult`).
- Tests: `Tests/Pivot.Framework.Application.Tests` (`Behaviors`, `Correlation`, `DomainEventHandler`, `Exceptions`, `Messaging`, `ReadModels`, `Responses`).
