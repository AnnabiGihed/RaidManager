---
name: dotnet-code-review
description: Perform a structured .NET code review that finds architecture, correctness, security, performance, and testing
  issues, and reports them with severity, location, and concrete fixes. Use whenever reviewing a pull request, auditing existing
  C#/.NET or Blazor code, self-reviewing changes before finalizing a PR, or when asked to check, review, or assess code quality.
---

Use this skill to run a consistent, repeatable .NET code review. The goal is a review another engineer could act on without asking follow-up questions: every finding names a location, explains why it matters, and proposes a fix.

## Rules

- Review only the code provided. Never invent findings for files or code you cannot see. If critical context is missing (DI composition, config, the calling code, target framework), state what is missing and ask for it rather than guessing.
- Every finding must cite a location (`File.cs:42` or method/class name) and the specific code it refers to. No location, no finding.
- Do not report style or formatting that an analyzer, linter, or `.editorconfig` already enforces (spacing, `var` usage, brace placement). Assume the build catches these.
- Do not pad. If a review dimension has no real issues, write "No issues found" for it. Never manufacture findings to fill a section.
- Prefer the smallest correct fix. Recommend rewrites only when the smaller fix can't address the root cause, and say why.
- Assign exactly one severity to every finding using the rubric below. Order all findings by severity, highest first.
- When tests are absent, propose at most 5 test cases covering the highest-risk paths. Do not scaffold a full suite unless asked.
- Separate facts from judgment. Mark anything you can't verify from the code as an assumption (e.g. "assuming this runs in a request scope").
- Keep fix snippets minimal — enough to show the pattern, not a full reimplementation.

## Severity rubric

Assign one level per finding:

- **Critical** — security hole, data loss/corruption, or a correctness bug on a production path. Examples: SQL injection, secrets in source, async deadlock under load, broken authorization check. Ship-blocking.
- **High** — likely to cause incidents or significant tech debt: resource exhaustion, N+1 on a hot path, swallowed exceptions hiding failures, missing input validation on a public endpoint. Fix before merge.
- **Medium** — degrades maintainability, performance, or robustness but won't break production soon: missing `CancellationToken` propagation, weak error handling, unclear contracts, missing tests on important logic.
- **Low** — minor improvements and nits not caught by tooling: naming, small refactors, doc gaps. Optional.

When unsure between two levels, pick the lower one and note the condition that would raise it.

## Review workflow

1. **Understand the project.** Identify the target framework and runtime, project type (ASP.NET Core, console, class library, Blazor, MAUI, Azure Functions, worker), entry points, DI composition root, and folder structure. State these up front so the reader knows what was assumed.

2. **Set scope.** Determine whether you're reviewing a whole solution, one project, a feature, or a diff. Confirm the emphasis: broad architecture, code quality, security, performance, or test coverage. If the user didn't specify, do a balanced review across all dimensions.

3. **Assess each dimension** (see "What to look for"). Skip a dimension only if it's genuinely irrelevant to the code under review, and say so.

4. **Apply workload branching.**
   - **ASP.NET Core / Web API** — endpoint and route design, middleware order, model binding and validation, response/status-code consistency, auth filters, exception-handling middleware.
   - **EF Core / data access** — query shape and translation, tracking vs `AsNoTracking()`, N+1, migrations, transaction boundaries, connection/`DbContext` lifetime.
   - **Library / SDK** — public API ergonomics, immutability, nullable annotations, exception contracts, `ConfigureAwait(false)`, semantic versioning of the surface, XML docs.
   - **Tests present** — structure, isolation, meaningful assertions (not just "no throw"), edge cases, naming.
   - **Tests absent** — recommend a minimal suite per the Rules.

5. **Report** using the output format below.

## What to look for

General dimensions: architecture and layering (SOLID, separation of concerns, domain boundaries, DI), correctness (nullability, exception handling, state, thread safety), security (secrets, authn/authz, injection, validation, secure defaults), performance (allocations, queries, caching, I/O, resource cleanup), and observability (logging, telemetry, health checks).

.NET-specific issues to actively hunt for — these are the high-value findings:

- `async void` outside event handlers, and sync-over-async (`.Result`, `.Wait()`, `.GetAwaiter().GetResult()`) that can deadlock.
- `new HttpClient()` per call (socket exhaustion) instead of `IHttpClientFactory`.
- Captive dependencies — a singleton capturing a scoped or transient service.
- EF Core: N+1 queries, missing `AsNoTracking()` on read-only paths, client-side evaluation, loading more columns/rows than needed, long-lived or shared `DbContext`.
- `CancellationToken` accepted but not propagated through the async chain (or not accepted at all on cancellable operations).
- Missing `ConfigureAwait(false)` in library code that can run under a UI `SynchronizationContext` (MAUI, WPF, WinForms). Server-only libraries (ASP.NET Core, workers — all of Pivot.Framework except `Authentication.Maui`) have no synchronization context; Pivot omits `ConfigureAwait(false)` there by convention, so do not flag it.
- Multiple enumeration of an `IEnumerable` (deferred execution run twice).
- `DateTime.Now` where `DateTime.UtcNow` (or `DateTimeOffset`) is correct.
- String-concatenated or interpolated SQL instead of parameters.
- Secrets in `appsettings.json` or source instead of Key Vault / user-secrets / environment.
- Swallowed exceptions (`catch {}`), catching `Exception` too broadly, or logging-and-rethrowing that loses the stack.
- `IDisposable` not disposed (missing `using`), and `IAsyncDisposable` ignored.
- Nullable reference types disabled or annotations ignored where null is reachable.
- **Blocking:** an API host without an interactive reference page (Scalar or Swagger UI) mapped at least in Development, a page removed or hidden from Development, two OpenAPI generators in one host, or no test asserting the page answers. Also Blocking: a new or renamed host whose AppHost resource has no "API reference" link (`dotnet-solution-scaffolding`).

## Output format

Produce the report in this structure:

1. **Summary** — 2–4 sentences: what was reviewed, the project type/runtime assumed, overall assessment, and the count of findings by severity.
2. **Findings** — grouped by severity (Critical → Low). For each finding:
   - **[Severity] Short title** — `File.cs:line`
   - *What:* the issue in one or two sentences.
   - *Why it matters:* impact, concretely.
   - *Fix:* a concrete recommendation, with a minimal code snippet when it clarifies.
3. **Missing tests / risks** — gaps and the minimal test cases to close them.
4. **Next steps** — an ordered, actionable list of what to do first.

## Example finding

> **[High] Sync-over-async blocks the request thread** — `OrderService.cs:88`
> *What:* `GetOrderAsync(id).Result` blocks synchronously on an async call inside a request handler.
> *Why it matters:* Under load this can exhaust the thread pool and deadlock in some sync contexts; it negates the benefit of the async API.
> *Fix:* make the caller async and await it.
> ```csharp
> // before
> var order = _service.GetOrderAsync(id).Result;
> // after
> var order = await _service.GetOrderAsync(id, cancellationToken);
> ```

## Completion criteria

- The review states the project type, runtime, and any assumptions made.
- Every finding has a severity, a location, and a concrete fix.
- Findings are ordered by severity and contain no analyzer-level nits.
- At least one primary dimension (architecture, correctness, security, performance, or testing) is covered in depth.
- Missing tests, unsafe patterns, and architectural risks are called out.
- The review ends with prioritized next steps.

## Example prompts

- "Review this ASP.NET Core Web API and flag security, performance, and code quality issues."
- "Inspect this .NET class library for public API design and test coverage gaps."
- "Review this EF Core data access layer, focused on query shape and async usage."

## Authentication check (Pivot.Framework / Keycloak)

Applies only where an accepted ADR selects Keycloak. RaidManager authenticates with Discord OAuth (ADR-0001):
there, review the Discord integration against that ADR instead, and the Keycloak items below do not apply
(see `raidmanager-conventions`).


- **Blocking:** authentication wiring that bypasses the Pivot.Framework Keycloak integration — raw `AddJwtBearer`/`AddOpenIdConnect` for the API scheme, hand-rolled token validation or role parsing, lookalike reimplementations of `AddKeycloakAuthentication`, `AddKeycloakBlazor`, `AddKeycloakMaui` or `AddHangfireKeycloakBrowserAuth` instead of the real packages (`pivot-auth-*`). Remains blocking even with an ADR that grants itself a waiver, a "documented fallback" remark, or config keys mirroring Keycloak's.
- **Blocking:** calls to APIs that don't exist publicly — `AddKeycloakBackend` (internal), `AddKeycloakRedisCache`, `UseExceptionHandling`, `UseTransactions`, `InstallServices` (see the drift table in `pivot-framework`); code that "works" only because someone added a lookalike helper.
- **Blocking:** token-minting endpoints or ephemeral signing keys in an application (`/dev/token` and the like, Development-only or not); a hand-written identity server instead of a Keycloak dev container orchestrated by the Aspire AppHost; the AppHost excluded from the solution build to work around tooling.
- **Blocking:** an Aspire AppHost without `Properties/launchSettings.json` (`https`/`http` profiles, `launchBrowser`, fixed dashboard/OTLP/resource-service ports) or without a `UserSecretsId`; a secret passed to a host through `AddParameter` without `secret: true`; any credential, client secret, connection-string password or placeholder password committed in `appsettings*.json` (placeholders teach the habit and get deployed). A change to AppHost or host startup configuration whose PR shows no evidence the AppHost was started.
- `Keycloak:Audience` missing/wrong (every request 401), `RequireHttpsMetadata: false` outside Development, secrets (`ClientSecret`, `AdminClientSecret`, `RabbitMQ:EncryptionKey`) in committed `appsettings*.json`.
- `/auth/introspect` from `MapAuthenticationApi` exposed without `RequireAuthorization`; refresh tokens returned to browsers.
- Logout that doesn't revoke (`ITokenRevocationCache.RevokeAsync` with Redis caching, or Keycloak revocation for refresh tokens).

## Pivot.Framework usage findings

- **High:** a second outbox drain mode, or both transports registered; `UseImmediateOutboxDraining` placed inside (after) `TransactionMiddleware`; `ExceptionHandlerMiddleware` not first.
- **High:** handler calling `DbContext.SaveChangesAsync` directly instead of `IUnitOfWork<T>.SaveChangesAsync` (bypasses audit + outbox); calling `SaveChangesAsync` twice per command.
- **High:** `UnitOfWork` subclass that doesn't forward `IIntegrationEventMappingCoordinator<TContext>?` while `AddIntegrationEventMapping` is registered (mapping silently disabled).
- **High:** events crossing RabbitMQ/in-process transport without an id-preserving `[JsonConstructor]` while the inbox is relied upon for dedup (framework defect — see `pivot-domain` §4).
- **Medium:** `Result.Failure` conversions that drop `ResultExceptionType` (turns 404/409 into 400); `IIdempotentCommand` on an `ICommand<T>` (cast failure on duplicates).
- **Medium:** `OutboxMessage`/inbox/event-history tables not mapped in the DbContext; aggregate `Version` not persisted with the event store enabled; missing strongly-typed-id value converters.
- **Medium:** projections that insert instead of upsert, or perform side effects without checking `ReplayContext.IsReplaying`; domain event handlers returning `Result.Failure` expecting a retry (MediatR discards it — throw instead).
- **Medium:** in-process transport with integration events or `EmitFailureEvent = true`.

## Blazor UI findings

- Registered-but-unapplied interactivity (interactive services registered, no `@rendermode` anywhere) — the decision must be applied, verified, and recorded in an ADR.
- `IHttpContextAccessor` used on paths reachable from an interactive circuit — note that `KeycloakAuthService` (Pivot Blazor auth) reads its session cookie through `IHttpContextAccessor`, so `InitialiseFromCookieAsync` must run during prerender/static render.
- A second `/auth/callback` page, or none: Pivot's `KeycloakCallback.razor` is **not** compiled into the package, so exactly one app-owned callback page must exist.
- **Blocking:** any `@code` block in a `.razor` file — all C# belongs in the `.razor.cs` code-behind; logic in code-behinds instead of view models; a hollow ViewModels project.
- `CancellationToken.None` passed from components; missing `ErrorBoundary`; per-page repeated `@using` instead of `_Imports.razor`; exposed-but-bypassed members.
- UI source projects delivered without their mirrored bUnit/view-model test projects.
- Bootstrap CSS/JS or classes (`row`, `col-*`, `btn`, `d-flex`, …), DIFA or any component library other than **Radzen**; more than one Radzen theme; client-side paging of unbounded lists instead of `RadzenDataGrid` `LoadData`; `DialogService`/`NotificationService` used without `<RadzenComponents />` in the layout.

## Domain modeling findings

- Bare `Guid` identifiers on aggregates or entities instead of `StronglyTypedGuidId<TSelf>` records (or a hand-written `IStronglyTypedId<TSelf>` whose `ToString()` doesn't return the raw value — the event store records it as `AggregateId`).
- Aggregates not deriving from the Pivot hierarchy (`AggregateRoot`/`AuditableAggregateRoot`/`LightweightAggregateRoot`); public setters; behaviours that throw for anticipated failures instead of returning `Result`.
- Free `string` where a fixed value set exists (statuses, natures, directions) instead of an enum.
- Codes and constrained strings (enterprise number, registration number, references) as raw strings instead of validating value objects (`ValueObject<T>` + `Result<T> Create`).
- Parallel `…Fr`/`…Nl` fields instead of a translation value object; amounts without currency instead of a `Money` value object. Pivot ships no `Money`/`Translation` types (the old `BaseDomainErrors.ValueObjects.Money` is obsolete) — define them once per domain in `Features/Shared/ValueObjects`, not ad hoc per feature.
- Domain events carrying id records or entities instead of primitives (they are JSON-serialized into the outbox).
- Domain types leaking as primitives *inside* the domain — primitives belong at the DTO edge only.
