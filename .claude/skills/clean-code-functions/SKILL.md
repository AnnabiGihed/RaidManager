---
name: clean-code-functions
description: Clean Code rules for methods and control flow in C#. Use whenever you write, refactor or review a method, constructor body, handler or controller action. Covers doing one thing at one level of abstraction, complexity and nesting limits, guard clauses, named predicates, parameters (no boolean flags, no out/ref), command-query separation, not returning null, pure domain calculations, CancellationToken forwarding, no blocking on async, and dead or empty code — with Pivot.Framework Result idioms.
---

# Functions and control flow

Source: Development Standards Wiki → *Clean Code* → *Functions and Control Flow*. Cite rules as
`Functions and Control Flow / R<n>`. Severity and *Analyzer*/*Review* markers as in `clean-code-naming`.
Numeric thresholds live only in the shared analyzer configuration (see `clean-code-static-analysis`); never hard-code,
quote or suppress one to make code pass.

## 1. One thing, one level
- 🔴 **R1.** A method does one thing: you cannot extract another method whose name is more than a fragment of the parent's. *Review.*
- 🔴 **R2.** Never mix orchestration with mechanism (building SQL, mapping HTTP status, formatting) in one body. *Review.*
- 🟡 **R3.** Length is bounded; exceeding it means R1 or R2 is broken. *Analyzer:* S138, MA0051.
- 🟡 **R4.** A method reads top to bottom at one level of abstraction. *Review.*

```csharp
public async Task<SlaReport> BuildMonthlyReportAsync(YearMonth period, CancellationToken ct)
{
	var dossiers = await _dossiers.ClosedDuringAsync(period, ct);
	var buckets = ClassifyIntoSlaBuckets(dossiers);
	return Summarize(buckets, period);
}
```

Pivot reference: `UnitOfWork.SaveChangesAsync` orchestrates (`UpdateAuditableEntities` → `PersistDomainEventsToOutboxAsync` → `PublishMappedIntegrationEventsAsync` → save → clear) and delegates each mechanism to a `protected virtual` step.

## 2. Complexity and nesting
- 🔴 **R5.** Cognitive complexity bounded; decompose, never annotate. *Analyzer:* S3776.
- 🔴 **R6.** Cyclomatic complexity bounded. *Analyzer:* S1541.
- 🔴 **R7.** Nesting depth bounded. *Analyzer:* S134.
- 🔴 **R8.** Guard clauses at the top: check, return or throw, happy path unindented at the end, no `else` after a guard. Guards keep braces (`csharp_prefer_braces = true:error`) in new code. *Review,* S3626.
- 🟡 **R9.** Complex conditions become named predicates on the type that owns the data: `if (dossier.IsOverdueAndActionable(today))`. *Analyzer:* S1067.
- 🟡 **R10.** No nested ternaries. *Analyzer:* S3358.

```csharp
public async Task<Result<DossierDecision>> Handle(DecideDossierCommand command, CancellationToken ct)
{
	var dossier = await _dossiers.FindByIdAsync(new DossierId(command.DossierId), ct);
	if (dossier is null)
	{
		return Result.Failure<DossierDecision>(DossierErrors.NotFound, ResultExceptionType.NotFound);
	}

	var decision = dossier.Decide(command.Outcome, command.Motivation, _time.GetUtcNow().UtcDateTime);
	if (decision.IsFailure)
	{
		return decision;                                   // keeps Error and ResultExceptionType (e.g. Conflict)
	}

	var saved = await _unitOfWork.SaveChangesAsync(ct);
	return saved.IsFailure
		? Result.Failure<DossierDecision>(saved.Error, saved.ResultExceptionType)
		: decision;
}
```
(Pivot has no `Result.NotFound(...)`/`Result.Conflict(...)` shortcuts — use `Result.Failure(error, ResultExceptionType.X)`.)

## 3. Parameters
- 🔴 **R11.** Parameter count bounded. *Analyzer:* S107.
- 🔴 **R12.** No behaviour-selecting boolean parameters in public/internal APIs; use two methods, an enum or an options object. *Review,* S2360. (Framework-level exception: registration flags like `AddEfCoreWritePersistence(includeEventStore: true)` — always pass them by name.)
- 🔴 **R13.** No `out`/`ref` except `TryX` or justified performance code; return a tuple, record or `Result`. *Analyzer:* CA1021.
- 🟡 **R14.** Parameters that always travel together become a type. *Review.*
- 🟡 **R15.** Order: subject, collaborators, options, `CancellationToken` last. *Analyzer:* CA1068.
- 🟡 **R16.** Never mutate parameters. (Saga steps are the documented exception: `ISagaStep.ExecuteAsync` may write into the saga data object so compensation can use it.) *Review.*

## 4. Return values and side effects
- 🔴 **R17.** Command-query separation: return a value **or** change state. Allowed exceptions: `TryX`, a create returning the id/aggregate, a bulk op returning a count, and Pivot's `Result`-returning domain behaviours (the `Result` reports whether the mutation happened). *Review.*
- 🔴 **R18.** A query-sounding name never mutates. *Review.*
- 🔴 **R19.** Never return `null` for "not found" without saying so in the type: `TryX`, `Task<T?> FindByIdAsync`, or `Result` with `ResultExceptionType.NotFound`. *Analyzer:* CS8603.
- 🟡 **R20.** Domain calculations are pure: no I/O, clock, randomness or environment — inject `TimeProvider`, id generators, options. (Framework primitives such as `DomainEvent()` and `UnitOfWork` audit stamping use `DateTime.UtcNow`/`Guid.NewGuid()` by design; your domain code passes time in.) *Review.*
- 🟡 **R21.** One meaningful exit for the happy path; guard returns at the top are fine. *Review.*

## 5. Async shape
- 🔴 **R22.** Never block on async outside `Main`/startup. *Analyzer:* CA1849, MA0042. (Known framework exception: `HangfireCookieDashboardAuthorizationFilter` — Hangfire's filter API is synchronous; don't copy the pattern elsewhere.)
- 🔴 **R23.** Every I/O method accepts a `CancellationToken` and forwards it to every call. *Analyzer:* CA2016, MA0032, MA0040.
- 🟡 **R24.** Validate arguments of an `async` method eagerly. *Analyzer:* S4457.

## 6. Dead and defensive code
- 🔴 **R25.** Remove unused parameters, locals, unreachable branches and dead stores. *Analyzer:* S1172, S1481, S1854, CA1508.
- 🔴 **R26.** No empty `catch`/`if`/method body without a comment explaining the deliberate no-op. *Analyzer:* S108, S1186.
- 🟡 **R27.** Don't null-check what the nullable type system excludes (constructor guards on injected dependencies stay — DI callers bypass nullability). *Review.*

## 7. Never
- `#pragma warning disable S3776` instead of decomposing.
- A controller action doing data access, business rules, mapping and logging — controllers only `Sender.Send(...)` and `HandleResult(...)`.
- `Process(dossier, true, false)`.

## 8. Checklist
- [ ] Each method does one thing at one level.
- [ ] Guards at the top, flat happy path, no nested ternaries, complex conditions named.
- [ ] No boolean flags, no `out`/`ref` outside `TryX`, `CancellationToken` last and forwarded.
- [ ] Queries don't mutate; "not found" is explicit (`T?`, `TryX`, or `Result` + `NotFound`); `ResultExceptionType` propagated.
- [ ] No `DateTime.Now`/`Guid.NewGuid()`/environment access in your domain logic.
- [ ] No dead code, no unexplained empty bodies.
