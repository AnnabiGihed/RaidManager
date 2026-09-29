---
name: pivot-domain
description: Pivot.Framework.Domain — DDD building blocks. Use when writing or changing entities, aggregate roots, strongly-typed IDs, AuditInfo/soft delete, domain events, integration events, the framework Result/Result<T>/Error/ValidationResult types, DomainError/BaseDomainErrors, domain exceptions and DomainExceptionScope, IAsyncCommandRepository/IUnitOfWork contracts, or saga state enums — either in a consuming service's domain layer or inside Src/Core/Pivot.Framework.Domain itself.
---

# Pivot.Framework.Domain

Location: `Src/Core/Pivot.Framework.Domain`. Only dependency: `CSharpFunctionalExtensions` (used solely for
`ValueObject<T>` as base of `AuditInfo` and `DomainError`). **Never add infrastructure or MediatR references here.**

Namespaces: `Pivot.Framework.Domain.Primitives`, `.DomainEvents`, `.IntegrationEvents`, `.Errors`,
`.Exceptions`, `.Shared`, `.Repositories`, `.Sagas`. Localised messages come from `Resource.resx`
(`Resource.RecordNotFound`, `Resource.Required`, …).

## 1. Entity hierarchy

All generic over `TId : IStronglyTypedId<TId>`.

| Class | Adds | Interfaces |
|---|---|---|
| `Entity<TId>` | `Id { get; protected init; }`, identity equality (same runtime type **and** same Id), `==`/`!=` | `IEquatable<Entity<TId>>` |
| `AuditableEntity<TId>` | `AuditInfo? Audit`, `InitializeAudit(utc, by)`, `Touch(utc, by)`, `SetAudit(audit)` | `IAuditableEntity` |
| `FullEntity<TId>` | `IsDeleted`, `DeletedBy`, `DeletedOnUtc`, `SoftDelete(utc, by)`, `Restore(utc, by)` | `ISoftDeletableEntity` |
| `LightweightAggregateRoot<TId>` : `Entity` | domain events + `Version` | `IAggregateRoot` |
| `AuditableAggregateRoot<TId>` : `AuditableEntity` | domain events + `Version` | `IAggregateRoot` |
| `AggregateRoot<TId>` : `FullEntity` | domain events + `Version` + `Delete(utc, by)` / `RestoreDeleted(utc, by)` wrappers | `IAggregateRoot` |

Rules that the code enforces:
- `Entity(TId id)` throws on null id. The protected parameterless constructor exists **only** for EF Core materialisation — keep one (can be `private`/`protected`) on every entity you create.
- `RaiseDomainEvent(IDomainEvent)` is `protected`, null-checked, **increments `Version`** and appends to the list. `GetDomainEvents()` is public read-only; `ClearDomainEvents()` is an explicit interface member (`((IAggregateRoot)agg).ClearDomainEvents()`), called by `UnitOfWork` after a successful save.
- `Version` is therefore "number of events raised since the aggregate was loaded + stored version"; the outbox publisher derives per-event `AggregateVersion` from it (see `pivot-persistence-efcore`). Don't set `Version` manually unless rehydrating.
- `SoftDelete`/`Restore` are idempotent (no-op if already in that state), require `DateTimeKind.Utc`, non-min/max dates and a non-whitespace actor, and also `Modify` the `Audit` if present.
- `IAuditableEntity.SetAudit` and `ISoftDeletableEntity.MarkDeleted/MarkRestored` are explicit interface implementations used by infrastructure.

### AuditInfo (value object)
`CreatedBy`, `CreatedOnUtc`, `ModifiedBy`, `ModifiedOnUtc`. Immutable: `AuditInfo.Create(utc, author)`,
`audit.Modify(utc, author)` returns a new instance, `audit.Update(createdBy, modifiedBy, createdOn, modifiedOn)`.
Validates UTC kind, rejects `DateTime.MinValue/MaxValue`, rejects blank authors. Has a private
`[JsonConstructor]` so it round-trips through System.Text.Json. EF maps it as an owned type with columns
`Audit_CreatedOnUtc`, `Audit_CreatedBy`, `Audit_ModifiedOnUtc`, `Audit_ModifiedBy` (done by `ApplyDomainPrimitives`).
Normally **you never stamp audit yourself** — `UnitOfWork.SaveChangesAsync` does it via `ICurrentUserProvider`.

## 2. Strongly-typed IDs

```csharp
public sealed record OrderId(Guid Value) : StronglyTypedGuidId<OrderId>(Value)
{
	public static OrderId New() => new(Guid.NewGuid());
}
```
- `StronglyTypedGuidId<TSelf>` rejects `Guid.Empty`, equality/hash on `Value`, `CompareTo`, `ToString()` → the Guid string (the outbox uses `Id.ToString()` as `AggregateId`).
- For non-Guid keys implement `IStronglyTypedId<TSelf>` (`IComparable<TSelf>`, `IEquatable<TSelf>`) yourself.
- EF Core needs a value converter per ID type (e.g. `builder.Property(x => x.Id).HasConversion(id => id.Value, v => new OrderId(v))`); `ApplyDomainPrimitives` does **not** add ID converters.

## 3. Aggregate example (idiomatic)

```csharp
public sealed class Order : AggregateRoot<OrderId>
{
	#region Properties
	public CustomerId CustomerId { get; private set; } = default!;
	public OrderStatus Status { get; private set; }
	#endregion

	#region Constructors
	private Order() { }                          // EF Core
	private Order(OrderId id, CustomerId customerId) : base(id) => CustomerId = customerId;
	#endregion

	#region Factory
	public static Result<Order> Create(CustomerId customerId)
	{
		if (customerId is null)
			return Result.Failure<Order>(BaseDomainErrors.General.ValueIsRequired(nameof(CustomerId)));

		var order = new Order(OrderId.New(), customerId);
		order.RaiseDomainEvent(new OrderCreatedDomainEvent(order.Id.Value, customerId.Value));
		return order;                            // implicit Result<Order>
	}
	#endregion

	#region Domain Behaviours
	public Result Ship()
	{
		if (Status != OrderStatus.Paid)
			return Result.Failure(new Error("Order.Status.NotPaid", "Order must be paid before shipping."));
		Status = OrderStatus.Shipped;
		RaiseDomainEvent(new OrderShippedDomainEvent(Id.Value));
		return Result.Success();
	}

	public void Cancel(string actor) => Delete(DateTime.UtcNow, actor);   // soft delete
	#endregion
}
```

## 4. Domain events and integration events

```csharp
public interface IDomainEvent      { Guid Id { get; } DateTime OccurredOnUtc { get; } }
public interface IIntegrationEvent { Guid Id { get; } DateTime OccurredOnUtc { get; } string? CorrelationId { get; } }
```
- Base records: `DomainEvent` (`Pivot.Framework.Domain.DomainEvents`) and `IntegrationEvent` (`Pivot.Framework.Domain.IntegrationEvents`). Parameterless ctor = `Guid.NewGuid()` + `DateTime.UtcNow`; the explicit ctor validates non-empty id and UTC kind. `IntegrationEvent.CorrelationId` is `init`-able.
- Payload properties should be **primitive / serializable** (Guid, string, decimal…) rather than strongly-typed IDs or entities — events are serialized with Newtonsoft (`TypeNameHandling.None`) into the outbox and resolved later via `Type.GetType(AssemblyQualifiedName)`. Renaming/moving an event type breaks deserialization of stored messages; use event versioning (`pivot-event-store-sagas`) instead.
- **Id preservation defect (verified):** `Id`/`OccurredOnUtc` are get-only, so a Newtonsoft round-trip produces a *new* Id. Inbox dedup on the consumer side depends on that Id. Until the base types are fixed, give events that cross a transport a constructor Newtonsoft will use:
  ```csharp
  public sealed record OrderShippedDomainEvent : DomainEvent
  {
  	public Guid OrderId { get; }
  	public OrderShippedDomainEvent(Guid orderId) => OrderId = orderId;

  	[Newtonsoft.Json.JsonConstructor]
  	private OrderShippedDomainEvent(Guid id, DateTime occurredOnUtc, Guid orderId)
  		: base(id, DateTime.SpecifyKind(occurredOnUtc, DateTimeKind.Utc)) => OrderId = orderId;
  }
  ```
  The proper framework fix is in `DomainEvent`/`IntegrationEvent` (e.g. `[JsonProperty]`-settable backing or `init` setters with validation), plus a round-trip test in `Tests/Pivot.Framework.Domain.Tests/IntegrationEvents`.
- `OutboxMessageFailedEvent` (integration event) is emitted by the outbox when a message is dead-lettered: `FailedMessageId`, `OriginalEventType`, `RetryCount`, `LastError`.

## 5. Result, Error, ValidationResult (`Pivot.Framework.Domain.Shared`)

```csharp
Result.Success();                          Result.Success(value);             // Result<T>
Result.Failure(error, ResultExceptionType.NotFound);
Result.Failure<T>(error);                  // default type = ValidationError
Result.Create(value);                      // null → Failure(Error.NullValue)
Result<Order> r = order;                   // implicit conversion (null → failure)
```
- Invariants: success cannot carry an error; failure must carry an error other than `Error.None`; successful `Result<T>` must have a non-null value; reading `.Value` of a failure throws.
- `ResultExceptionType`: `None`, `ValidationError` (default for failures), `NotFound`, `Conflict`, `AuthenticationRequired`, `AccessDenied`. **This drives HTTP and gRPC status mapping** (`ApiController.HandleFailure` → 400/404/409/401/403; gRPC → InvalidArgument/NotFound/AlreadyExists/Unauthenticated/PermissionDenied). Always pick the right type.
- `Error(code, message)`: equality on code **and** message; implicit `string` conversion yields the code; `Error.None`, `Error.NullValue`, `Error.SystemError(msg)`, `Error.InvalidValue(msg)`.
- Extensions: `result.Ensure(predicate, error, type)`, `result.Map(f)` (propagates failure + type), `result.SafeMap(f, errorFactory?, type)` (catches exceptions → `Error.SystemError`).
- `ValidationResult` / `ValidationResult<T>`: failure with `Error = ValidationErrors.ValidationError` and an `Errors` collection; created with `WithErrors(...)` (requires ≥1 non-None error). Produced by the application `ValidationPipelineBehavior`; `ApiController` puts `Errors` into `ProblemDetails.Extensions["validationErrors"]`.

## 6. DomainError and BaseDomainErrors

- `DomainError(code, message)` — value object, equality by **code only**. `Serialize()` → `"code||message"`; `DomainError.Deserialize(s)` returns an `Error` (splits on the first `||`, throws `FormatException` otherwise; has a legacy special-case for `Resource.ANoneEmptyRequestBodyIsRequired`).
- Label helpers `DomainError.ForField(name)` and `DomainError.ValueForField(name, value)` build the localised fragments.
- Code convention: `ClassName.PropertyName.Reason` (e.g. `Order.Status.Invalid`).
- `BaseDomainErrors.General` factories return `Error`: `NotFound(id?)`, `ValueIsInvalid`, `ValueAlreadyExists`, `ValueIsTooLong(name, value, max)`, `ValueIsTooShort`, `ValueIsRequired`, `ValueNotNegative`, `InvalidLength`, `CollectionIsTooSmall(min, current)`, `CollectionIsTooLarge`, `UnexpectedError(message)`. Each has an overload `(…, codeDescriptor, propertyDescriptor)` to supply your own code and format string (validated non-blank). `ValueIsToLong/ToShort` and `BaseDomainErrors.ValueObjects.Money` are `[Obsolete]` — don't use them in new code.
- Define service-specific errors in the consuming domain:
  ```csharp
  public static class OrderErrors
  {
  	public static readonly Error NotFound = new("Order.NotFound", "The order was not found.");
  }
  ```

## 7. Domain exceptions (`Pivot.Framework.Domain.Exceptions`)

Use for invariant violations that are truly exceptional; prefer `Result` for expected business failures.
- `DomainException` is **abstract**: `(string parameterName, string message[, Exception inner])`; exposes `ParameterName`.
- Concrete: `RequiredDomainException`, `AlreadyExistsDomainException`, `NotExistsDomainException`, `OutOfRangeDomainException`, `AtLeastOneIsRequiredDomainException`, `UnknownDomainException` — each has `(parameterName)` (localised default message) and `(parameterName, message)`.
- `AggregateDomainException(IEnumerable<DomainException>)` : `AggregateException`, exposes `DomainExceptions`.
- Collect several violations then throw once:
  ```csharp
  using var scope = new DomainExceptionScope();
  if (string.IsNullOrWhiteSpace(name)) scope.AddException(new RequiredDomainException(nameof(name)));
  if (qty <= 0) scope.AddException(new OutOfRangeDomainException(nameof(qty)));
  scope.ThrowIfAny();   // throws AggregateDomainException, clears the list
  ```
- Note: `ExceptionHandlerMiddleware` does **not** special-case domain exceptions — they become HTTP 500. Map them yourself or return `Result`.

## 8. Repository / UoW contracts

```csharp
public interface IAsyncCommandRepository<TEntity, TId>
	where TEntity : Entity<TId>, IAggregateRoot where TId : IStronglyTypedId<TId>
{
	Task<TEntity?> FindByIdAsync(TId id, CancellationToken ct = default);
	Task AddAsync(TEntity entity, CancellationToken ct = default);
	Task UpdateAsync(TEntity entity, CancellationToken ct = default);
	Task DeleteAsync(TEntity entity, CancellationToken ct = default);
	Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken ct = default);
}
public interface IUnitOfWork { Task<Result> SaveChangesAsync(CancellationToken ct = default); }
```
Command repositories are for aggregate roots only. Query-side reads go through read models (`pivot-application`).

## 9. Sagas enums
`SagaState`: NotStarted, InProgress, Completed, Compensating, Compensated, Failed.
`SagaStepStatus`: Pending, Executing, Completed, Compensating, Compensated, Failed. Used by `pivot-event-store-sagas`.

## Changing this package

- Keep it dependency-free; any new primitive must be persistence-agnostic.
- Guard every public ctor/factory; validate UTC dates the same way `AuditInfo` does.
- Changing event base types, `Result` invariants or `ResultExceptionType` values is a breaking change across every package (HTTP/gRPC mapping, outbox serialization) — search usages in `Src/Containers` and `Src/Infrastructure` and update tests in `Tests/Pivot.Framework.Domain.Tests` (folders `Primitives`, `Shared`, `Errors`, `Exceptions`, `IntegrationEvents`, `Sagas`, with test doubles in `TestDoubles`).
- New localised messages go into `Resource.resx` (and its designer).
