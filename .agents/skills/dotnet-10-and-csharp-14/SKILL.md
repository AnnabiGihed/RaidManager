---
name: dotnet-10-and-csharp-14
description: 'Target and migrate to .NET 10 (LTS) with C# 14: TFM and LangVersion setup, the C# 14 feature set (field keyword,
  extension members, null-conditional assignment, partial constructors/events, nameof unbound generics, lambda modifiers),
  ASP.NET Core 10 changes (OpenAPI 3.1, minimal API validation, Blazor NotFound/QuickGrid/static asset changes), EF Core 10
  (named query filters, LINQ and performance), SDK changes (.slnx, file-based apps), and the .NET 8 end-of-support deadline
  of 10 November 2026. Use when scaffolding on .NET 10, upgrading a solution from .NET 8/9, or deciding whether a C# 14
  feature is available.'
---

.NET 10 is the current **LTS** release (November 2025, supported until **10 November 2028**). It ships with **C# 14**, **ASP.NET Core 10**, and **EF Core 10**.

**.NET 10 is mandatory for new projects** per the organization's technology standards: .NET 10 or later, **LTS versions only** (even-numbered — 10, 12, …). Never scaffold a new solution on .NET 8 or 9, and never adopt an odd-numbered STS (Standard Term Support) release. Pivot.Framework itself targets `net10.0` (set in its `Directory.Build.props`), so services built on it must target `net10.0` or later.

**The deadline that drives this:** .NET 8 *and* .NET 9 both reach **end of support on 10 November 2026**. Every solution in the estate must be on .NET 10 before that date — after it, no security patches. Treat migration as scheduled work with an owner, not as an opportunistic upgrade.

## Targeting .NET 10

- **TFM:** `net10.0`, set once in `Directory.Build.props`, never per project (see `clean-code-static-analysis` for the full props file).
- **LangVersion:** targeting `net10.0` selects C# 14 automatically — do **not** pin `<LangVersion>` unless deviating. If a project stays on an older TFM under the .NET 10 SDK, C# 14 syntax is *not* available there unless `LangVersion` is set explicitly; prefer moving the TFM over pinning the language version.
- **Package compatibility:** a `net10.0` application consumes `net8.0` libraries normally, but the reverse is not true: the `Pivot.Framework.*` packages target **`net10.0` only** (with EF Core 10, MediatR 14, RabbitMQ.Client 7, Npgsql 10). A solution still on .NET 8/9 cannot reference Pivot — migrating the TFM is a prerequisite for adopting it, not an optional step. Align your own `Microsoft.*` / EF Core package versions with the ones Pivot references to avoid downgrade warnings (NU1605).
- **Container images:** update base images to the .NET 10 tags in the same change as the TFM; a `net10.0` app on an 8.0 runtime image fails at startup.

## Migrating an existing solution (.NET 8/9 → .NET 10)

The jump from 8 or 9 is low-friction: most changes are in the runtime, SDK, and libraries rather than API removals. Sequence:

1. Bump the TFM in `Directory.Build.props` and the container base images together.
2. Update `Microsoft.*` / `Microsoft.EntityFrameworkCore.*` package versions to 10.x in `Directory.Packages.props` (central package management — see `clean-code-static-analysis`).
3. Restore, build, and run the full test suite. Fix analyzer warnings surfaced by the newer analyzers rather than suppressing them.
4. Record the upgrade as an ADR (framework/major-dependency choices require one) and add a CHANGELOG entry under `[Unreleased]`.
5. Optionally adopt C# 14 features afterwards, as a separate PR — never mix a framework upgrade with a refactor in one pull request (one PR, one concern).

Web Forms, WCF, and Windows Workflow Foundation remain unsupported; treat any remaining usage as migration debt tracked separately.

## C# 14 — what's available and when to use it

Use these where they remove ceremony, not for their own sake. Consistency within a file and with surrounding code wins over adopting a new feature mid-codebase.

- **`field` keyword (field-backed properties)** — access the compiler-generated backing field from an accessor, so an auto-property gains validation without a hand-written field:
  ```csharp
  public string Sku
  {
      get;
      set => field = value?.Trim() ?? throw new ArgumentNullException(nameof(value));
  }
  ```
  Caution: `field` is contextual. If a type already has a member named `field`, the keyword shadows it — rename the member.
- **Extension members (extension blocks)** — extensions are no longer limited to methods: properties, static members, and operators are now possible, grouped in an `extension` block inside a static class. Prefer one block per extended type; if an extension member's signature matches a real member of the type, the type's own member wins.
- **Null-conditional assignment** — `?.` and `?[]` on the *left* of an assignment or compound assignment:
  ```csharp
  customer?.Order = GetCurrentOrder();   // right side evaluated only when customer is non-null
  ```
  Works with `+=`, `-=`, etc. **Not** with `++`/`--`.
- **`nameof` with unbound generics** — `nameof(List<>)` returns `"List"` without supplying a type argument; useful in diagnostics and logging.
- **Lambda parameter modifiers** — `ref`, `in`, `out`, `scoped` on lambda parameters without repeating the parameter types.
- **Partial constructors and partial events** — exactly one defining and one implementing declaration each; only the implementing constructor declaration may carry a `this()`/`base()` initializer. Mainly for source-generator scenarios.
- **User-defined compound assignment operators** — a type can define `+=` etc. directly, avoiding an allocation per operation on large value types.
- **Implicit span conversions** — less ceremony converting arrays and spans in performance-sensitive code.

House rules still override: these features never justify putting logic in a `.razor` file, bypassing Pivot.Framework base types, or skipping the `Result` pattern. Match Pivot's own style when using them (e.g. primary constructors are used for repositories and publishers in Pivot; the `field` keyword is not used yet — adopt it consistently per file).

## ASP.NET Core 10

- **OpenAPI 3.1 documents** are generated natively — align the committed contract file with the 3.1 output when upgrading, and re-run the CI validation (the contract stays the single source of truth).
- **Minimal API validation**: `.AddValidation()` enables built-in validation on endpoints. In this estate, validation belongs to Pivot's `ValidationPipelineBehavior<,>` (Pivot.Framework.Application) + FluentValidation validators — do **not** introduce a second validation mechanism at the endpoint just because the framework now offers one.
- **Blazor** in .NET 10 (relevant to the UI layer):
  - The template includes a `NotFound.razor` page, and `NavigationManager.NotFound()` handles missing resources — 404 status under static SSR, Not Found content under interactive rendering.
  - `QuickGrid` gains a `RowClass` parameter for conditional per-row styling.
  - The Blazor script is served as a fingerprinted, compressed static web asset rather than an embedded resource; WebAssembly apps can preload static assets.
  - Enhanced form validation and improved diagnostics.
  - Passkey support in Identity — irrelevant here: authentication goes through Keycloak via the Pivot.Framework.Authentication packages (`pivot-auth-*`) regardless of new framework primitives.
- Memory pool eviction reduces idle memory in long-running server apps.

## EF Core 10

- **Named query filters** — multiple filters per entity type, each named and selectively disabled, instead of the single unnamed filter of earlier versions. Prefer named filters when a type carries both a soft-delete and a tenant/visibility filter, so one can be disabled without losing the other.
- LINQ translation improvements, better Cosmos DB support, and materialization/performance gains.
- Verify migrations after the upgrade: generate and inspect a no-op migration to confirm the model snapshot is unchanged before deploying.

## SDK and tooling

- **`.slnx`** replaces the legacy `.sln` format; migrate with `dotnet solution migrate`. Adopt it deliberately — confirm the CI agents and Visual Studio versions in use support it before converting.
- **File-based apps** (`dotnet run app.cs`) run a single `.cs` file without a project (and `dotnet publish app.cs` produces a Native AOT binary by default). Fine for throwaway scripts and spikes; **never** for a delivered application — the house structure applies to everything shipped.
- Aspire ships out-of-band from .NET, with its own versioning; check the Aspire release notes separately when upgrading, and keep the workload installed in the agent setup steps.

## Completion criteria

- The solution targets `net10.0` via `Directory.Build.props`, with container base images updated in the same change and no per-project TFM overrides.
- Framework and package versions moved together; the full test suite passes; analyzer warnings addressed, not suppressed.
- The upgrade is recorded in an ADR and the CHANGELOG, and shipped as its own PR separate from feature or refactoring work.
- C# 14 features are used only where they remove ceremony, and never to bypass a house standard.
- No second validation mechanism, no framework-native auth replacing the Pivot Keycloak integration, no file-based app as a deliverable.
