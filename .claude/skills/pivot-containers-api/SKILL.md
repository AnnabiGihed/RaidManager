---
name: pivot-containers-api
description: Pivot.Framework.Containers.API — ASP.NET Core hosting helpers. Use when writing controllers on ApiController (HandleResult/HandleFailure → ProblemDetails), wiring the middleware pipeline (ExceptionHandlerMiddleware, TransactionMiddleware<TContext>, UseImmediateOutboxDraining), ApiResponse<T>, BFF degraded responses and caching, health checks (SQL Server/PostgreSQL/RabbitMQ/HTTP), OpenTelemetry (AddPivotObservability), SignalR push (AddPivotSignalR/EventPushService), API versioning (AddPivotApiVersioning), resilient typed HTTP clients (AddServiceClient), or the one-call AddKeycloakAuthentication(config, swaggerTitle); and when changing Src/Containers/Pivot.Framework.Containers.API.
---

# Pivot.Framework.Containers.API

Location: `Src/Containers/Pivot.Framework.Containers.API`. References Domain, Application, Infrastructure.Abstraction,
Authentication.AspNetCore, Authentication.Caching; packages: MediatR, EF Core, Asp.Versioning.Mvc(+ApiExplorer),
OpenTelemetry (+OTLP, AspNetCore, Http, Runtime), Polly (Microsoft.Extensions.Http.Polly), RabbitMQ.Client,
Npgsql, Microsoft.Data.SqlClient, Serilog (referenced, not configured by the package).

## 1. Controllers — `Abstractions/ApiController`

```csharp
[Route("api/v{version:apiVersion}/orders")]
[ApiVersion(1.0)]
public sealed class OrdersController(ISender sender) : ApiController(sender)
{
	[HttpPost]
	public async Task<IActionResult> Create(CreateOrderRequest body, CancellationToken ct)
		=> HandleResult(await Sender.Send(new CreateOrderCommand(body.CustomerId), ct));

	[HttpGet("{id:guid}")]
	public async Task<IActionResult> Get(Guid id, CancellationToken ct)
		=> HandleResult(await Sender.Send(new GetOrderQuery(id), ct));

	[HttpPost("{id:guid}/ship")]
	public async Task<IActionResult> Ship(Guid id, CancellationToken ct)
	{
		var result = await Sender.Send(new ShipOrderCommand(id), ct);   // Result (non-generic)
		return result.IsSuccess ? NoContent() : HandleFailure(result);
	}
}
```
- `[ApiController]` is on the base; `Sender` is `protected readonly ISender`.
- `HandleResult<T>(Result<T>)` → `200 OK` with `result.Value` (unwrapped), or `HandleFailure`. There is **no** non-generic `HandleResult(Result)` — use `HandleFailure` as above.
- `HandleFailure(Result)` maps `ResultExceptionType` → `NotFound 404`, `Conflict 409`, `AuthenticationRequired 401`, `AccessDenied 403`, everything else (incl. `ValidationError`) → `400` (title "Validation Error" if the result is an `IValidationResult`, else "Bad Request"). Body is `ProblemDetails { Title, Status, Type = error.Code, Detail = error.Message }` + `extensions.validationErrors` (list of `{code, message}`) when present. Calling it with a success throws.
- `HandleGlobalFailure(Result)` *throws* app exceptions (`NotFoundException`, `BadRequestException`) for use with the middleware instead — rarely needed.
- For 201 Created, write `CreatedAtAction(...)` yourself on success.

`ApiResponse<T>` (`Ok`, `Fail`, `FromResult`) is an optional envelope `{ success, message, errors, data }`. Don't mix envelope and ProblemDetails styles within one API.

## 2. Middleware pipeline

```csharp
app.UseMiddleware<ExceptionHandlerMiddleware>();           // FIRST — catches everything below
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseImmediateOutboxDraining<AppDbContext>();            // only in ImmediateAfterRequest mode; must be OUTSIDE (before) TransactionMiddleware
app.UseMiddleware<TransactionMiddleware<AppDbContext>>();  // after auth, so 401/403 short-circuit without a transaction
app.MapControllers();
```
There are no `UseExceptionHandling()`/`UseTransactions<T>()` helpers (README is wrong) — use `UseMiddleware<…>`.

`ExceptionHandlerMiddleware` → `application/problem+json`:
| Exception | Status | `type` |
|---|---|---|
| `Application.Exceptions.ValidationException` | 400 | `ValidationException` (+ `validationErrors`) |
| `BadRequestException` | 400 | `BadRequestException` (+ `validationErrors`) |
| `NotFoundException` | 404 | `NotFoundException` |
| anything else (incl. `DomainException`, FluentValidation's `ValidationException`) | 500 | `InternalServerError`, title "An unexpected error occurred." |
`detail` = `ex.ToString()` outside Production, `null` in Production; `extensions.traceId` = `HttpContext.TraceIdentifier`; `instance` = path. If the response already started it rethrows.

`TransactionMiddleware<TContext>`: skips `GET` only (HEAD/OPTIONS are wrapped too); begins a transaction via
`ITransactionManager<TContext>`, commits when status is 2xx **or 422**, otherwise rolls back; exceptions → rollback + rethrow.
Note 400 validation failures roll back (nothing was saved anyway). The `UnitOfWork` save happens inside this transaction,
so outbox rows only become visible after commit.

`OutboxProcessingMiddleware<TContext>` (internal; via `UseImmediateOutboxDraining<TContext>()`): after `next`, if 2xx,
resolves `IOutboxProcessor<TContext>` and drains; errors logged, never surfaced. The extension throws at startup unless
`AddOutboxDraining` was called with `ImmediateAfterRequest`. Order matters: middleware registered earlier wraps later ones,
so registering it *before* `TransactionMiddleware` makes the drain run after the commit. Registered after it, the drain would
publish messages while the transaction is still open (a later failure could roll back rows that were already sent).

`ProblemDetailsExtensions.WithValidationErrors(errors)` and `CustomValidationProblemDetails` are helpers for custom handlers.

## 3. Authentication convenience

```csharp
services.AddKeycloakAuthentication(configuration, swaggerTitle: "Orders API", swaggerVersion: "v1");
```
(`ServiceInstallers/AddKeycloakAuthenticationServiceInstaller`) = `AddKeycloakAuthentication(config, o => o.WithCurrentUser().WithRedisTokenCaching().WithSwagger(title, version))`
— JWT bearer + `ICurrentUser` + Redis claims cache/revocation (**needs `ConnectionStrings:Redis`** and `TokenRevocation` section) + SwaggerGen with Keycloak OAuth2.
An interactive reference page is mandatory for every API host (`dotnet-solution-scaffolding`). With this Swashbuckle setup map `app.UseSwagger();` in every environment and `app.UseSwaggerUI(o => o.UseKeycloakOAuth(app.Services));` at least in Development, or Scalar on the same document. Details: `pivot-auth-aspnetcore`, `pivot-auth-caching`.

## 4. BFF helpers (`BFF/*`)

```csharp
services.AddBffInfrastructure(o => o
	.AddCacheable("dashboard:summary", TimeSpan.FromSeconds(30))
	.AddNeverCached("dashboard:alerts"));
services.AddControllers(o => o.Filters.Add<BffResponseFilter>());
```
- `IBffCacheService → InMemoryBffCacheService` (singleton, per-process `ConcurrentDictionary`): only keys declared cacheable are stored/returned; exact-key TTL lookup (declare each concrete key, or wrap the service); `InvalidateByPrefixAsync`.
- Return `BffResponse<T>.Ok(data)`, `.Degraded(partialData, new DegradedComponent { ServiceName = "billing", AffectedSection = "invoices", Reason = "timeout" })` or `.Unavailable(retryAfterSeconds: 30, …)`.
- `BffResponseFilter` turns `Unavailable` into **503** + `Retry-After` header; `Degraded` stays 200 with `degradedComponents` in the body.

## 5. Health checks

```csharp
services.AddHealthChecks()
	.AddPostgreSqlHealthCheck(cs)                          // or AddSqlServerHealthCheck(cs)
	.AddRabbitMQHealthCheck("amqp://guest:guest@rabbit:5672/")   // URI form, not RabbitMQSettings
	.AddServiceHealthCheck("billing", "http://billing/health");  // Degraded on failure by default, 5 s timeout
app.MapHealthChecks("/health");
```
Each check opens a real connection per probe (no pooling for RabbitMQ) — keep probe frequency reasonable.

## 6. Observability, SignalR, versioning, service clients

- `services.AddPivotObservability("orders-service", otlpEndpoint: "http://otel-collector:4317")` — tracing (AspNetCore, HttpClient, `AddSource(serviceName)`) + metrics (AspNetCore, HttpClient, Runtime); OTLP exporter only when an endpoint is given. No resource attributes/logging are set — add `ConfigureResource` yourself if needed. Create `new ActivitySource("orders-service")` for custom spans so they're captured.
- `services.AddPivotSignalR()` → `AddSignalR()` + singleton `EventPushService` (`NotifyAllAsync`, `NotifyGroupAsync`, `NotifyUserAsync`). Map the hub yourself: `app.MapHub<EventNotificationHub>("/hubs/events")`. Clients call `JoinGroup/LeaveGroup`. Typical use: push from an integration-event handler.
- `services.AddPivotApiVersioning(1, 0)` — default v1.0, assume default when unspecified, report versions, readers: URL segment, header `X-Api-Version`, query `api-version`; API explorer group format `'v'VVV` with URL substitution.
- `services.AddServiceClient<IBillingClient, BillingClient>(o => { o.BaseUrl = "http://billing"; o.RetryCount = 3; })` — typed `HttpClient` with timeout, transient-error retry (delay = `RetryBaseDelaySeconds ^ attempt` — with the default base of 1 that is **always 1 s**; use 2 for exponential backoff) and circuit breaker. Add `.AddKeycloakHandler()` yourself only in Blazor/MAUI hosts; for service-to-service tokens write a client-credentials `DelegatingHandler`.

## Changing this package
- It's the composition layer: keep domain logic out; new helpers should be opt-in extension methods (`AddPivotXxx`/`UseXxx`) with `TryAdd*`.
- `InternalsVisibleTo("Pivot.Framework.Containers.API.Tests")`. Tests live in `Tests/Pivot.Framework.Containers.API.Tests` (`Abstractions`, `BFF`, `Extensions`, `Health`, `Middleware`, `Observability`, `RealTime`, `Versioning`, plus `Outbox`/`Inbox`/`Sagas` for messaging). Middleware tests use `DefaultHttpContext` + NSubstitute; EF tests use the InMemory provider.
- If you change status-code mapping, keep HTTP (`ApiController`, `ExceptionHandlerMiddleware`) and gRPC (`pivot-containers-grpc`) mappings consistent and update README.
