---
name: clean-code-types
description: Clean Code rules for designing C# types, aligned with Pivot.Framework's building blocks (entity/aggregate hierarchy, StronglyTypedGuidId, ValueObject<T>, Result factories, domain exceptions). Use whenever you create, split, refactor or review a class, record, struct, entity, value object, DTO or service, or add constructor dependencies. Covers single responsibility, sealed by default, strongly typed identifiers and value objects, immutability, encapsulated collections and entity state, Law of Demeter, feature envy, narrow dependencies, TimeProvider, constructor injection and testability.
---

# Types, encapsulation and coupling

Source: Development Standards Wiki → *Clean Code* → *Types, Encapsulation, and Coupling*. Cite rules as
`Types, Encapsulation, and Coupling / R<n>`. Severity and markers as in `clean-code-naming`. Which layer or aggregate a
type belongs to is decided by `dotnet-ddd-cqrs-conventions`; the framework types are described in `pivot-domain`.

## 1. Cohesion and size
- 🔴 **R1.** One reason to change: state the responsibility in one sentence without "and"/"or" — that sentence is the `Purpose` of the type header. If you can't, split. *Review.*
- 🔴 **R2.** `Manager`, `Helper`, `Utils`, `Processor` or a vague `Service` is a smell: name the specific responsibility. (Pivot's `OutboxProcessor` is acceptable: "processor" is the pattern's name.) *Review.*
- 🟡 **R3.** Member count and coupling bounded. *Analyzer:* S1448, S1200.
- 🟡 **R4.** Fields used by only some methods hide a second class. *Review.*
- 🟡 **R5.** Types are `sealed` unless designed for inheritance with documented extension points. Pivot designs for inheritance explicitly: abstract bases (`AggregateRoot<TId>`, `UnitOfWork<TContext>`, `ProjectionHandler<T>`) and `public virtual`/`protected virtual` members on repositories, stores and transports. Your own aggregates, handlers, configurations and services are `sealed`. *Analyzer:* CA1852.
- 🟢 **R6.** Prefer composition; never share an abstract base between unrelated types for code reuse. *Review.*

## 2. Primitive obsession and value objects
- 🔴 **R7.** Domain identifiers are never raw `int`/`Guid`/`string` inside the domain: one id type per aggregate —
  `public sealed record OrderId(Guid Value) : StronglyTypedGuidId<OrderId>(Value);` (rejects `Guid.Empty`, `ToString()` returns the Guid — the event store records it as `AggregateId`). A hand-written `IStronglyTypedId<TSelf>` must keep that `ToString()` contract. Map with an EF value converter. *Review.*
- 🔴 **R8.** Values with invariants (money, registry numbers, date ranges, email, percentages) are value objects that validate on creation. Base: `CSharpFunctionalExtensions.ValueObject<T>` (as Pivot's `AuditInfo`/`DomainError`) or a `record`. *Review.*
- 🟡 **R9.** `readonly record struct` for small value objects, `sealed record` or `ValueObject<T>` for larger ones. *Review.*
- 🟡 **R10.** When creation can fail for business reasons, expose `static Result<T> Create(...)` (user input — returns `BaseDomainErrors`/typed `Error`s) and keep the constructor private; add a throwing overload only for trusted input. *Review.*
- 🟡 **R11.** Closed sets are enums or type hierarchies, never magic strings. *Review.*

```csharp
public sealed class EstablishmentNumber : ValueObject<EstablishmentNumber>
{
	#region Constants
	private const int RequiredLength = 10;
	#endregion

	#region Properties
	/// <summary>Gets the ten-digit establishment number.</summary>
	public string Value { get; }
	#endregion

	#region Constructors
	private EstablishmentNumber(string value) => Value = value;
	#endregion

	#region Factory Methods
	/// <summary>Creates an establishment number from user input.</summary>
	/// <param name="value">The raw input.</param>
	/// <returns>The value object, or a <see cref="ResultExceptionType.ValidationError"/> failure.</returns>
	public static Result<EstablishmentNumber> Create(string? value)
	{
		if (string.IsNullOrWhiteSpace(value))
			return Result.Failure<EstablishmentNumber>(BaseDomainErrors.General.ValueIsRequired("establishment number"));

		if (value.Length != RequiredLength || !value.All(char.IsAsciiDigit))
			return Result.Failure<EstablishmentNumber>(BaseDomainErrors.General.ValueIsInvalid("establishment number", value));

		return new EstablishmentNumber(value);
	}
	#endregion

	#region Equality
	/// <inheritdoc />
	protected override bool EqualsCore(EstablishmentNumber other) => Value == other.Value;

	/// <inheritdoc />
	protected override int GetHashCodeCore() => Value.GetHashCode(StringComparison.Ordinal);
	#endregion

	#region Overrides
	/// <inheritdoc />
	public override string ToString() => Value;
	#endregion
}
```
(Type header omitted for brevity — required in real code, see `csharp-xml-documentation`.)

## 3. Immutability
- 🔴 **R12.** No public fields. *Analyzer:* S1104, CA1051.
- 🔴 **R13.** No mutable `public static` state. *Analyzer:* S2223, S2386, CA2211.
- 🔴 **R14.** Value objects and domain/integration events are immutable (`DomainEvent`/`IntegrationEvent` are records). *Review.*
- 🟡 **R15.** DTOs, commands, queries and options are `record`s with `init`; mutability opt-in with a reason. (Read models may use public setters — they are persistence documents rebuilt by projections.) *Review.*
- 🟡 **R16.** Entities expose `{ get; private set; }` and change state only through named domain methods; aggregates return `Result` from behaviours and call `RaiseDomainEvent(...)`. Audit and soft-delete state are managed by the base classes and `UnitOfWork` — never set them from handlers. *Review.*

## 4. Encapsulation
- 🔴 **R17.** Entity-owned collections: `IReadOnlyCollection<T>` over a private backing field with named mutation methods (as `GetDomainEvents()` over `_domainEvents`). *Analyzer:* CA1002, MA0016.
- 🔴 **R18.** Never expose an internal mutable object (backing list, `DbContext`, dictionary). *Review.*
- 🟡 **R19.** A constructor leaves the object valid; no `Initialize()` second phase. The only extra constructor is the parameterless one reserved for EF Core materialisation. *Review.*
- 🟡 **R20.** Constructors only assign and validate. *Review.*

```csharp
public sealed class Dossier : AggregateRoot<DossierId>
{
	#region Fields
	private readonly List<Inspection> _inspections = [];
	#endregion

	#region Properties
	/// <summary>Gets the lifecycle status of the dossier.</summary>
	public DossierStatus Status { get; private set; }

	/// <summary>Gets the inspections planned for this dossier, in planning order.</summary>
	public IReadOnlyCollection<Inspection> Inspections => _inspections.AsReadOnly();
	#endregion

	#region Domain Behaviours
	/// <summary>Plans an inspection; only allowed while the dossier is under review.</summary>
	/// <returns>Success, or a <see cref="ResultExceptionType.Conflict"/> failure when the status forbids planning.</returns>
	public Result PlanInspection(InspectorId inspector, DateOnly on)
	{
		if (Status is not DossierStatus.UnderReview)
			return Result.Failure(DossierErrors.InspectionNotAllowed(Status), ResultExceptionType.Conflict);

		_inspections.Add(Inspection.Plan(inspector, on));
		Status = DossierStatus.InspectionPlanned;
		RaiseDomainEvent(new InspectionPlannedDomainEvent(Id.Value, inspector.Value, on));
		return Result.Success();
	}
	#endregion
}
```

## 5. Coupling
- 🔴 **R21.** Law of Demeter: at most one hop into other objects. *Review.*
- 🔴 **R22.** Feature envy: move behaviour to the data; avoid anemic entities with fat handlers. *Review.*
- 🟡 **R23.** Depend on the narrowest abstraction: `IAsyncCommandRepository`-derived per-aggregate interfaces, `IReadModelRepository<T,TId>`, `IOptions<T>`, `IUnitOfWork<TContext>` — never `IServiceProvider`, a whole `IConfiguration`, or a `DbContext` in handlers. (Framework dispatchers that resolve open generics by reflection — `ProjectionDispatcher`, `IntegrationEventMappingCoordinator` — are the documented exception.) *Review.*
- 🟡 **R24.** No static infrastructure access in business code: `DateTime.Now/UtcNow`, `Guid.NewGuid()`, `Random`, `Environment.*`, `File.*`, `HttpContext`. Inject `TimeProvider`/generators/options; use `ICurrentUser` (not `HttpContext.User`) for the caller. *Analyzer:* MA0132.

## 6. Testability
- 🔴 **R25.** Every dependency arrives through the constructor (primary constructors are fine). No service location or `new` of infrastructure in domain code. *Review.*
- 🔴 **R26.** A type with business rules can be constructed in a unit test without a database, HTTP client, broker or configuration file. *Review.*
- 🟡 **R27.** Time as `TimeProvider` (tests: `FakeTimeProvider`), never a custom `IClock`. *Review.*
- 🟡 **R28.** A handful of constructor dependencies at most. *Analyzer:* S107.

## 7. Checklist
- [ ] Each type's responsibility fits in one sentence (its Purpose); no vague Manager/Helper names.
- [ ] Your types sealed; inheritance only from Pivot's designed bases.
- [ ] Identifiers are `StronglyTypedGuidId` records; invariant-bearing values are value objects with `Result<T> Create`.
- [ ] No public fields or mutable statics; DTOs/commands immutable; entities change only via behaviours returning `Result`.
- [ ] Collections exposed read-only; no chains beyond one hop.
- [ ] Dependencies via constructor, narrow and few; time via `TimeProvider`; unit-testable without infrastructure.
