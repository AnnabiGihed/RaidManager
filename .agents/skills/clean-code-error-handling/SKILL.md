---
name: clean-code-error-handling
description: Clean Code rules for errors, exceptions, failure logging and null safety in C#, aligned with Pivot.Framework's Result/Error/ResultExceptionType types, domain and application exceptions, ExceptionHandlerMiddleware and gRPC status mapping. Use whenever you throw, catch or log an exception, return a Result, design a TryX method, write middleware or an exception handler, touch nullability, or add retries.
---

# Error handling and null safety

Source: Development Standards Wiki → *Clean Code* → *Error Handling and Null Safety*. Cite rules as
`Error Handling and Null Safety / R<n>`. Severity and *Analyzer*/*Review* markers as in `clean-code-naming`.
Framework specifics: `pivot-domain` (Result, errors, exceptions), `pivot-application` (app exceptions),
`pivot-containers-api` / `pivot-containers-grpc` (boundary mapping).

## 1. Exceptions or results

| Situation | Use (Pivot.Framework) |
|---|---|
| Anticipated business outcome: not found, validation failure, conflict, transition not allowed | `Result` / `Result<T>` from `Pivot.Framework.Domain.Shared` with an `Error(code, message)` and the matching `ResultExceptionType` (`NotFound`, `Conflict`, `ValidationError`, `AuthenticationRequired`, `AccessDenied`) |
| Several validation errors | `ValidationResult` / `ValidationResult<T>.WithErrors(...)` (produced automatically by `ValidationPipelineBehavior`) |
| Parsing or lookup where absence is normal | `TryX`, or `Task<T?> FindByIdAsync` (Pivot repositories) |
| Broken invariant inside an aggregate that must never happen | a `DomainException` subtype (`RequiredDomainException`, `OutOfRangeDomainException`, …; several → `DomainExceptionScope` → `AggregateDomainException`) |
| Programming error, unavailable infrastructure | exception (framework infrastructure converts these to `Result.Failure` at its own boundary, e.g. `UnitOfWork.SaveChangesAsync`) |

- 🔴 **R1.** Exceptions only for the exceptional. *Review.*
- 🔴 **R2.** Never use exceptions for control flow. *Review,* S112.
- 🟡 **R3.** Anticipated failures return `Result`/`Result<T>` with a typed `Error` **and the correct `ResultExceptionType`** — it decides the HTTP status (400/404/409/401/403 via `ApiController.HandleFailure`) and the gRPC status. When converting between result types, propagate it: `Result.Failure<T>(r.Error, r.ResultExceptionType)`. *Review.*
- 🟡 **R4.** Parsing and lookups offer `TryX` or a nullable `Find…`. *Review.*
- Error codes are stable, dotted, machine-readable (`Order.Status.NotPaid`, `Saga.NoSteps`); define them as `static readonly Error` fields in a `<Aggregate>Errors` class, or use `BaseDomainErrors.General.*` for generic cases.

## 2. Throwing
- 🔴 **R5.** Never throw `Exception`, `SystemException` or `ApplicationException`; throw the most specific type. *Analyzer:* S112, CA2201.
- 🔴 **R6.** Messages name the failing operation and values, without secrets or personal data. Never "Invalid input". *Review.*
- 🔴 **R7.** Guard public entry points: `ArgumentNullException.ThrowIfNull(x)`, `ArgumentException.ThrowIfNullOrWhiteSpace(x)`, `ArgumentOutOfRangeException.ThrowIfNegative(x)`; injected dependencies `?? throw new ArgumentNullException(nameof(x))` (Pivot pattern). *Analyzer:* CA1062, S3928.
- 🟡 **R8.** Domain rule violations throw a `DomainException` subtype (it is abstract — never throw it directly), never a framework exception. *Review.*
- 🟡 **R9.** Create a custom exception type only if some caller catches it by type. *Review.*

## 3. Catching
- 🔴 **R10.** Catch `Exception` only in: the global handler/middleware, a message consumer's outermost frame (`RabbitMQReceiver`), a background loop (`OutboxPublisherService`), a resilience policy, or an infrastructure boundary that converts it into `Result.Failure` (Pivot repositories/publishers). Log it with the exception and never continue as if it succeeded. *Analyzer:* CA1031; *review.*
- 🔴 **R11.** No empty `catch`; no log-and-continue into code assuming success. *Analyzer:* S2486, S108.
- 🔴 **R12.** Rethrow with `throw;`; wrap with the original as `innerException`. *Analyzer:* CA2200, S3445.
- 🔴 **R13.** Delete any `catch` that only rethrows. *Analyzer:* S2737.
- 🔴 **R14.** `OperationCanceledException` is never logged as an error or turned into a 500 or a `Result.Failure` — rethrow it (Pivot's `UnitOfWork`, `ProjectionRebuilder` and `SagaOrchestrator` all do `catch (OperationCanceledException) { throw; }` first). *Review.*
- 🟡 **R15.** Catch only what you can handle, with `when` filters. *Review.*

```csharp
catch (DbUpdateConcurrencyException ex)
{
	return Result.Failure(new Error("Order.ConcurrencyConflict", ex.Message), ResultExceptionType.Conflict);
}
```

## 4. Boundaries
- 🔴 **R16.** No raw exception crosses a process boundary, and there is **one** global translator per transport — never try/catch per controller:
  - HTTP: `app.UseMiddleware<ExceptionHandlerMiddleware>()` first in the pipeline → RFC 7807/9457 `application/problem+json` (`ValidationException`/`BadRequestException` → 400 with `validationErrors`, `NotFoundException` → 404, anything else → 500).
  - Controllers return `HandleResult(result)` / `HandleFailure(result)` from `ApiController` for `Result` failures.
  - gRPC: `AddPivotGrpc()` registers `GrpcExceptionInterceptor`; use `result.GetValueOrThrow(statusMapper)`.
  - Consequence: `DomainException` subtypes surface as **500** through the middleware. Either return a `Result` for anticipated domain failures, or extend the framework mapping (change `ExceptionHandlerMiddleware.MapException` and `DefaultGrpcExceptionStatusMapper` together, with tests) — do not add a second, competing handler. *Review,* integration test.
- 🔴 **R17.** Error responses never contain stack traces, connection strings, SQL, internal host names or personal data in reachable environments. Pivot's middleware and gRPC mapper already omit `detail` in `Production` — keep `ASPNETCORE_ENVIRONMENT=Production` in every non-developer environment. *Review.*
- 🔴 **R18.** Every returned failure carries a trace identifier that also appears in the server log. The middleware adds `extensions.traceId = HttpContext.TraceIdentifier` and logs it; if you write another boundary (e.g. `ProblemDetails` from `HandleFailure`), add `traceId` the same way. *Review.*
- 🟡 **R19.** Infrastructure exceptions are translated at the boundary of the layer that produced them: a `DbUpdateException` must not reach a command handler — `UnitOfWork.SaveChangesAsync` returns `DatabaseError` / `DbUpdateConcurrencyError` results instead. *Review.*

## 5. Logging failures
- 🔴 **R20.** Pass the exception first: `_logger.LogError(ex, "…")`, never `$"…{ex.Message}"`. *Analyzer:* S6667.
- 🔴 **R21.** Constant message templates with structured placeholders. *Analyzer:* CA2254, S2629.
- 🔴 **R22.** Log a failure once, where it is handled. *Review.*
- 🔴 **R23.** No personal data beyond a resolvable identifier (log `UserId`, not email). *Review.*

## 6. Nullability
- 🔴 **R24.** `<Nullable>enable</Nullable>` solution-wide (Pivot's `Directory.Build.props` sets it), nullable warnings as errors.
- 🔴 **R25.** The null-forgiving `!` is forbidden unless the same line explains why the compiler is wrong. Accepted Pivot idioms: `Id = default!;` / `= default!;` in EF Core materialisation constructors (comment "EF Core"), and `_value!` guarded by `IsSuccess`. *Review.*
- 🔴 **R26.** `null` is never a business value; model "not decided / not applicable / unknown" as distinct states. *Review.*
- 🟡 **R27.** Collection-returning methods return empty, never `null`. *Analyzer:* S1168.
- 🟡 **R28.** Changing a return from `T` to `T?` is breaking → `### Breaking changes` in the CHANGELOG. *Review.*

## 7. Retries
- 🟡 **R29.** Retries, timeouts, circuit breakers are Polly policies declared at composition time — Pivot already does this for RabbitMQ publishing (`MessagingResiliencePolicies`) and typed service clients (`AddServiceClient`). Never loops with `Task.Delay` in business code. *Review.*
- 🔴 **R30.** A retried operation is idempotent or deduplicated: inbox (`AddInboxSupport`) for consumers, `IIdempotentCommand` for commands (non-generic `ICommand` only — see `pivot-messaging-outbox`). *Review.*
- 🟡 **R31.** Each retry emits a warning/metric; final failure logs an error (the outbox logs dead-lettering and emits `OutboxMessageFailedEvent`). *Review.*

## 8. Checklist
- [ ] Anticipated outcomes are `Result`s with the right `ResultExceptionType`; exceptions are specific with useful messages.
- [ ] No catch of `Exception` outside the allowed places; no empty or log-and-continue catches; `throw;` only.
- [ ] Cancellation is rethrown, never an error.
- [ ] API errors go through `ExceptionHandlerMiddleware` / `ApiController` / the gRPC interceptor as Problem Details or status codes with a trace id, no internals leaked.
- [ ] Logging uses the exception argument and constant templates, once, without personal data.
- [ ] No unexplained `!`; no `null` meaning several things; empty collections instead of null.
- [ ] Retried operations are idempotent.
