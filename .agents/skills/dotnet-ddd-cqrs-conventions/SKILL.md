---
name: dotnet-ddd-cqrs-conventions
description: Scaffold and wire features in the house DDD + CQRS conventions on Pivot.Framework — feature-folder (vertical slice) layout in every layer, aggregates on Pivot's entity hierarchy, strongly typed ids, commands/queries/handlers on Pivot's ICommand/IQuery abstractions over MediatR, validation and idempotency behaviours, domain/integration events through the transactional outbox, read-model projections, repositories on IAsyncCommandRepository, and the single transaction owner. Use when adding or restructuring a feature, wiring commands/queries/handlers, or dispatching use cases — not for general .NET idioms.
---

Use this skill to add or restructure a feature so it follows the house DDD/CQRS conventions as implemented by
**Pivot.Framework** (the foundation every layer builds on — see `pivot-framework` for the package map). For idiomatic
C# choices use `dotnet-best-practices`; for audits use `dotnet-code-review`. This skill is about *modelling
discipline and wiring*.

## Rules

- Match the existing project structure and base types exactly. Before scaffolding, find a comparable aggregate/command/handler and mirror its layout, naming and namespaces.
- If a house convention isn't visible in the code, **verify it against the Pivot.Framework source or the `pivot-*` skills** — never guess at a base class, extension method or pipeline wiring (several README examples are known to be wrong; `pivot-framework` lists them).
- Write/read split: commands mutate and return `Result` or `Result<TId>`; queries never mutate and never touch the write model. Never both in one handler.
- All invariants live inside the aggregate. No public setters on aggregate state; behaviours validate, mutate, `RaiseDomainEvent(...)`, and return `Result`. Handlers never mutate aggregate internals.
- Domain events are past-tense facts (`PaymentRegisteredDomainEvent`), immutable records on `DomainEvent`, carrying the minimum **primitive, serializable** data (Guid/string/decimal — not id records or entities; they are JSON-serialized into the outbox). Integration events (`IntegrationEvent`) are the cross-service contracts, versioned, living in `{Solution}.IntegrationEvents`.
- **Aggregates are state-stored, with an event history.** Pivot persists aggregate state with EF Core and, when enabled, appends every domain event to `EventHistory` in the same transaction (see `pivot-event-store-sagas`). There is no `Apply`/`When` rehydration. Proposing full event sourcing is a deviation that needs an ADR.
- One repository per aggregate root: the interface extends `IAsyncCommandRepository<TAggregate, TId>` and is declared in Domain; the implementation derives from `BaseAsyncCommandRepository<TAggregate, TId>` in Persistence.
- **Persistence is SQL Server via EF Core** (`Pivot.Framework.Infrastructure.Persistence.EntityFrameworkCore`) unless the work item says otherwise; PostgreSQL is the supported alternative (`…Persistence.PostgreSQL`). In-memory providers are forbidden in application code, configuration and application tests; unit tests mock repository interfaces, integration tests use a real database in a container. (The Pivot.Framework repository's own technical tests use EF InMemory — that exemption does not extend to services.)
- Read models are separate, denormalized per query, eventually consistent, and rebuildable (projections are idempotent upserts).
- Validate shape at the command boundary (FluentValidation validator per command, run by `ValidationPipelineBehavior`); enforce business rules inside the aggregate. Don't duplicate domain rules in handlers or validators.
- Every new command, event handler, projection, mapper and repository is wired into DI in the same place as the existing ones — no orphans.

## Feature-folder layout in every layer (mandatory)

Every project is organized as vertical slices under `Features/`, with `Features/Shared/` for genuinely cross-feature pieces. Never flat `Commands/`, `Handlers/`, `Entities/`, `Dtos/` folders at project root.

```text
{Solution}.Domain/Features/<Feature>/
├── Aggregates/        ← aggregate roots (AggregateRoot<TId> etc.), child entities, <Aggregate>Id records
├── ValueObjects/      ← ValueObject<T> / records with Result<T> Create
├── Events/            ← <Aggregate><Verb>DomainEvent records
├── Errors/            ← <Aggregate>Errors (static readonly Error)
└── Repositories/      ← I<Aggregate>Repository : IAsyncCommandRepository<TAggregate, TId>

{Solution}.Application/Features/<Feature>/
├── Commands/<UseCase>/
│   ├── <UseCase>Command.cs            ← ICommand / ICommand<TResponse>
│   ├── <UseCase>CommandHandler.cs     ← ICommandHandler<,>
│   ├── <UseCase>CommandValidator.cs   ← AbstractValidator<<UseCase>Command>
│   └── <UseCase>Response.cs           (when the command returns more than an id)
├── Queries/<UseCase>/                 ← <UseCase>Query (IQuery<T>), QueryHandler, QueryValidator, Response
├── EventHandlers/                     ← DomainEventHandlerBase<T>, IntegrationEventHandlerBase<T>
├── Projections/                       ← ProjectionHandler<TEvent> + read models (IReadModel<TId>)
└── Sagas/                             ← ISagaDefinition<T>, ISagaStep<T>, saga data (when needed)

{Solution}.Persistence.EntityFrameworkCore/
├── {Solution}DbContext.cs, {Solution}UnitOfWork.cs    ← PivotDbContextBase, UnitOfWork<TContext>
└── Features/<Feature>/
    ├── Configurations/                ← IEntityTypeConfiguration<T> (id converters, Version column)
    └── Repositories/                  ← BaseAsyncCommandRepository implementations

{Solution}.IntegrationEvents/Features/<Feature>/   ← IntegrationEvent contracts (+ IIntegrationEventMapper<T> in Infrastructure/Persistence)
{Solution}.Infrastructure/Features/<Feature>/      ← external clients, routing resolvers, feature-specific infra

{Solution}.ViewModels/Features/<Feature>/          ← view models, their API client interfaces and transport records

{Solution}.Web/                                    ← the Blazor host
├── _Imports.razor                     ← at the project root, so it covers every feature folder
├── Program.cs
├── Components/                        ← host root only: App.razor, Routes.razor (+ code-behinds)
└── Features/
    ├── <Feature>/
    │   ├── Pages/                     ← routable pages (@page): <Page>.razor + <Page>.razor.cs
    │   ├── Components/                ← components only this feature uses
    │   └── <Feature>Routes.cs, *ApiClient.cs, endpoints, DI extensions
    └── Shared/
        ├── Layout/                    ← MainLayout, the app shell
        └── Components/                ← components several features use
```
- One use case = one folder with Command/Query + Handler + Validator + Response, nothing else.
- **The UI follows the same slices, and this is not optional.** A page, its feature components, its routes and its API
  client live in `Features/<Feature>/` of the Web project, next to the feature's view models in
  `{Solution}.ViewModels/Features/<Feature>/`. A flat `Pages/` or `Components/Pages/` folder holding feature pages, or a
  page outside its feature, is a defect; move it in the same PR that touches it.
- Test projects mirror the same `Features/<Feature>/…` tree (see `dotnet-unit-tests`).

## Mediator dispatch is mandatory

- Every use case is a MediatR request through **Pivot's abstractions** (`Pivot.Framework.Application.Abstractions.Messaging`): `ICommand`/`ICommand<TResponse>` (→ `IRequest<Result>`/`IRequest<Result<T>>`), `IQuery<TResponse>`, `ICommandHandler<,>`, `IQueryHandler<,>`. Never raw `IRequest`/`IRequestHandler`.
- **Callers only send.** Controllers derive from `ApiController` and do `HandleResult(await Sender.Send(command, ct))`; minimal APIs, gRPC services, jobs and view models inject `ISender`. Nothing news up a handler, calls a repository or touches an aggregate outside a handler.
- **Cross-cutting behaviours, registered once:** `ValidationPipelineBehavior<,>` (Pivot, returns `ValidationResult` — don't write another) and, where commands can be retried, `IdempotentCommandBehavior<,>` via `AddInboxSupport<T>().AddIdempotentCommands()` (only for non-generic `ICommand` — known limitation). Logging behaviours are allowed; a transaction behaviour is **not** (see below).
- **Domain event dispatch** goes through the outbox, never synchronously inside `SaveChangesAsync`: events are written by `UnitOfWork` and dispatched later by the configured drain + transport to `IDomainEventHandler<T>` via MediatR notifications (`DomainEventNotification<T>`). One mechanism per service.
- Domain never references MediatR or Pivot.Application; the mediator lives in Application and outward.
- Composition root registers MediatR (assembly scan of Application) + behaviours + validators once:
  ```csharp
  services.AddMediatR(cfg =>
  {
  	cfg.RegisterServicesFromAssembly(Application.AssemblyReference.Assembly);
  	cfg.AddOpenBehavior(typeof(ValidationPipelineBehavior<,>));
  });
  services.AddValidatorsFromAssembly(Application.AssemblyReference.Assembly);
  ```

## Pivot building blocks per artefact

| Artefact | Pivot type / call |
|---|---|
| Aggregate | `AggregateRoot<TId>` (audit + soft delete + events), `AuditableAggregateRoot<TId>`, `LightweightAggregateRoot<TId>`; private EF ctor; `static Result<T> Create(...)` |
| Id | `sealed record XId(Guid Value) : StronglyTypedGuidId<XId>(Value)` + EF value converter |
| Value object | `ValueObject<T>` / record; `Result<T> Create` |
| Errors | `Error`, `BaseDomainErrors.General.*`, `ResultExceptionType` |
| Repository | `IAsyncCommandRepository<T,TId>` / `BaseAsyncCommandRepository<T,TId>`; `FindByIdAsync`, `AddAsync`, `UpdateAsync`, `DeleteAsync`, `ExistsAsync` |
| Commit | `IUnitOfWork<TContext>.SaveChangesAsync` → audit stamping + outbox (+ event history, + mapped integration events) + save, returns `Result` |
| Query side | `IReadModelRepository<T,TId>` + `ReadModelSpecification<T>` (EF: `AddEfCoreReadModelStore<T>()`, Mongo: `AddMongoReadModelStore(...)`) |
| Projection | `ProjectionHandler<TEvent>` + `IReadModelStore<T,TId>.UpsertAsync` |
| Integration event | `IntegrationEvent` + `IIntegrationEventMapper<TDomainEvent>` (`AddIntegrationEventMapping<T>()`) or `IIntegrationEventPublisher<T>` |
| Consumer | `IntegrationEventHandlerBase<T>` via RabbitMQ receiver + inbox |
| Long-running process | `ISagaOrchestrator` + `ISagaDefinition<T>` (`AddSagaSupport<T>()`) |

## Event, messaging and transaction discipline

- **Layer purity.** No web framework, ORM or mediator in Domain.
- **Strongly typed ids** per aggregate; never reuse one aggregate's id type for another.
- **Validation at the boundary** through the single validation behaviour.
- **Idempotency & inbox.** Anything delivered at least once is deduplicated: `AddInboxSupport<T>()` (consumer side, keyed by event id + `RabbitMQ:ClientProvidedName`), `IIdempotentCommand` for retried commands. Until the framework fixes event-id round-tripping, events that cross the transport declare a `[JsonConstructor]` forwarding `id`/`occurredOnUtc` (see `pivot-domain` §4).
- **One transaction-boundary owner:** `TransactionMiddleware<TContext>` for HTTP (non-GET; commits on 2xx and 422) or `GrpcTransactionInterceptor<TContext>` for gRPC. The unit of work saves inside that ambient transaction; handlers and behaviours never open transactions.
- **Pipeline order is load-bearing:** `ExceptionHandlerMiddleware` first; `UseImmediateOutboxDraining` *outside* `TransactionMiddleware` (drain after commit). Don't "simplify" an order you don't understand.
- **Outbox, exactly one drain mode** (`AddOutboxDraining<T>` throws on a second call) and **one transport** (`AddRabbitMQPublisher` or `AddInProcessMessagePublisher`). In-process cannot deliver integration events.
- **Replay-safe projections:** idempotent upserts, no side effects when `ReplayContext.IsReplaying`.
- **Event versioning:** evolve schemas with `IEventUpgrader<T>` + `EventVersionRegistry`; never shrink stored metadata (id, type, version, correlation/causation, aggregate id/version, timestamps are all kept by `EventHistoryEntry`). Persist the aggregate `Version` column so event-history versions stay unique.
- **Single entry point:** configure subsystems through their public registration methods (`AddKeycloakAuthentication`, `AddEfCoreWritePersistence`, `AddOutboxDraining`, `AddRabbitMQPublisher`, …). Internal helpers (`AddKeycloakBackend`) and README-only helpers (`InstallServices`, `UseExceptionHandling`) don't exist publicly — check `pivot-framework`'s drift table.

## Workflow

1. **Locate the pattern to mirror** — the closest existing feature.
2. **Model in the domain first** — which aggregate owns the invariant; new behaviour or new aggregate; name the domain event(s) and errors.
3. **Write path** — Command → Validator → Handler → aggregate behaviour (`Result`) → repository → `IUnitOfWork<T>.SaveChangesAsync` (outbox). Handler stays thin; propagate `ResultExceptionType`.
4. **Read path** — which projection(s) update on the new event(s), or a new read model + query.
5. **Integration** — does another service need to know? Add an `IntegrationEvent` + mapper, routing and topology (`pivot-messaging-outbox`).
6. **Wire it** — repository registration, mappers, projection handlers (MediatR picks up handlers by scan), EF configuration incl. id converters; nothing orphaned.
7. **Tests** — aggregate scenarios (Gherkin), handler tests with mocked repositories, where they live (`dotnet-unit-tests`).

## Output format

1. **Plan** — 2–4 sentences: aggregate, behaviour, events, projections, the feature mirrored.
2. **Files** — grouped by layer: path + role + minimal code on Pivot base types.
3. **Wiring** — exact DI/pipeline registrations and where.
4. **Read-side impact** — projections per event, or the new read model.
5. **Open questions / assumptions.**
6. **Next steps** — including where tests go.

## Example

> **Plan:** add `RegisterPayment` to the `Dossier` aggregate; raises `PaymentRegisteredDomainEvent`; the dossier-payments projection upserts its payment list. Mirrors the existing dossier-closing flow.
> ```csharp
> // Domain — Features/Dossiers/Aggregates/Dossier.cs (behaviour, not a setter)
> public Result<PaymentId> RegisterPayment(Money amount, DateTime registeredOnUtc)
> {
> 	if (Status == DossierStatus.Closed)
> 		return Result.Failure<PaymentId>(DossierErrors.Closed, ResultExceptionType.Conflict);
>
> 	var payment = Payment.Register(PaymentId.New(), amount, registeredOnUtc);
> 	_payments.Add(payment);
> 	RaiseDomainEvent(new PaymentRegisteredDomainEvent(Id.Value, payment.Id.Value, amount.Amount, amount.Currency, registeredOnUtc));
> 	return payment.Id;
> }
>
> // Domain — Features/Dossiers/Events/PaymentRegisteredDomainEvent.cs (past tense, primitives)
> public sealed record PaymentRegisteredDomainEvent(Guid DossierId, Guid PaymentId, decimal Amount, string Currency, DateTime RegisteredOnUtc) : DomainEvent;
>
> // Application — Features/Dossiers/Commands/RegisterPayment/RegisterPaymentCommand.cs
> public sealed record RegisterPaymentCommand(Guid DossierId, decimal Amount, string Currency) : ICommand<Guid>;
>
> // Application — …/RegisterPaymentCommandHandler.cs (thin: load → behave → commit)
> internal sealed class RegisterPaymentCommandHandler(IDossierRepository dossiers, IUnitOfWork<DossiersDbContext> unitOfWork, TimeProvider time)
> 	: ICommandHandler<RegisterPaymentCommand, Guid>
> {
> 	public async Task<Result<Guid>> Handle(RegisterPaymentCommand command, CancellationToken ct)
> 	{
> 		var dossier = await dossiers.FindByIdAsync(new DossierId(command.DossierId), ct);
> 		if (dossier is null)
> 			return Result.Failure<Guid>(DossierErrors.NotFound, ResultExceptionType.NotFound);
>
> 		var money = Money.Create(command.Amount, command.Currency);
> 		if (money.IsFailure)
> 			return Result.Failure<Guid>(money.Error, money.ResultExceptionType);
>
> 		var paymentId = dossier.RegisterPayment(money.Value, time.GetUtcNow().UtcDateTime);
> 		if (paymentId.IsFailure)
> 			return Result.Failure<Guid>(paymentId.Error, paymentId.ResultExceptionType);
>
> 		var saved = await unitOfWork.SaveChangesAsync(ct);   // audit + outbox (+ event history) in one transaction
> 		return saved.IsFailure ? Result.Failure<Guid>(saved.Error, saved.ResultExceptionType) : paymentId.Value.Value;
> 	}
> }
>
> // API — Features/Dossiers/DossiersController.cs (only sends)
> [HttpPost("{id:guid}/payments")]
> public async Task<IActionResult> RegisterPayment(Guid id, RegisterPaymentRequest body, CancellationToken ct)
> 	=> HandleResult(await Sender.Send(new RegisterPaymentCommand(id, body.Amount, body.Currency), ct));
> ```
> **Read-side impact:** `DossierPaymentsProjection : ProjectionHandler<PaymentRegisteredDomainEvent>` loads the read model and upserts the full list (rebuildable from `EventHistory`) rather than patching a partial one.

(Type headers and regions omitted for brevity — required in real code: `csharp-xml-documentation`, `csharp-regions`.)

## Completion criteria

- Output mirrors an existing feature inside `Features/<Feature>/…` in every layer; one folder per use case.
- Aggregates, ids, value objects, errors, repositories, unit of work and handlers use the Pivot base types listed above — no hand-rolled Result/command/aggregate/repository bases.
- Repository interfaces in Domain, implementations in Persistence; SQL Server (or PostgreSQL) via EF Core; no in-memory provider in the service.
- Write/read separated; thin handlers propagating `ResultExceptionType`; invariants in the aggregate.
- Events past-tense, immutable, primitive payloads, correctly classified; cross-transport events preserve their id.
- Everything wired; callers use `ISender`/`ApiController` only; validation via the Pivot behaviour; one transaction owner (middleware/interceptor).
- One drain mode and one transport; projections replay-safe; no assumed framework API used without verifying it exists.
- Assumptions stated wherever a convention wasn't visible.

## Example prompts
- "Scaffold a new penalty dossier feature following our CQRS conventions on Pivot."
- "Add a RegisterPayment command end to end — command, handler, event, projection — in our style."
- "Publish an integration event when a dossier closes and consume it in the billing service."
