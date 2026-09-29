---
name: pivot-testing
description: Testing conventions for Pivot.Framework — xUnit 2 + FluentAssertions 8 + NSubstitute 5 + coverlet, EF Core InMemory test contexts, middleware/interceptor/DI-registration test patterns, naming and file layout, coverage settings. Use when adding or fixing tests in Tests/*, creating a new test project for a package that has none, writing regression tests for framework bug fixes, or unit-testing a consuming service's handlers/aggregates built on Pivot.Framework.
---

# Testing Pivot.Framework

## Test projects

Scope: this skill covers the **Pivot.Framework repository**. Services built on Pivot use the house stack (Reqnroll + Shouldly + Moq for business behaviour) described in `dotnet-unit-tests` and `gherkin-scenarios`; never mix the two stacks in one repository.

| Project | Covers | Extra packages |
|---|---|---|
| `Tests/Pivot.Framework.Domain.Tests` | Domain (Primitives, Shared, Errors, Exceptions, IntegrationEvents, Sagas; doubles in `TestDoubles/` e.g. `TestAggregateRoot`, `TestId`, `TestDomainEvent`) | none (no NSubstitute) |
| `Tests/Pivot.Framework.Application.Tests` | Behaviors, Correlation, DomainEventHandler, Exceptions, Messaging, ReadModels, Responses | NSubstitute |
| `Tests/Pivot.Framework.Infrastructure.Abstraction.Tests` | options/models/defaults of every contract | none |
| `Tests/Pivot.Framework.Containers.API.Tests` | Containers.API **and** Messaging/Persistence EF behaviour (Outbox, Inbox, Sagas, Extensions) | NSubstitute, EF InMemory, AspNetCore.Http, SignalR |
| `Tests/Pivot.Framework.Containers.Grpc.Tests` | Extensions, Interceptors, StatusMapping (`TestDoubles/TestServerCallContext`, `TestHostEnvironment`, `TestDbContext`, `TestValidationResult`) | NSubstitute, EF InMemory |
| `Tests/Pivot.Framework.Authentication.Tests` | Auth core services (`TestDoubles/StubHttpMessageHandler`), Storage, Extensions, API endpoints, Testing helpers | NSubstitute |

No test projects yet for: Tools.DependencyInjection, Persistence.PostgreSQL, Infrastructure.Caching, ReadStore.MongoDB,
Infrastructure.Scheduling, Authentication.Caching, Authentication.Blazor, Authentication.Maui, Authentication.Hangfire.
Creating one: copy a sibling `.csproj` (Microsoft.NET.Test.Sdk 18.3, xunit 2.9.3, xunit.runner.visualstudio 3.1.5,
FluentAssertions 8.9, NSubstitute 5.3, coverlet.collector 8.0.1), `GlobalUsings.cs` with `global using Xunit;`, reference the
package under test, add it to `Pivot.Framework.sln` under the `Tests` folder. For `internal` types add
`[assembly: InternalsVisibleTo("<TestProject>")]` (+ `DynamicProxyGenAssembly2` for NSubstitute) in the source project's `InternalsVisibleTo.cs`.

## Conventions (match existing files)
- Folder/namespace mirrors the source area: `Tests/Pivot.Framework.Containers.API.Tests/Middleware/TransactionMiddlewareTests.cs` → `namespace Pivot.Framework.Containers.API.Tests.Middleware;`.
- One test class per unit, `public class XxxTests`, header doc comment (`Author`, `Date`, `Purpose`) like production code; tabs.
- `#region` groups: `Fields`, `Constructors`, `Test Infrastructure`, then by behaviour (`Happy Path Tests`, `Bypass Tests`, `Commit Tests`, …).
- Method names: `Method_Scenario_ShouldExpectation` (`ProcessOutboxMessagesAsync_NoMessages_ShouldReturnSuccess`, `POST_With2xxStatus_ShouldCommit`), each with a `/// <summary>` line.
- Arrange/act/assert separated by blank lines; FluentAssertions (`result.IsSuccess.Should().BeTrue()`, `act.Should().ThrowAsync<InvalidOperationException>()`); NSubstitute `Received()/DidNotReceive()`, `Arg.Any<CancellationToken>()`.
- `[Theory]` + `[InlineData]` for status-code/enum matrices.

## Patterns

EF InMemory context for anything generic over `TContext`:
```csharp
public class TestDbContext : DbContext, IPersistenceContext
{
	public TestDbContext() : base(new DbContextOptionsBuilder<TestDbContext>()
		.UseInMemoryDatabase(Guid.NewGuid().ToString()).Options) { }

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		modelBuilder.Entity<OutboxMessage>();
		modelBuilder.ApplyConfiguration(new OutboxMessageConsumerConfiguration());
	}
}
```
(InMemory ignores transactions and column types — use it for logic, not for SQL behaviour. `BeginTransactionAsync` on InMemory throws unless the warning `InMemoryEventId.TransactionIgnoredWarning` is ignored; prefer substituting `ITransactionManager<T>`.)

Middleware:
```csharp
var middleware = new TransactionMiddleware<TestDbContext>(next: ctx => { ctx.Response.StatusCode = 500; return Task.CompletedTask; }, logger);
var services = new ServiceCollection().AddSingleton(_transactionManager).BuildServiceProvider();
var context = new DefaultHttpContext { RequestServices = services };
context.Request.Method = "POST";
await middleware.InvokeAsync(context);
await _transactionManager.Received(1).RollbackTransactionAsync(Arg.Any<CancellationToken>());
```

DI registration tests (see `Tests/…Containers.API.Tests/Extensions`):
```csharp
var services = new ServiceCollection();
services.AddEfCoreWritePersistence<TestDbContext, TestUnitOfWork>();
services.Should().Contain(d => d.ServiceType == typeof(IOutboxRepository<TestDbContext>) && d.Lifetime == ServiceLifetime.Scoped);
// behavioural: build the provider with required dependencies and resolve
```

Minimal-API routes: build a `WebApplication`, map, and read `((IEndpointRouteBuilder)app).DataSources` route patterns (see `AuthenticationApiTests`).

HTTP-calling services: inject `new HttpClient(new StubHttpMessageHandler(...))` and assert the captured request (URL, form fields) and parsed response.

Claims/current user: `AuthenticationTestContextFactory.CreatePrincipal(...)` / `CreateHttpContext(principal)` from `Pivot.Framework.Authentication.AspNetCore.Testing`.

Result assertions: check `IsSuccess`/`IsFailure`, `Error.Code`, **and** `ResultExceptionType` (it drives HTTP/gRPC status). For validation results assert `result.Should().BeAssignableTo<IValidationResult>()` and inspect `Errors`.

Serialization regressions (outbox/transport): round-trip with the same Newtonsoft settings the framework uses (`TypeNameHandling.None`) and assert `Id`/`OccurredOnUtc` are preserved — this currently fails for `DomainEvent`/`IntegrationEvent` (see `pivot-domain` §4); a fix should come with exactly such a test.

## Testing consuming services
- Aggregates: pure unit tests — call factory/behaviour, assert state, `GetDomainEvents()` contents and `Version`.
- Command handlers: substitute your repository interface and `IUnitOfWork<TContext>` (`SaveChangesAsync` returns `Result.Success()`), assert the returned `Result`.
- Validators: FluentValidation `TestValidate`; pipeline behaviour: construct `ValidationPipelineBehavior<TReq,TRes>` with validators and a `next` delegate.
- Integration tests: `WebApplicationFactory<Program>` + Testcontainers (PostgreSQL/RabbitMQ/Redis/Keycloak) — replace `JwtBearer` with a test scheme or mint tokens from a Keycloak container.

## Running and coverage
```bash
dotnet test Pivot.Framework.sln -c Release --collect:"XPlat Code Coverage" --results-directory ./TestResults
dotnet test Tests/Pivot.Framework.Domain.Tests --filter "FullyQualifiedName~StronglyTypedGuidId"
```
`coverletrc` includes `[Pivot.Framework.*]*`, excludes test assemblies, migrations, designers and `AssemblyReference`.
CI (`test.yml`) reports coverage only for Domain, Application, Containers.API, Infrastructure.Abstraction (ReportGenerator
`assemblyfilters`), thresholds 60/80 (informational, `fail_below_min: false`), and fails on any failing test.
Add new assemblies to those filters when you add test projects.
