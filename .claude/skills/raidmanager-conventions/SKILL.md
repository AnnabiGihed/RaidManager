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
| Azure DevOps pipelines and wiki, `::: mermaid` fences | GitHub Actions, MkDocs Material, a GitHub Wiki generated from `docs/`, fenced `mermaid` blocks | `.github/workflows/`, `mkdocs.yml`, `scripts/build_wiki.py` |
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

## 5. Aspire AppHost and the local run

`src/Containers/Aspire/Hosting/RaidManager.AppHost` is the only way to run the product locally.

- **Launch profile:** `Properties/launchSettings.json`, `https` profile. The dashboard is at `https://localhost:17190`
  (OTLP `21190`, resource service `22190`); the `http` profile uses `15190`/`19190`/`20190`. The website is
  `https://localhost:55365` and the API `https://localhost:55366`. Keep these fixed; the Discord redirect and the
  README depend on them.
- **Parameters and secrets:** `website-service-key` is generated and persisted by Aspire (ADR-0011).
  `discord-client-id` and `discord-client-secret` (secret) are set by the owner with
  `dotnet user-secrets set "Parameters:<name>" "<value>" --project src/Containers/Aspire/Hosting/RaidManager.AppHost`.
  Add every new secret the same way: `AddParameter(name, secret: true)`, a README line, never a settings file.
  Check which parameters exist by reading the keys of the AppHost's `secrets.json`, never by printing values.
- **Feed credentials in the agent shell:** `PIVOT_PACKAGES_USER`/`PIVOT_PACKAGES_TOKEN` are Windows user variables; an
  agent session started earlier may not inherit them. Read them from the user profile for the one restore command
  that needs them, without echoing them.
- **Smoke test after touching startup:** any change to the AppHost, a host's `Program.cs`, launch settings, or
  configuration keys requires one local run, recorded in the PR:
  1. `dotnet run --project src/Containers/Aspire/Hosting/RaidManager.AppHost --launch-profile https` in the background.
  2. Wait until `https://localhost:55365/` returns 200, check `https://localhost:55366/` answers and
     `https://localhost:55366/scalar` returns 200, and that `/sign-in` redirects to `https://discord.com/api/oauth2/authorize` with a `client_id` and the
     `https://localhost:55365/signin-discord` redirect URI (redact the id and state in the PR).
  3. Stop the AppHost and every `RaidManager.*` process it started, then remove any leftover `sql-*` container
     (keep the data volume). Never leave processes or containers running for the owner to collide with.
  Signing in to Discord itself is the owner's step; the agent never enters Discord credentials.

## 6. API reference page (mandatory)

Every RaidManager API host serves an interactive reference page; it is never optional. The choice is Scalar
([ADR-0013](../../../docs/adr/0013-scalar-api-reference.md)); switching to Swagger UI needs a new ADR, removing the
page is not allowed.

- **Wiring:** `AddOpenApi()` is the only generator, `app.MapOpenApi()` publishes `/openapi/v1.json` in every
  environment, and `app.MapScalarApiReference(...)` serves `/scalar` in Development only, with
  `AddPreferredSecuritySchemes` listing the schemes the host enforces. Source: `RaidManager.ApiService/Program.cs`.
- **Dashboard:** the AppHost adds an "API reference" link on the host's resource with `WithUrl(...)`.
- **Tests:** mirror `ApiReferenceTests` (Development, `/scalar` returns 200 with the page title) and
  `ApiReferenceProductionTests` (Production factory, 404) in `RaidManager.ApiService.Tests`.
- **Smoke test:** step 2 of §5 checks the page.
- **New API host:** a second API host copies all of the above in the same PR that creates it, including its
  dashboard link, tests, README line and an entry in `docs/reference/api-contracts.md`.

## 7. Documentation publication (mandatory)

`docs/` is published twice after every merge to `main`, both generated
([ADR-0003](../../../docs/adr/0003-publish-documentation-through-github-pages.md),
[ADR-0014](../../../docs/adr/0014-mirror-documentation-to-the-github-wiki.md)):

- **GitHub Pages:** `https://annabigihed.github.io/RaidManager/`, the strict MkDocs build.
- **GitHub Wiki:** `https://github.com/AnnabiGihed/RaidManager/wiki`, built by `scripts/build_wiki.py` and pushed by
  the `wiki` job of `docs-publish.yml` with `GITHUB_TOKEN`.

Rules for every documentation change:

- Never create, edit or delete a wiki page by hand, and never ask the owner to. Change `docs/` and let the merge
  publish it.
- Every new document starts with its `#` title and gets a `mkdocs.yml` nav entry in the same PR. The docs check runs
  `python scripts/build_wiki.py` and fails on a missing nav entry, a broken link, or a duplicate page name. Run it
  locally before pushing.
- Keep documents to GitHub-compatible Markdown: fenced `mermaid` blocks, relative links, no MkDocs-only syntax
  (admonitions, tabs, snippets) until the converter supports it.
- A new ADR also gets a row in `docs/adr/README.md`.

## 8. Test coverage gate (mandatory)

Coverage is measured and enforced on every pull request
([ADR-0015](../../../docs/adr/0015-gate-pull-requests-on-test-coverage.md)).

- **Rule:** at least 80% of the coverable lines a pull request changes under `src/` are covered, and total line
  coverage stays at or above 60%. `build-test` fails otherwise, so the PR can't merge.
- **Measure before pushing:**
  `dotnet test RaidManager.sln --no-build --settings coverage.runsettings --results-directory TestResults`, then
  `python scripts/coverage_gate.py --reports TestResults --base origin/main`. The summary lists every uncovered
  changed line; add tests for them in the same PR.
- **Evidence:** quote the changed-lines and total percentages from the coverage comment in the PR's "How it was
  tested" section.
- **Never** lower the thresholds, widen `coverage.runsettings`, or add `[ExcludeFromCodeCoverage]` to get past the
  gate. Changing a threshold needs a new ADR and owner approval.
- **New projects:** a new `src/` project gets its mirrored test project with real tests in the same PR. The summary
  names every project no test loads, such as `RaidManager.DiscordBot` today.

## 9. Work-item hierarchy (mandatory)

Every work item sits in one chain of native sub-issues
([ADR-0016](../../../docs/adr/0016-epic-feature-story-task-hierarchy.md)): Epic → Feature → Story, Improvement or
Bug → Task or Spike. Only an epic has no parent. `raidmanager-github-project-workflow` holds the contracts and the
delivery sequence; these rules override the ticket rules of `pr-and-branching-standards`:

- The "work item" that a branch, commit and pull request carry is always a **task or spike** number, never a story,
  improvement, bug, feature or epic. A bug is fixed through its child task.
- A story, improvement or bug can't close without a completed child task; a feature or epic can't close without a
  completed child of the level below. `project-hierarchy.yml` reopens early closures and labels misplaced items
  `needs-parent`; the docs `validate` check rejects a pull request whose task doesn't reach an epic.
- Create issues with the issue forms (blank issues are disabled) and link the parent immediately. Tooling,
  documentation and process work belongs under **Epic: Engineering platform and delivery** (#142).
- Never create an epic without the owner's approval. Never work around the rules with a personal token, a
  standalone task, or a parent closed as completed without evidence.

## 10. UI mockups in Penpot (mandatory)

Penpot is the only UI design tool ([ADR-0017](../../../docs/adr/0017-penpot-mockups-for-ui-work.md),
`docs/how-to/design-a-screen.md`). A mockup comes before the screen, for every user interface: website, companion,
addon and Discord messages.

- **Files:** each screen is `docs/mockups/<screen>.penpot` (source) plus `docs/mockups/<screen>.svg` (export),
  committed together; `validate_docs.py` fails on half a pair or any other file in that folder. Never hand-edit the
  SVG, and never commit a mockup from another tool.
- **Work items:** every UI item carries `ui` and links or shows its mockup; `needs-mockup` marks those that don't.
  Epics and features list their stories' mockups.
- **Pull requests:** a change to `src/Containers/UI/`, `addon/`, `.razor`, `.css`, `.html`, `.lua` or `.toc` files
  shows its mockup or states `No visual change: <reason>`; the docs `validate` check (`scripts/ui_mockups.py`)
  enforces it.
- **The agent can't design in Penpot** (it needs the owner's account). When a UI task has no mockup, stop and ask the
  owner for it; don't invent a layout. Deliberate deviations update the mockup in the same PR.
