---
name: raidmanager-conventions
description: 'RaidManager-specific precedence rules over the imported house and Pivot.Framework skills. Use at the start
  of every RaidManager task, and whenever another skill mentions Keycloak, RabbitMQ, FAVV-AFSCA, Azure DevOps, tabs,
  the PACKAGES_READ_* variables, GitFlow develop branches, or organization templates — it states which of those rules
  apply here, which are replaced by accepted ADRs or enforced repository configuration, and which conflicts are still
  open and must be asked about.'
---

# RaidManager conventions and skill precedence

Most skills in `.agents/skills/` were imported from the FAVV-AFSCA house standards and the Pivot.Framework
repository. They stay the default, but RaidManager is a single-owner product (owner and author: Gihed Annabi) whose
accepted ADRs and enforced configuration win where they differ. Precedence, highest first:

1. Accepted ADRs in `docs/adr/`.
2. Configuration enforced by tooling in this repository: `.editorconfig`, `Directory.Build.props`,
   `Directory.Packages.props`, `stylecop.json`, `.github/workflows/*.yml`.
3. This skill, `raidmanager-project-packaging` and `wow-addon-335a-lua`.
4. The imported house skills and the `pivot-*` skills (a `pivot-*` skill wins on how the framework behaves).

## 1. Replaced by accepted decisions

- **Authentication is Discord OAuth only** (ADR-0001). Keycloak is not used. Ignore every Keycloak requirement in
  `architecture-proposal`, `blazor-components`, `dotnet-code-review`, `dotnet-solution-scaffolding`,
  `dotnet-e2e-tests`, `dotnet-10-and-csharp-14` and `repository-readiness`. The `pivot-auth-*` skills do not apply.
  Implement Discord sign-in with ASP.NET Core authentication (`AddAuthentication().AddCookie().AddOAuth(...)` or a
  vetted Discord handler recorded in an ADR); that is not a "bypass" finding.
- Note: `Pivot.Framework.Containers.API` references `Pivot.Framework.Authentication.AspNetCore` and `.Caching`, so
  those assemblies arrive transitively. Do not call `AddKeycloakAuthentication` or the Containers.API overload that
  wraps it; use the non-auth helpers only.
- **Character sync goes through the addon and a desktop companion** (ADR-0002). Addon work follows
  `wow-addon-335a-lua`; the companion uploads to the API and never stores secrets in addon files.
- **Messaging:** RaidManager has no broker today. RabbitMQ and the outbox transport are not part of the mandatory
  frame here; introducing a broker, an outbox drain mode or a second service needs its own ADR.

## 2. Resolved by enforced repository configuration

| Imported skill says | RaidManager uses | Source of truth |
| --- | --- | --- |
| Indent C# and Gherkin with tabs (`csharp-regions`, `clean-code-static-analysis` R4, `gherkin-scenarios` §10) | 4 spaces for every file | `.editorconfig` + `dotnet format --verify-no-changes` in CI |
| `PACKAGES_READ_USER` / `PACKAGES_READ_TOKEN` | `PIVOT_PACKAGES_USER` / `PIVOT_PACKAGES_TOKEN` | `nuget.config`, `ci.yml`, README |
| Skills kit under `.claude/skills/` or `.github/skills/` | `.agents/skills/` and an identical `.claude/skills/`; apply every change to both in one commit | `AGENTS.md` |
| SonarAnalyzer.CSharp + Meziantou.Analyzer + SonarCloud gate | .NET analyzers + StyleCop only; no SonarCloud project yet | `Directory.Build.props` (adding the others needs an ADR, `clean-code-static-analysis` R1) |
| Azure DevOps pipelines and wiki, `::: mermaid` fences | GitHub Actions, MkDocs Material, fenced `mermaid` blocks | `.github/workflows/`, `mkdocs.yml` |
| Keep Mermaid to the Azure DevOps subset | Still keep to that subset: it renders everywhere | `docs-diagrams-as-code` |
| Swashbuckle via Pivot's Keycloak Swagger setup | `Microsoft.AspNetCore.OpenApi` (`AddOpenApi`), one generator only | `RaidManager.ApiService/Program.cs` |
| Test projects `{Project}.UnitTests`, mocks in `{Solution}.Shared.UnitTests.Mocks` | `{Project}.Tests` mirrored under `test/`; ask where shared mock factories live when the first one is needed | solution layout |

## 3. Organization references that do not exist here

- `favv-afsca/engineering-configs`, `favv-afsca/template-<stack>`, `favv-afsca/dev-standards-wiki`, the architecture
  guild, team channels and a technical writer do not exist. The local configuration files are the shared config;
  the owner is the reviewer. Do not invent contacts or URLs (`docs-root-files`).
- The LICENSE is proprietary to Gihed Annabi, not the FAVV-AFSCA default text.
- Domain vocabulary is the RaidManager glossary (`docs/glossary.md`), in English: character, loadout, raid lockout,
  raid signup, roster selection, community, eligibility. The FAVV examples (`Dossier`, `Établissement`, French terms)
  only illustrate the rule. Accepted abbreviations add `WoW`, `WotLK`, `ICC` and `UTC` to `clean-code-naming` R14.
- Work-item ids: use the GitHub issue number (`feature/42-raid-templates`, `@workitem:42`). If a task has no issue,
  ask for one rather than inventing it.
- Branching: `main` is the only long-lived branch (`CONTRIBUTING.md`). There is no `develop`; do not create one.
  PRs target `main` and state that GitFlow is not used.

## 4. Open conflicts — ask before aligning either side

The existing code and the imported skills disagree on these points, and no ADR decides them. Do not mass-rewrite
existing files either way. Follow the existing code in files you only touch, and ask the owner which convention to
adopt before a dedicated alignment PR.

- **Type header:** skills require `Author / Date (MM-yyyy) / Purpose` inside `<summary>`; the code uses a one-line
  `<summary>` plus `<remarks>` with `Author:`, `Date: yyyy-MM-dd` and `Purpose:`.
- **Regions:** skills require bare `#endregion`, `Properties` before `Constructors`, `Domain Behaviours`; the code
  uses named `#endregion Fields`, constructors first, `Domain Behavior`.
- **Spelling in docs:** skills use British spelling (`Initialises`); the code uses American (`Initializes`).
- **Domain events:** skills require the `DomainEvent` suffix and primitive payloads; the code has `CharacterImported`
  carrying a `CharacterId`. Primitive payloads matter once events go through the outbox (Newtonsoft round-trip).
- **Anticipated failures:** skills require `Result` returns; `Character.Claim` throws `UnknownDomainException`.
- **Gherkin:** the existing `.feature` files use numbered titles, free tags, regex bindings and `should`, which
  `gherkin-scenarios` forbids.
- **Documentation suppression:** `Directory.Build.props` adds `CS1591` to `NoWarn` while `.editorconfig` sets it to
  error; `clean-code-static-analysis` R25 requires a waiver ADR for a project-wide suppression.
- **Floating versions:** `Directory.Packages.props` floats Pivot (`*`) and several packages (`10.*`), against
  `clean-code-static-analysis` R2. The comment there already asks to pin Pivot once the feed is reachable.
