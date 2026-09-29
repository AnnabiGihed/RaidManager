---
name: pivot-framework
description: Entry point and map of Pivot.Framework (the .NET 10 Clean Architecture NuGet package family). Use first whenever a task touches this repository or a service built on Pivot.Framework packages — to find which package owns a concept, the dependency graph between packages, repo-wide coding conventions, build/test/publish commands, the end-to-end recipe for wiring a new service, and the list of places where README.md is out of date with the source. Routes to the per-package pivot-* skills.
---

# Pivot.Framework — overview and router

Pivot.Framework is a family of NuGet packages (target `net10.0`, AGPL-3.0, author Gihed Annabi) that provide
plug-and-play infrastructure for Clean Architecture / DDD / CQRS services: domain primitives, an application
layer on MediatR, EF Core write-side persistence with a transactional outbox, RabbitMQ messaging, an event
store with projection rebuilds, sagas, read stores (EF Core / MongoDB), Redis caching, Hangfire scheduling,
ASP.NET Core and gRPC hosting helpers, and Keycloak authentication for APIs, Blazor Server and MAUI.

**Source of truth is the code under `Src/`, not `README.md`.** The README is partially stale — see
"Known README drift" below before copying anything from it.

## Package map → skill to load

| Package (folder under `Src/`) | What it owns | Skill |
|---|---|---|
| `Core/Pivot.Framework.Domain` | Entities, aggregates, strongly-typed IDs, `AuditInfo`, domain/integration events, `Result`/`Error`, domain exceptions, `IAsyncCommandRepository`, `IUnitOfWork`, saga enums | `pivot-domain` |
| `Core/Pivot.Framework.Application` | CQRS contracts on MediatR, validation pipeline, event handlers, read-model contracts, correlation/causation/replay context, saga contracts, app exceptions | `pivot-application` |
| `Tools/Pivot.Framework.Tools.DependencyInjection` | `IServiceInstaller`, `BaseServiceInstaller` (Scrutor scanning) | `pivot-dependency-injection` |
| `Infrastructure/Pivot.Framework.Infrastructure.Abstraction` | Pure contracts: outbox, inbox, broker, event store, sagas, transactions, UoW, scheduling, BFF, audit, search, object storage | `pivot-infrastructure-abstraction` |
| `Infrastructure/Pivot.Framework.Infrastructure.Persistence.EntityFrameworkCore` | `PivotDbContextBase`, `UnitOfWork<TContext>`, repositories, specifications, transaction manager, outbox/event-store/inbox/saga EF implementations | `pivot-persistence-efcore` (+ `pivot-event-store-sagas`) |
| `Infrastructure/Pivot.Framework.Infrastructure.Persistence.PostgreSQL` | Npgsql registration, jsonb/timestamptz overrides, advisory locks, read/write contexts | `pivot-persistence-postgresql` |
| `Infrastructure/Pivot.Framework.Infrastructure.Messaging.EntityFrameworkCore` | DI entry points (`AddEfCoreWritePersistence`, `AddOutboxDraining`, …), RabbitMQ publisher/receiver/topology, in-process publisher, inbox, integration-event mapping, projection dispatcher, saga/event-store registration | `pivot-messaging-outbox` (+ `pivot-event-store-sagas`) |
| `Infrastructure/Pivot.Framework.Infrastructure.Caching` | `ICacheService` over Redis `IDistributedCache` | `pivot-caching-redis` |
| `Infrastructure/Pivot.Framework.Infrastructure.ReadStore.MongoDB` | Mongo `IReadModelRepository`/`IReadModelStore` | `pivot-readstore-mongodb` |
| `Infrastructure/Pivot.Framework.Infrastructure.Scheduling` | Hangfire server + dashboard + `IRecurringJobService` | `pivot-scheduling-hangfire` |
| `Containers/Pivot.Framework.Containers.API` | `ApiController`, exception/transaction/outbox middleware, BFF, health checks, OpenTelemetry, SignalR, API versioning, resilient service clients | `pivot-containers-api` |
| `Containers/Pivot.Framework.Containers.Grpc` | gRPC exception + transaction interceptors, Result→Status mapping, proto conventions | `pivot-containers-grpc` |
| `Containers/Authentication/Pivot.Framework.Authentication` | `KeycloakOptions`, `ICurrentUser`, `IKeycloakAuthService`, provider-neutral IdP services, token message handler | `pivot-auth-core` |
| `Containers/Authentication/…Authentication.AspNetCore` | `AddKeycloakAuthentication` (JWT bearer), Swagger OAuth2, policies, test helpers | `pivot-auth-aspnetcore` |
| `Containers/Authentication/…Authentication.Caching` | Redis claims cache + token revocation (`WithRedisTokenCaching`) | `pivot-auth-caching` |
| `Containers/Authentication/…Authentication.API` | `MapAuthenticationApi` minimal-API endpoints | `pivot-auth-api` |
| `Containers/Authentication/…Authentication.Blazor` | Blazor Server PKCE flow with Redis session store | `pivot-auth-blazor` |
| `Containers/Authentication/…Authentication.Maui` | MAUI PKCE via `WebAuthenticator` + `SecureStorage` | `pivot-auth-maui` |
| `Containers/Authentication/…Authentication.Hangfire` | Cookie + OIDC login for the Hangfire dashboard | `pivot-auth-hangfire` |
| `Tests/*` | xUnit + FluentAssertions + NSubstitute conventions | `pivot-testing` |

## Dependency graph (project references)

```
Domain  ←  Application  ←  Persistence.EntityFrameworkCore  ←  Messaging.EntityFrameworkCore
   ↑            ↑                   ↑                                   
   └── Infrastructure.Abstraction ──┘        Persistence.PostgreSQL → Persistence.EntityFrameworkCore
Domain, Infrastructure.Abstraction ← Infrastructure.Scheduling
Application ← ReadStore.MongoDB
Infrastructure.Caching (standalone: StackExchangeRedis only)
Tools.DependencyInjection (standalone: Scrutor only)
Containers.API → Domain, Application, Infrastructure.Abstraction, Authentication.AspNetCore, Authentication.Caching
Containers.Grpc → Domain, Application, Infrastructure.Abstraction
Authentication (core) ← AspNetCore ← Caching (also → Infrastructure.Caching)
Authentication (core) ← API, Blazor, Maui, Hangfire (Hangfire also → Infrastructure.Scheduling)
```

Layering rule: **Domain has no infrastructure dependency** (only `CSharpFunctionalExtensions`, used for
`ValueObject<T>`). Application depends only on Domain. `Infrastructure.Abstraction` holds interfaces only —
never add EF Core, RabbitMQ, Redis, etc. there. Implementations live in the concrete infrastructure packages.
Registration extension methods for EF-based messaging features live in `Messaging.EntityFrameworkCore`
even when the implementation class lives in `Persistence.EntityFrameworkCore`.

## Repo-wide coding conventions (follow them for every change)

- **Indentation: tabs.** File-scoped namespaces. `Nullable` and `ImplicitUsings` enabled (from `Directory.Build.props`). `LangVersion` latest.
- Every public type starts with the header doc comment:
  ```csharp
  /// <summary>
  /// Author      : Gihed Annabi
  /// Date        : MM-YYYY
  /// Purpose     : One-paragraph explanation of the type's responsibility.
  /// </summary>
  ```
  and every public member has XML docs.
- Members are grouped with `#region` blocks: `Fields`, `Properties`, `Constructors`, `Factory`/`Factory Methods`, `Public Methods`, `Domain Behaviours`, `Equality`, `Private Helpers`, etc.
- Guard clauses at the top: `ArgumentNullException.ThrowIfNull(x)`, `ArgumentException.ThrowIfNullOrWhiteSpace(s)`. Constructor-injected dependencies use `?? throw new ArgumentNullException(nameof(x))`.
- Expected failures return the framework's own `Pivot.Framework.Domain.Shared.Result` / `Result<T>` with an `Error(code, message)` — **not** `CSharpFunctionalExtensions.Result`. Error codes are dotted strings (`"Projection.NotFound"`, `"Saga.NoSteps"`, `"DomainEventPublishError"`).
- Infrastructure methods catch exceptions and convert them to `Result.Failure(new Error("Xxx.Yyy", ex.Message))`, but always rethrow `OperationCanceledException`.
- DI extension methods: `static class XxxExtensions`, return `IServiceCollection`, validate args, prefer `TryAdd*` so consumers can override.
- Generic-over-context pattern: anything tied to a database is generic over `TContext : DbContext, IPersistenceContext` (or `class, IPersistenceContext` in abstractions) so multiple write contexts can coexist in one process.
- Serialization: outbox/event-store payloads use **Newtonsoft.Json** with `TypeNameHandling.None`; event type is stored as `AssemblyQualifiedName`. Redis caches use **System.Text.Json** camelCase.
- Timestamps are UTC (`DateTime.UtcNow`, `DateTimeKind.Utc` validated by domain types).
- Each assembly has an `AssemblyReference` static class exposing `Assembly`.
- Tests use `InternalsVisibleTo` (see `InternalsVisibleTo.cs` in Containers.API and Messaging).

## Build, test, publish

```bash
dotnet restore Pivot.Framework.sln
dotnet build Pivot.Framework.sln -c Release --no-restore
dotnet test Pivot.Framework.sln -c Release --no-build --collect:"XPlat Code Coverage"
```

- CI: `.github/workflows/test.yml` (push/PR on master/main/develop) builds, tests, generates coverage (ReportGenerator), posts a sticky PR comment. `sonarqube.yml` runs analysis.
- Publishing: pushing a tag `vX.Y.Z` triggers `.github/workflows/publish.yml`, which rewrites `<Version>` in every `.csproj`, packs, and pushes to `https://nuget.pkg.github.com/AnnabiGihed/index.json`. All packages always share one version. Do not bump `<Version>` by hand.
- Every package `.csproj` has `GeneratePackageOnBuild`, `PackageId`, `Version`, `Description`. A new package needs those, plus adding it to `Pivot.Framework.sln` under the right solution folder.
- Test projects exist for Domain, Application, Infrastructure.Abstraction, Containers.API (also covers outbox, inbox, sagas via Messaging), Containers.Grpc and Authentication. Persistence, Caching, MongoDB, Scheduling, Blazor and MAUI have no dedicated test project.

## Recipe: wiring a new write-side service (HTTP)

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;
var config = builder.Configuration;

// 1. MediatR + validation (+ optional idempotency)
services.AddMediatR(cfg =>
{
	cfg.RegisterServicesFromAssembly(MyService.Application.AssemblyReference.Assembly);
	cfg.AddOpenBehavior(typeof(ValidationPipelineBehavior<,>));
});
services.AddValidatorsFromAssembly(MyService.Application.AssemblyReference.Assembly);

// 2. Persistence (PostgreSQL example) — see pivot-persistence-efcore / -postgresql
services.AddPostgreSqlContext<AppDbContext>(config.GetConnectionString("Default")!);
services.AddEfCoreWritePersistence<AppDbContext, AppUnitOfWork>(includeEventStore: false);
services.AddDomainEventDispatcher();                  // needed by in-process + RabbitMQ receiver
services.AddIntegrationEventDispatcher();             // needed if this service consumes integration events

// 3. Transport + outbox drain — see pivot-messaging-outbox
services.AddRabbitMQPublisher(config);                // or services.AddInProcessMessagePublisher();
services.AddOutboxDraining<AppDbContext>(o =>
{
	o.Mode = OutboxDrainMode.BackgroundPolling;
	o.PollingInterval = TimeSpan.FromSeconds(5);
});

// 4. Auth — see pivot-auth-aspnetcore / pivot-containers-api
// Containers.API convenience overload = JWT + ICurrentUser + Redis token cache (needs ConnectionStrings:Redis) + Swagger
services.AddKeycloakAuthentication(config, "My Service API");

services.AddControllers();
var app = builder.Build();

app.UseMiddleware<ExceptionHandlerMiddleware>();       // first
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<TransactionMiddleware<AppDbContext>>();
app.MapControllers();
app.Run();
```

## Known README drift (verified against source)

| README says | Source actually has |
|---|---|
| `Result` comes from `CSharpFunctionalExtensions`; `Result.Failure<T>("code")` | Framework's own `Pivot.Framework.Domain.Shared.Result`/`Result<T>`/`Error`; failures take an `Error` plus optional `ResultExceptionType` |
| `new DomainException("msg")`; `AggregateDomainException(orderId, "msg")` | `DomainException` is **abstract** `(parameterName, message)`; `AggregateDomainException(IEnumerable<DomainException>)` |
| `DomainError.Deserialize` returns `DomainError` | Returns `Error` |
| `ICurrentUser` in Application package | `ICurrentUser` lives in `Pivot.Framework.Authentication.Models`; Application has `ICurrentUserProvider` (`string GetCurrentUser()`) |
| `unitOfWork.Repository<Order, OrderId>()`, `repo.GetByIdAsync` | `UnitOfWork<TContext>` exposes only `SaveChangesAsync`; repositories are `BaseAsyncCommandRepository<TEntity,TId>` subclasses using `FindByIdAsync` |
| `TemplatesCoreDbContextBase<TContext>` | `PivotDbContextBase` (non-generic; file is still named `TemplatesCoreDbContextBase.cs`) |
| `builder.ConfigureDomainPrimitives()` | `modelBuilder.ApplyDomainPrimitives()` (called by `PivotDbContextBase`) |
| `EntitySpecification<Order>`, `ApplyOrderBy`, `EntitySpecificationEvaluator<Order>.GetQuery` | `EntitySpecification<TEntity, TId>`, `AddOrderBy`/`AddOrderByDescending`, `EntitySpecificationEvaluator.GetQuery<TEntity,TId>(…)` |
| `services.AddEfCoreReadModel<…>()` | `services.AddEfCoreReadModelStore<TContext>()` (open generics) |
| `services.AddMongoReadModel<T,TId>(configuration)` | `services.AddMongoReadModelStore(connectionString, databaseName)` |
| `ProjectionHandler<TEvent, TReadModel>` with `HandleAsync` returning the model | `ProjectionHandler<TEvent>` with `ProjectAsync(TEvent, ct)` |
| `IRecurringJobService.AddOrUpdate(...)`, `RecurrenceConfiguration`, `TimeOfDay`, `IRecurringJob` | `IRecurringJobService<TIdentifier,TParams,TValue>` with `CreateJob`/`CreateJobWithParams`/…, `RecurrenceConfig { Type, Interval }` |
| `services.AddScheduling(configuration)` | `services.AddHangfireWithDashboard(configuration, "HangfireConnection")` + `app.UseHangfireDashboardWithOptions()` |
| `app.UseExceptionHandling()`, `app.UseTransactions<T>()` | No such extensions — use `app.UseMiddleware<ExceptionHandlerMiddleware>()` / `app.UseMiddleware<TransactionMiddleware<TContext>>()` |
| `HandleResult(Result)` non-generic, `ValidationException → 422` | Only `HandleResult<T>(Result<T>)` + `HandleFailure(Result)`; `ValidationException`/`BadRequestException` → **400** in middleware |
| RabbitMQ config keys `Host`, `Username` | `HostName`, `UserName`, plus required `Queue`, `RoutingKey`, `EncryptionKey` (32 chars), `ClientProvidedName` |
| `AddKeycloakBackend(configuration)` | `internal`; public API is `AddKeycloakAuthentication(config, o => o.WithCurrentUser()…)` |
| `AddKeycloakRedisCache`, `TokenRevocation:DefaultTtlDays`, `RevokeAsync(token)` | `.WithRedisTokenCaching()` on the options builder; `TokenRevocation:RevokeAllTtl` (TimeSpan); `RevokeAsync(token, expiresAt)` |
| Blazor `LoginAsync(returnUrl)`; `IBlazorKeycloakAuthService` redeclares Login/Logout/Refresh | `LoginAsync(ct)` only (return URL = current page); interface adds just `InitialiseFromCookieAsync` and `HandleCallbackAsync` |

## Known defects worth knowing (verified)

1. **Event IDs are not preserved across the wire.** `DomainEvent` and `IntegrationEvent` expose get-only `Id`/`OccurredOnUtc`; Newtonsoft deserialization runs the base parameterless constructor and generates a new `Id`. Consequence: `RabbitMQReceiver` and `InProcessMessagePublisher` compute inbox dedup keys from a fresh `Id` on every delivery, so redeliveries are not deduplicated. Workaround per event: a `[Newtonsoft.Json.JsonConstructor]` constructor taking `(Guid id, DateTime occurredOnUtc, …)` that forwards to `base(id, DateTime.SpecifyKind(occurredOnUtc, DateTimeKind.Utc))`. Framework-level fix: make the base properties deserializable (see `pivot-domain`).
2. `IdempotentCommandBehavior` returns `(TResponse)Result.Success()` on duplicates — throws `InvalidCastException` when the command returns `Result<T>`. Only use `IIdempotentCommand` on non-generic `ICommand`.
3. `UnitOfWork.UpdateAuditableEntities` calls `entry.Entity.Audit.Modify(...)` for modified entities; if `Audit` is null (row created outside the framework) the save fails with an `UnexpectedError` result.
4. `Infrastructure.Scheduling/AssemblyReference.cs` declares namespace `Pivot.Framework.Infrastructure.Persistence.EntityFrameworkCore` — same full name as the Persistence package's `AssemblyReference` (CS0433 ambiguity if a consumer references both and uses it).
5. `RecurringJobService` passes captured `Func<>` delegates to Hangfire expressions; Hangfire cannot serialize delegate targets reliably — verify before relying on it.
6. `Authentication.Blazor` contains `KeycloakCallback.razor`, but the project uses `Microsoft.NET.Sdk` so the page is not compiled into the package — consumers must add their own `/auth/callback` page (as the README says).
7. `EventStoreRepository.GetFromPositionAsync` uses offset paging (`Skip(position)`) ordered by `CreatedAtUtc, Id`, so "position" is a row count, not a stable sequence number.

When fixing any of these, add a regression test in the matching `Tests/` project (see `pivot-testing`).

## House engineering standards (merged skills, aligned with Pivot.Framework)

These organization-wide skills sit alongside the `pivot-*` package skills and have been aligned with what the
framework provides (Pivot types, Keycloak via Pivot auth, RabbitMQ via the Pivot outbox, Pivot code style):

| Area | Skills |
|---|---|
| Architecture & design | `architecture-proposal`, `dotnet-ddd-cqrs-conventions`, `dotnet-solution-scaffolding`, `docs-adr` |
| C# style (codifies Pivot's own style) | `csharp-regions`, `csharp-xml-documentation`, `clean-code-naming`, `clean-code-functions`, `clean-code-types`, `clean-code-error-handling`, `clean-code-comments`, `clean-code-refactoring`, `clean-code-static-analysis` |
| .NET practice | `dotnet-best-practices`, `dotnet-10-and-csharp-14`, `dotnet-code-review`, `blazor-components` |
| Testing | `dotnet-unit-tests` + `gherkin-scenarios` (services: Reqnroll/Shouldly/Moq), `dotnet-e2e-tests`, `pivot-testing` (this repo: xUnit/FluentAssertions/NSubstitute) |
| Documentation | `docs-standards-governance`, `docs-as-code`, `docs-root-files`, `docs-api-contracts`, `docs-diagrams-as-code`, `docs-quality-gates`, `docs-standards-wiki-page` |
| Workflow | `repository-readiness` (run first), `pr-and-branching-standards` |

Precedence: when a house skill and a `pivot-*` skill disagree about how the framework behaves, the `pivot-*` skill
(verified against source) wins; when they disagree about process or style, the house skill wins.
