---
name: dotnet-unit-tests
description: 'Write .NET unit and component tests in the house format for services built on Pivot.Framework — Reqnroll (Gherkin) feature files with step definitions on xUnit, Shouldly and Moq for business behaviour (aggregates, value objects, handlers, projections), plain xUnit for technical tests, bUnit for Blazor components, and what to assert on Pivot types (Result, ResultExceptionType, domain events, Version). Also states the different stack used inside the Pivot.Framework repository itself. Use whenever authoring or restructuring unit or component tests.'
---

Use this skill to write unit and component tests that verify behaviour, read clearly and don't rot into a catch-all
pile. For integration/E2E against real dependencies use `dotnet-e2e-tests`. The exact Gherkin and binding rules are in
`gherkin-scenarios`; this skill decides *what* is tested *how*, and where it lives.

## Which stack applies

| Where | Business behaviour | Technical tests | Assertions / mocks |
|---|---|---|---|
| **Services built on Pivot.Framework** (house standard) | **Reqnroll** feature + step definitions (xUnit runner, `Reqnroll.xUnit`) — mandatory | plain xUnit `[Fact]`/`[Theory]` | **Shouldly**, **Moq** (shared mock factories) |
| **Pivot.Framework repository** | n/a (framework code has no business language) | xUnit | FluentAssertions, NSubstitute, EF Core InMemory test contexts — see `pivot-testing` |

Never mix the two stacks inside one repository. In a service, do not introduce FluentAssertions/NSubstitute; in the
framework repo, do not introduce Reqnroll/Shouldly/Moq.

**Scope rule (services).** Reqnroll is how domain behaviour is tested — aggregates, invariants, value objects, use-case
business rules in handlers, projections — because those tests must read as business language. Plain `[Fact]`/`[Theory]`
for *business* behaviour is **forbidden**. xUnit is mandatory for **technical** tests: persistence mappings, EF
configurations, serialization (e.g. event Newtonsoft round-trips), DI/composition-root wiring, routing resolvers,
anything with no business rule to express. If you can't write the scenario in the business's words, it's a technical test.

## Reqnroll format (services)

- Read the canonical example set in this skill first and reproduce its structure exactly:
  `examples/ProductCreation.feature`, `examples/ProductCreationStepDefinitions.cs`, `examples/MockProductRepository.cs`.
- Feature files follow `gherkin-scenarios` exactly: one `@workitem:<id>` tag, three-line narrative, every scenario inside a `Rule`, Cucumber expressions with `{int}`/`{string}` only, file named after the Feature title, step class `<FeatureFileName>StepDefinitions` (`sealed`, `[Binding]`, `[Scope(Feature = "…")]`).
- `Background` establishes a **fully valid default state** (typically a vertical `| field | value |` table) so each scenario states only its deviation.
- The **first scenario of each Rule is its happy path** and asserts the full success contract. For a Pivot aggregate factory that is: `Result.IsSuccess`, the specific domain event raised (`GetDomainEvents().OfType<XCreatedDomainEvent>().ShouldHaveSingleItem()` with its payload), and the generated strongly typed id (`Id.Value != Guid.Empty`). Do **not** assert `Audit` after creation — Pivot's `UnitOfWork` stamps `AuditInfo` at save time, so it is `null` on a freshly created aggregate unless the aggregate calls `InitializeAudit` itself.
- Then **one scenario per business rule violation**, asserting the **exact** `Error.Message` from the domain's catalogue (`BaseDomainErrors` or the aggregate's `<Aggregate>Errors`). Copy messages character for character — Pivot's catalogue messages have irregular spacing (`Value  PRD-0001 for product sku  already exists !`). Where the failure type matters, also assert `ResultExceptionType` (e.g. `Conflict` for a forbidden transition) with a separate `Then`/`And` step.
- Reqnroll's generated `*.feature.cs` is build output: never edit it.

### Step definitions
- Pivot-style type header and regions (`csharp-xml-documentation`, `csharp-regions`): `Fields` → `Constructors` → `Given Steps` → `When Steps` → `Then Steps` → `Private Helpers`.
- Every field carries a `<summary>` (test types are documented like production code).
- The constructor builds mocks from the shared factories in `{Solution}.Shared.UnitTests.Mocks` — one static factory per Domain repository interface (the interface extends Pivot's `IAsyncCommandRepository<TAggregate, TId>`), backed by a list so `ExistsAsync`/`FindByIdAsync` behave realistically. No EF InMemory, no database.
- Prerequisite aggregates are created **through their real factory methods**, never by setting state.
- Given arranges, When calls the factory/behaviour/handler and captures the `Result`, Then asserts with Shouldly. Async code is awaited in `async Task` steps — never `.Result`/`.Wait()`.

### Placement and naming
- Test projects mirror source projects in the `test/` tree: `{Solution}.Domain.UnitTests`, `{Solution}.Application.UnitTests`, … and folders mirror source folders (`Features/<Feature>/Aggregates/`), the `.feature` and its step class side by side.
- **A UI project without its mirrored test project is an incomplete delivery** — `{Solution}.Web`/`Shared`/`ViewModels` require `test/Containers/UI/…` projects in the same PR (view models as plain classes with Reqnroll or xUnit per the scope rule, components with bUnit).

## What to assert, by unit type

- **Aggregate / domain behaviour** — the behaviour returns `Result.Failure` with the exact error (and `ResultExceptionType`) when an invariant is violated, and on success mutates state, raises the right `DomainEvent` with the right primitive payload, and increments `Version` by the number of events raised.
- **Value object** — `Create` returns failures with exact messages for invalid input; equality by value; formatting (`ToString`).
- **Command handler** — thin wiring, with mocked repository and `Mock<IUnitOfWork<TContext>>` (`SaveChangesAsync` → `Result.Success()`): loads the aggregate, invokes one behaviour, calls `SaveChangesAsync` once, returns the expected `Result`; missing aggregate → `ResultExceptionType.NotFound`; unit-of-work failure is propagated with its `ResultExceptionType`. Don't re-test aggregate rules here.
- **Validator** — FluentValidation `TestValidate` (technical-style xUnit is acceptable for pure shape validation; business rules live in the aggregate).
- **Query handler / read side** — given a mocked `IReadModelRepository<T,TId>` (or specification), returns the expected DTO shape; read-only.
- **Projection** — `ProjectAsync` upserts the expected read model via a mocked `IReadModelStore<T,TId>`; replaying the same event twice yields the same model (idempotent); inside `ReplayContext.BeginReplayScope()` no external side effect is invoked.
- **Integration-event mapper** — `Map` returns the expected `IntegrationEvent`s (technical xUnit if purely structural, Reqnroll if it encodes a business rule).
- **Idempotency** — an `IIdempotentCommand` processed twice with the same key produces one effect (mock `IInboxService`).
- **Event serialization** (technical) — events that cross the transport round-trip through Newtonsoft (`TypeNameHandling.None`) with `Id` and `OccurredOnUtc` preserved.

## Rules
- Test behaviour, not implementation: observable outcomes (returned `Result`, raised events, rendered output), never private fields or call sequences.
- One behaviour per scenario/test; isolated; no shared mutable state; no real I/O.
- Meaningful assertions — never just "did not throw".
- Mock owned boundaries only (repositories, `IUnitOfWork<T>`, external clients, `TimeProvider` via `FakeTimeProvider`); never mock Pivot's `Result`/aggregate base classes or the unit under test.
- Structure by bounded context and feature; one feature file per capability; no catch-all classes.
- Technical xUnit tests are named `Method_Condition_ExpectedResult` and documented with a `<summary>`.
- Cover the highest-risk paths first.
- **Never race a fake clock.** Advancing a `FakeTimeProvider` completes a delay, but the code after it may run on
  another thread after `Advance` returns, and a test that asserts at once is flaky on CI (#514). Wait for the code
  under test, not for time: count timers once they have a due time and wait until a new one is armed or the flow
  ends. `FlowClock`, `DispatcherFlowClock` (`avalonia-tests` §5) and the website's `ArmedTimeProvider`
  (`PairedCompanionsTests`, #527) do it. Prove a timing fix with a stress run, not one green run.
- **Start a fake clock at the real time when the domain checks the real clock.** `Character` refuses a timestamp more
  than five minutes after `DateTimeOffset.UtcNow`, so a `FakeTimeProvider` set to a fixed noon made a capture one hour
  earlier look like the future (#384). Use `new FakeTimeProvider(DateTimeOffset.UtcNow)`, and give a repeated upload
  the same snapshot object instead of recomputing "one hour ago" after `Advance`.
- **Parentheses in a cucumber expression mean optional text.** `[When("validated without a (user id|character id)")]`
  never matched, and Reqnroll reported the step as pending (#385). Pass the choice as `{string}` from the feature file
  instead of reaching for a regular expression.
- **Don't commit regenerated `.feature.cs` files of features you didn't touch.** A build renumbers Reqnroll's table
  variables (`table3` to `table11`) in every generated file; restore those with `git checkout --` before staging
  (#385, eight files again in #553), and commit only the files of features you changed.
- **Coverage is gated, and the gate is mandatory.** Every test project references `coverlet.collector`, and CI runs
  the tests with the repository's coverage settings (`coverage.runsettings`: tests, migrations and generated code
  excluded). A pull request must cover at least **80% of its changed coverable lines**, and total line coverage must
  stay at or above the repository's floor (60% unless its ADR says otherwise). Check it locally before pushing and add
  tests for every uncovered changed line the summary lists. Never lower a threshold, widen an exclusion, or add
  `[ExcludeFromCodeCoverage]` to pass the gate; the attribute is only for code that genuinely can't run under test,
  with a comment saying why.

## Avalonia (desktop companion)
- The companion's tests follow `avalonia-tests`: its client tests use this skill's stack (xUnit, Reqnroll for business rules, Shouldly, Moq); its host tests run views headless with `Avalonia.Headless.XUnit` on xUnit v3 (ADR-0032).

## bUnit (Blazor components)
- Render with a bUnit `TestContext`; assert on markup via `Find`/`FindAll`, not component fields.
- Trigger events through markup; assert the resulting render or the `EventCallback`.
- Radzen components need their services and JS: call `Services.AddRadzenComponents()` and set `JSInterop.Mode = JSRuntimeMode.Loose` on the bUnit context; assert on Radzen's rendered markup (e.g. `.rz-data-row`, `.rz-notification`) or, better, on the view model state the component drives.
- Register fakes for injected services (view models, `IBlazorKeycloakAuthService` as a Moq mock, a test `AuthenticationStateProvider` for `<AuthorizeView>`); supply parameters through the render builder.
- Stub JS interop with bUnit; no real browser. Render-mode behaviour is integration-level (see `blazor-components`).

## Workflow
1. Decide the stack (service vs framework repo) and business vs technical.
2. Read the examples; find or create the right feature/step pair (or xUnit class) in the mirrored folder.
3. Pick the unit type and its assertions from above.
4. Arrange minimally with shared mock factories; act through the public surface; assert observable outcomes.
5. Run the `gherkin-scenarios` checklist for feature files.

## Output format
1. **Target & file** — what's under test and which file(s).
2. **Tests** — the feature (Rules/scenarios) + step definitions, or xUnit tests.
3. **Notes** — what was mocked and why; edge cases covered or deferred.

## Completion criteria
- Correct stack for the repository; business behaviour in Gherkin (services), technical in xUnit.
- Feature files pass the `gherkin-scenarios` checklist; step classes follow Pivot-style headers/regions.
- Happy path asserts success, event and id (not `Audit`); failures assert exact messages (and `ResultExceptionType` where relevant).
- Only owned boundaries mocked, via shared factories; no EF InMemory or database in unit tests.
- Idempotency, replay-safety and event round-tripping asserted where the code relies on them.
- UI deliveries come with their mirrored test projects.
- The coverage gate passes: at least 80% of the changed lines covered and the total at or above the floor, with no
  new exclusion added to get there.

## Example prompts
- "Write the Gherkin scenarios and step definitions for the Product aggregate's creation rules."
- "Add handler tests for RegisterPaymentCommandHandler with a mocked repository and unit of work."
- "Add bUnit tests for this component's event handling and rendered output."
