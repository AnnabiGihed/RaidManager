---
name: dotnet-best-practices
description: 'Apply idiomatic .NET standards when writing or refactoring code: the recommended pattern for common architecture,
  API, config, async, data-access, security, and testing decisions. Use whenever writing or changing C#/.NET code — implementing
  a feature, fixing a bug, or refactoring — even if the request does not mention best practices. Prescriptive guidance, not
  a findings audit (use dotnet-code-review for audits).'
---

Use this skill to write or refactor .NET code to the idiomatic standard. This is the prescriptive counterpart to a review: instead of listing what's wrong, it names the recommended pattern for each common decision and shows it. For auditing existing code and ranking issues by severity, use the `dotnet-code-review` skill instead.

## Rules

- Recommend one idiomatic default per decision, then note the exception that would change it. Don't present every option neutrally — pick the winner and justify it briefly.
- When refactoring, preserve observable behavior unless the user asked for a behavior change. Call out explicitly if a recommendation alters behavior.
- Apply standards proportionally to the workload. A console utility doesn't need the layering of a service; say when a practice is overkill for the context rather than applying it reflexively.
- Every pattern recommendation shows a minimal code snippet demonstrating it. No snippet, no recommendation.
- Prefer the platform/BCL way over a third-party library unless the library earns its place. Name the built-in option first.
- Don't recommend a pattern the target framework version can't support. Confirm the TFM before suggesting `required` members, primary constructors, collection expressions, etc.
- Match the project's existing conventions and analyzer config. Don't impose a house style the codebase has already chosen against.
- Threading a `CancellationToken` through async paths, structured logging, and disposing `IDisposable`/`IAsyncDisposable` are defaults, not optional polish.

## Idiomatic defaults

Name the recommended choice for each common fork; note the exception:

- **HTTP clients** — `IHttpClientFactory` (typed/named clients), never `new HttpClient()` per call. Exception: a single long-lived static client in a tiny tool.
  With Pivot: resilient typed clients via `services.AddServiceClient<TClient, TImpl>(o => …)` (retry + circuit breaker; set `RetryBaseDelaySeconds = 2` for exponential backoff — the default base 1 means a constant 1 s); user-token forwarding in Blazor/MAUI via `.AddKeycloakHandler()`.
- **Configuration** — bind to strongly-typed options via `IOptions<T>` / `IOptionsSnapshot<T>`; never read raw `IConfiguration["key"]` scattered through code.
- **Logging** — `ILogger<T>` with structured templates (`logger.LogInformation("Order {OrderId} failed", id)`), never string interpolation or concatenation into the message.
- **Async** — async all the way down; accept and propagate `CancellationToken`; `ConfigureAwait(false)` only in library code that can run under a UI synchronization context (Pivot omits it in server-side packages by convention); never `.Result` / `.Wait()` / `async void` (except event handlers).
- **DTOs / value objects** — `record` (or `record struct`) for immutable data; classes for entities with identity and behavior.
- **Config-bound and required data** — `required` members and `init` setters over mutable settable properties, where the TFM allows.
- **Nullability** — nullable reference types enabled project-wide; annotate intent rather than sprinkling `!`.
- **JSON** — `System.Text.Json` with configured `JsonSerializerOptions` for your own code; reach for Newtonsoft only for features STJ lacks. Exception inherited from Pivot.Framework: outbox, event-history and saga payloads are serialized with **Newtonsoft.Json** (`TypeNameHandling.None`), so domain/integration events and saga data must be Newtonsoft-round-trippable (settable properties or a `[JsonConstructor]`). Redis caches (`ICacheService`) use STJ camelCase.
- **Error handling** — exceptions for exceptional flow; Pivot's `Result`/`Result<T>` (`Pivot.Framework.Domain.Shared`) with `Error` + `ResultExceptionType` for expected failures in domain and application code. One policy per layer (see `clean-code-error-handling`).
- **DI lifetimes** — match lifetime to state: singleton for stateless/shared, scoped for per-request (e.g. `DbContext`), transient for lightweight. Never capture a shorter-lived service in a longer-lived one.
- **EF Core** — `AsNoTracking()` on read-only queries; project to DTOs with `Select` rather than loading whole entities; keep `DbContext` scoped; explicit transactions only when spanning multiple `SaveChanges`.
  With Pivot: writes go through `IUnitOfWork<TContext>.SaveChangesAsync` (never `DbContext.SaveChangesAsync` in handlers); write-side queries via `EntitySpecification<T,TId>` (no-tracking and soft-delete filtering by default); reads via `IReadModelRepository<T,TId>`; explicit transactions are owned by `TransactionMiddleware`/`GrpcTransactionInterceptor`, not by handlers.
- **Validation** — validate at the boundary with FluentValidation validators run by Pivot's `ValidationPipelineBehavior` (returns a `ValidationResult` → HTTP 400 with `validationErrors` via `ApiController`); don't add a second mechanism (data annotations, minimal-API `AddValidation`) for commands/queries; don't trust input deeper in the stack.
- **Time** — `TimeProvider` (or an injected clock) over `DateTime.Now`; `UtcNow` for storage and comparison.
- **Collections** — return the narrowest useful type; avoid exposing mutable internal collections; beware multiple enumeration of `IEnumerable`.

## Workflow

1. **Identify the workload and TFM.** Project type (ASP.NET Core, library, worker, Blazor, Functions, console), target framework, and whether the request is greenfield code, a refactor, or general guidance. The TFM gates which language features you can recommend.

2. **Apply the relevant standards.** Map the code to the idiomatic defaults above. Apply only what fits the workload — note where a practice would be overkill.

3. **Apply workload-specific guidance.**
   - **Web API** — minimal APIs or controllers consistently; model binding + boundary validation; consistent response/status-code shape; `ProblemDetails` for errors; auth via policies.
   - **Library / SDK** — clear, minimal public surface; immutability where possible; `ConfigureAwait(false)`; documented exception contracts; semantic versioning of the surface.
   - **EF Core / data access** — the data-access defaults above; migrations checked in; no business logic in the data layer.
   - **Tests** — arrange/act/assert, one behavior per test, isolated (no shared mutable state), meaningful assertions. If tests are missing, propose a focused plan for the highest-risk paths first.

4. **Deliver** using the output format below.

## Output format

For each recommendation:

- **Pattern** — the idiomatic choice, named.
- **Why** — the concrete benefit (1–2 sentences): what it prevents or enables.
- **Apply** — a minimal before → after or a target snippet showing the pattern in this code.

Group recommendations by area (architecture, async, data access, etc.) when there are several. End with prioritized next steps.

## Example

> **Pattern:** Inject `IHttpClientFactory` instead of constructing `HttpClient`.
> **Why:** Per-call `new HttpClient()` leaks sockets under load (`TIME_WAIT` exhaustion); the factory pools and rotates handlers.
> ```csharp
> // before
> using var client = new HttpClient();
> var res = await client.GetStringAsync(url);
>
> // after — registered: services.AddHttpClient<WeatherClient>();
> public WeatherClient(HttpClient client) => _client = client;
> var res = await _client.GetStringAsync(url, cancellationToken);
> ```

## Completion criteria

- The output identifies the workload and target framework.
- Each recommendation names the idiomatic pattern, gives a concrete reason, and shows a snippet.
- Recommendations fit the workload — no reflexive over-engineering.
- No recommendation requires language features the TFM can't support.
- Refactors preserve behavior unless a change is called out.
- The result ends with prioritized next steps.

## Example prompts

- "Refactor this .NET service to current best practices for async, config, and DI."
- "What's the idiomatic way to structure HTTP clients and options in this ASP.NET Core app?"
- "Bring this EF Core data layer up to standard for query shape and DbContext lifetime."
