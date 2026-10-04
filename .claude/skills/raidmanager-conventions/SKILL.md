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
- **The desktop companion is an Avalonia application** (ADR-0032, owner decision on #514), not WPF, MAUI or Blazor
  Hybrid: `avalonia-desktop` and `avalonia-tests` apply to it, and the `blazor-components` and `pivot-auth-maui`
  rules don't. Its host tests use xUnit v3, the rest of the repository xUnit v2.
- **Messaging:** RaidManager has no broker today. RabbitMQ and the outbox transport are not part of the mandatory
  frame here; introducing a broker, an outbox drain mode or a second service needs its own ADR.

## 2. Resolved by enforced repository configuration

| Imported skill says | RaidManager uses | Source of truth |
| --- | --- | --- |
| Indent C# and Gherkin with tabs (`csharp-regions`, `clean-code-static-analysis` R4, `gherkin-scenarios` §10) | 4 spaces for every file | `.editorconfig` + `dotnet format --verify-no-changes` in CI |
| `PACKAGES_READ_USER` / `PACKAGES_READ_TOKEN` | `PIVOT_PACKAGES_USER` / `PIVOT_PACKAGES_TOKEN` | `nuget.config`, `ci.yml`, README |
| Skills kit under `.claude/skills/` or `.github/skills/` | `.agents/skills/` and an identical `.claude/skills/`; apply every change to both in one commit | `AGENTS.md` |
| SonarAnalyzer.CSharp + Meziantou.Analyzer + SonarCloud gate | .NET analyzers + StyleCop in the build; SonarCloud automatic analysis, gated by the required `sonar` check (§11) | `Directory.Build.props` (adding analyzers needs an ADR, `clean-code-static-analysis` R1); ADR-0020 |
| Azure DevOps pipelines and wiki, `::: mermaid` fences | GitHub Actions, MkDocs Material, a GitHub Wiki generated from `docs/`, fenced `mermaid` blocks | `.github/workflows/`, `mkdocs.yml`, `scripts/build_wiki.py` |
| Keep Mermaid to the Azure DevOps subset | Still keep to that subset: it renders everywhere | `docs-diagrams-as-code` |
| Swashbuckle via Pivot's Keycloak Swagger setup | `Microsoft.AspNetCore.OpenApi` (`AddOpenApi`), one generator only | `RaidManager.ApiService/Program.cs` |
| Test projects `{Project}.UnitTests`, mocks in `{Solution}.Shared.UnitTests.Mocks` | `{Project}.Tests` mirrored under `test/`; ask where shared mock factories live when the first one is needed | solution layout |
| xUnit v2 for every test project (`dotnet-unit-tests`) | Desktop companion host tests use `xunit.v3` with `Avalonia.Headless.XUnit`; every other test project stays on `xunit` v2 | `avalonia-tests` §1, ADR-0032 |

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
  3. Stop the AppHost and every `RaidManager.*` process it started, then remove any leftover `postgres-*` container
     (keep the data volume). Never leave processes or containers running for the owner to collide with.
  Signing in to Discord itself is the owner's step; the agent never enters Discord credentials.
- **Never stop an app the owner started.** A `RaidManager.*` process you didn't start belongs to the owner (check its
  start time). If it locks `bin/` and the build fails, don't stop it: build elsewhere with `--artifacts-path` (say which
  tests didn't run there, if any) or ask the owner. Stop only the processes and `postgres-*` containers your own run
  started. The same holds for the owner's
  Visual Studio (`devenv`) and its MSBuild nodes, even when they lock a scratch NuGet cache.
- **A forced restore with another cache moves the assets file.** `dotnet restore --force` with `NUGET_PACKAGES`
  pointed at a scratch folder rewrites `obj/project.assets.json` to that cache; run a normal restore afterwards.

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
- **Run Vale before every handover in which Markdown changed**, the changelog included. CI fails on any Vale error,
  and `scripts/spell_check.py` alone isn't enough (it accepts plurals that Vale rejects):

  ```bash
  MSYS_NO_PATHCONV=1 docker run --rm -v "<repository path>:/work" -w /work jdkato/vale:v3.23.0 README.md CHANGELOG.md CONTRIBUTING.md SECURITY.md docs
  ```

- **Spelling is strict and US English.** The docs check runs `python scripts/spell_check.py` (install
  `pyspellchecker`): every word outside code and links must be a US English word or appear in the one project word
  list, `.vale/styles/config/vocabularies/RaidManager/accept.txt`, which Vale and Visual Studio (through
  `.editorconfig`) also use. British spellings fail, and so does any Vale error (`fail_on_error: true`); Vale matches
  case, so add the capitalized form of a word when it starts sentences. Put code names in backticks, add only genuine project or
  technical terms to the list, keep it sorted, and never add a word to hide a misspelling.
- **Word list order:** sorted without regard to case, with an uppercase form before its lowercase twin (`Dev` before
  `dev`): `sorted(words, key=lambda w: (w.lower(), w))`, which is what the validator test checks. Add words to the
  list read from `main` instead of passing it through a `set()`, which reorders such twins (#413).
  Put a GitHub login such as `@AnnabiGihed` in backticks instead of adding it. Vale also flags possessives of
  product names (`Docker's`, `Aspire's`): reword them ("the Docker repository").
- **Run markdownlint before pushing**, as the docs check does. Node isn't installed in the agent's shell, so run it in
  Docker:

  ```bash
  MSYS_NO_PATHCONV=1 docker run --rm -v "<repository path>:/work" -w /work node:20-alpine npx --yes markdownlint-cli2@0.18.1 "**/*.md" "#node_modules"
  ```

  When rewrapping a paragraph, no line may start with an issue number: `#376, which ...` at the start of a line is
  read as a heading without a space (MD018). Reword so the number falls inside the line.
- **Run the repository's script tests before pushing** any change to `scripts/`, the word list or the docs, as the
  docs `validate` check does: `python -m unittest` from `scripts/tests`, and read its `Ran ... OK` line. Its output
  includes lines such as "Flagged #170 needs-mockup" from fake data; they don't touch real issues.
- **Type-check the scripts too:** the same check runs `python -m mypy` from the repository root (install
  `mypy==1.11.2`); run it before pushing any change to `scripts/`, because `unittest` passes code mypy rejects (#454).

## 8. Test coverage gate (mandatory)

Coverage is measured and enforced on every pull request
([ADR-0015](../../../docs/adr/0015-gate-pull-requests-on-test-coverage.md)).

- **Rule:** at least 80% of the coverable lines a pull request changes under `src/` are covered, and total line
  coverage stays at or above 60%. `build-test` fails otherwise, so the PR can't merge.
- **Measure before pushing:**
  `dotnet test RaidManager.sln --no-build --settings coverage.runsettings --results-directory TestResults`, then
  `python scripts/coverage_gate.py --reports TestResults --base origin/main`. The summary lists every uncovered
  changed line; add tests for them in the same PR.
- **Value objects compared with `==`.** A value object that is a class (such as `PairingCode`) compares by reference
  unless it overloads `==` and `!=`; repository doubles in application tests then miss every match (11 tests failed in
  #382). Give such a value object `Equals`, `GetHashCode` and both operators. EF Core still translates `==` on a
  converted property to SQL with the operators in place, as the PostgreSQL API tests of #382 showed.
- **Run the gate after committing.** `coverage_gate.py` compares committed changes with `--base`, so uncommitted work
  shows as "0 / 0 changed lines". Commit first, then run it on the fresh `TestResults`.
- **Evidence:** quote the changed-lines and total percentages from the coverage comment in the PR's "How it was
  tested" section.
- **Never** lower the thresholds, widen `coverage.runsettings`, or add `[ExcludeFromCodeCoverage]` to get past the
  gate. Changing a threshold needs a new ADR and owner approval.
- **New projects:** a new `src/` project gets its mirrored test project with real tests in the same PR. The summary
  names every project no test loads, such as `RaidManager.DiscordBot` today.

## 9. Work management (mandatory)

The [Work Management and Delivery Specification](../../../docs/reference/work-management-specification.md) is the
authority for work management ([ADR-0026](../../../docs/adr/0026-adopt-the-work-management-specification.md)). The
eight `work-*` skills apply it, and `raidmanager-github-project-workflow` holds the repository's branch, pull request
and review mechanics. These rules override the ticket rules of `pr-and-branching-standards`:

- Nothing is standalone: Epic → Feature → User Story, Improvement, Bug or Spike → Task, on this Project or any other
  board (spec §2, `work-classification-and-hierarchy`). Breaking this is a major failure.
- No task, story, improvement, bug or spike is executed outside an active sprint; check the gate before starting and
  before resuming (spec §8, `work-sprint-planning-and-eligibility`).
- The "work item" that a branch, commit and pull request carry is always a **task** number, never a story,
  improvement, bug, spike, feature or epic. Each of those is delivered through its child tasks.
- Tooling, documentation and process work belongs under **Epic: Engineering platform and delivery** (#142).
- Never create an epic without the owner's approval. Never work around the rules with a personal token, a
  standalone item, a moved sprint date, or an item closed as completed without evidence.

## 10. UI mockups in Penpot (mandatory)

Penpot is the only UI design tool ([ADR-0017](../../../docs/adr/0017-penpot-mockups-for-ui-work.md),
[ADR-0018](../../../docs/adr/0018-generate-penpot-mockups-in-the-repository.md),
[ADR-0019](../../../docs/adr/0019-dark-design-system-with-an-app-shell.md), `docs/how-to/design-a-screen.md`).
A mockup comes before the screen, for every user interface: website, companion, addon and Discord messages. The
`penpot-mockups` skill holds the full workflow.

- **Files:** each screen is `docs/mockups/<screen>.penpot` (source) plus `docs/mockups/<screen>.svg`, rendered from it
  by `python scripts/penpot_render.py`. Commit them together. `validate_docs.py` fails on half a pair, on an SVG that
  isn't the current rendering, on any other file in that folder, and on any `.penpot` or `.svg` outside
  `docs/mockups/` (or `docs/` and `wwwroot/` for other SVGs). **Never put a mockup in the repository root** or any
  other folder; scratch files stay in your scratchpad.
- **Generate, then render:** draft a screen as `scripts/mockups/<screen_name>.py` with `scripts/penpot_scene.py`, run
  it to write the `.penpot` and its SVG, then have the owner import the `.penpot` in Penpot and confirm it. When the
  owner edits the design in Penpot, their downloaded `.penpot` replaces the generated one; re-render the SVG.
- **One design system (ADR-0019):** the dark palette and Open Sans type scale in `scripts/penpot_scene.py`, and the
  app shell (sidebar with Player and Officer navigation, community card, user card, top bar with breadcrumb and realm
  status) on every website screen, drawn with `app_screen` and the shared components in
  `scripts/penpot_components.py`. Never draw a screen's own shell, buttons or badges. The website keeps Radzen and is
  re-themed to match (#203). On the website, the palette lives only in the project-wide theme
  `wwwroot/theme/raidmanager-theme.css`, and every specific look is a standalone, generic component
  (`blazor-components`, both rules mandatory).
- **Use it in the story:** show the SVG in the user story (or improvement or bug), its UI task and the pull request.
- **Work items:** every UI item carries `ui` and links or shows its mockup; `needs-mockup` marks those that don't.
  Epics and features list their stories' mockups.
- **Pull requests:** a change to `src/Containers/UI/`, `addon/`, `.razor`, `.css`, `.html`, `.lua` or `.toc` files
  shows its mockup or states `No visual change: <reason>`; the docs `validate` check (`scripts/ui_mockups.py`)
  enforces it.
- **Check the screen against its mockup before handover (mandatory).** Tests prove behavior, not looks: the owner
  rejected a sign-in page whose tests passed but whose card and button didn't follow the mockup. For every UI change,
  render each state the mockup shows with the real styles (the Radzen theme, `wwwroot/fonts/open-sans.css`,
  `wwwroot/theme/raidmanager-theme.css` and the scoped `RaidManager.Web.styles.css` bundle) and compare it with the
  board: layout, colors, type, button shape and case. Signed-out pages can be checked in the running app; for
  signed-in pages, dump the component's markup from a throwaway bUnit test into the scratchpad and open it with those
  stylesheets in the browser. When the browser pane is hidden it can't take screenshots: check the computed styles
  and sizes instead, and say so. Never commit the throwaway test or the preview files.
- **When the browser pane is too small to judge**, render both sides to PNG with headless Chrome and read the images
  (#513): crop each board out of the mockup SVG by copying it to the scratchpad with the root `viewBox` set to the
  board (`"<x> <y> 1440 900"`, `width="1440" height="900"`), and write each dumped page to an HTML file in the
  scratchpad that links the Radzen `material-base.css` from the NuGet cache, the two `wwwroot` stylesheets and
  `obj/Debug/net10.0/scopedcss/bundle/RaidManager.Web.styles.css` by `file:///` URL, with the markup in a
  `rm-theme-dark` frame 240 px from the left and 64 px from the top. Then run
  `chrome.exe --headless=new --hide-scrollbars --allow-file-access-from-files --window-size=1440,900
  --screenshot=<png> <file URL>` for each.
- **Never pass `class` to a shared component** such as `SurfaceCard`: its `@attributes` come after its own `class`
  and replace it, so the card lost its look on the confirm page of #513. Wrap the component in an element of the
  page that carries the size or placement.
- **The Radzen menu ignores a per-item `Match`** (Radzen 11.5): set on a `RadzenPanelMenuItem`, it doesn't highlight
  subpages. A shell entry that must stay lit under its route sets `MatchesSubpaths` on `ShellEntry`, and
  `NavigationSection` passes `Selected` itself; the menu keeps matching whole paths, so Overview at `/` isn't lit
  everywhere (#513).
- **Product decisions stay with the owner.** Draft layouts freely, but when a UI task depends on an open product
  question, ask it on the task instead of designing an answer. Deliberate deviations update the mockup in the same PR.

## 11. SonarCloud findings (mandatory)

SonarCloud analyzes every pull request, and the required `sonar` check fails it on any finding
([ADR-0020](../../../docs/adr/0020-fail-pull-requests-on-sonarcloud-findings.md)).

- **Rule:** a pull request merges with no open SonarCloud issue (bug, vulnerability or code smell, any severity) and
  no security hotspot to review. SonarCloud's own `SonarCloud Code Analysis` check follows the default quality gate,
  which passes code smells: never read its green status as "no findings".
- **Hand over only at zero findings.** Wait for `sonar_gate.py` on the head commit before giving the owner the PR
  summary and its review comments; never present a PR whose findings the owner would have to point out. Name a
  repeated string literal once as a constant while writing a script, rather than after Sonar flags it.
- **Wait for the analysis first.** Run the gate below only after the PR's `sonar` check finished on the head
  commit; a gate run during the analysis can report nothing (#516).
- **Check before asking for review:** `python scripts/sonar_gate.py --project AnnabiGihed_RaidManager
  --pull-request <number> --commit <head sha>` prints each finding with its file, line and rule. Common ones in this
  repository: a string literal repeated three or more times (`python:S1192`, name it once as a constant), and
  `${{ }}` expressions inside `run:` (pass them through `env:`).
- **Scripts never pass command-line text to a subprocess** (`pythonsecurity:S8705`, command argument injection).
  Sonar traces an argument into `subprocess` even after `argparse` validation or `--end-of-options`. Use a constant
  (the repository name), the script's own value (`next(name for name in CHOICES if name == args.x)`), or state the
  process already has (`git rev-list HEAD` after checking `HEAD` equals the given commit), as
  `scripts/record_deployment.py` does (#454).
- **Fix every finding in the same pull request.** Only a finding that is genuinely wrong is marked as a false
  positive or accepted in SonarCloud, individually, with a reason naming the pull request. Never bulk-resolve.
- **Evidence:** the `sonar` check is green on the head commit; say so in "How it was tested".

## 12. Working on Windows (mandatory)

The owner's checkout is on Windows with `core.autocrlf`, so files in the working copy have CRLF line endings while
Git stores LF.

- **Write files with LF.** A script that writes a source file uses `newline="\n"` (Python); Git converts on checkout.
- **`dotnet format --verify-no-changes`** reports `ENDOFLINE` for every CRLF file in a Windows working copy, including
  files nobody changed. CI on Linux doesn't. Verify only the files you changed, after normalizing them to LF:
  `dotnet format RaidManager.sln --verify-no-changes --include <changed .cs and .razor files>`, and ignore
  `ENDOFLINE` lines for files you didn't touch.
- **Line-ending-only diffs:** a regenerated file that differs only in line endings shows as modified;
  `git diff --ignore-cr-at-eol --name-only` lists the real changes. Don't commit or report line-ending-only changes.
- **Binary files** such as fonts are marked in `.gitattributes`; add a rule there for any new binary type.
- **Throwaway tests build with the analyzers on.** A preview test added to a test project only to dump markup
  fails the build on documentation and ordering rules; start it with `#pragma warning disable`, since it is deleted
  before committing.
- **Long or quoted text in a shell:** a heredoc containing apostrophes can fail in the agent's shell, even with a
  quoted delimiter (`<<'EOF'`), which happened three times in #382 and #513. Write long files
  with the editor tool or from a script file in the scratchpad instead. Make code and text edits with the editor tool
  or a Python script, not `sed` with escaped patterns, which mangle `\n`, `\s` and quotes.
- **Issue and pull request bodies** are written with `newline="\n"` too; a body with CRLF breaks the guard's heading
  parsing (`raidmanager-board-operations`).

- **Mermaid diagrams** are rendered with the pinned `minlag/mermaid-cli:11.12.0` in Docker (no Node locally), from
  `docs/diagrams`:

  ```bash
  MSYS_NO_PATHCONV=1 docker run --rm -u root -v "$(pwd -W):/data" minlag/mermaid-cli:11.12.0 -i /data/<name>.mmd -o /data/<name>.svg -c /data/mermaid-config.json -b white
  ```

  Never pipe it through `tail`: a parse error then looks like success and the old SVG stays. Check the SVG's modification time, and render a PNG to the
  scratchpad to look at it. In a sequence diagram, a `;` inside a message ends the statement and breaks parsing;
  use commas or parentheses (#100).

## 13. Working with the owner (mandatory)

The owner, Gihed Annabi, works with the agent through short messages and expects the same behavior in every session.

- **Follow the specification and the skills to the letter.** When a request conflicts with a rule, a record or an
  earlier decision, ask; never decide alone. Ask with the question tool, two to four options, the recommended one
  first and marked "(Recommended)", each with its consequence. Product and scope questions are always asked.
- **Record every owner decision on the issue it settles**, in the body under `### Owner decisions (<date>, recorded
  here)` or as a comment, before acting on it. A request for new work becomes a work item in the hierarchy, selected
  into the active sprint with the request recorded, before execution.
- **Never enter credentials or ask for them.** The owner performs steps that need OVH, DNS, Discord developer portal
  or GitHub settings access; ask for facts (server specifications, names) without credentials.
- **Give owner steps one at a time, in the chat** (owner decision, #387). Each step says what to do, where (which
  program, which folder, PowerShell or the server), why in a sentence, and what the owner should see. Wait for the
  owner's report before the next step. Never assume a tool or habit: ask how they sign in or work before a step
  depends on it (the owner signed in to the server with a password through PuTTY).
- **The agent never signs in to the owner's server.** To learn its state, give read-only commands that print no
  secret and ask the owner to paste the output; check from outside what can be checked from outside (DNS, HTTPS,
  certificates, SSH methods, a port scan), and record both on the issue. Read pasted output carefully: a `sudo grep`
  logs its own command line, which can look like the match it searched for.
- **Changes on a shared server never assume a fresh machine.** Ask what already runs there before writing a step,
  and make every step only add, check before changing, and restore on failure (#374).
- **"Merged"** means: confirm the merge yourself, clean up the branches, close the task and its validated parents
  with evidence, run the board report, then report and propose the next item. Never act on "merged" before
  `gh pr view` says `MERGED`.
- **Report like this:** lead with the result; then what changed on the board and in the files, the checks run with
  their results, what to review carefully, and the two drafted review comments verbatim. Name your own mistakes plainly
  and say how they were fixed. Keep it short; no recap of earlier turns.
- **Alternative review comments:** when the owner asks for another version (for example because a pipeline keeps
  failing), give new texts, each checked with `verify_review.py`, never a lightly edited copy. When the review gate
  fails because an existing review copies the operator's text, a new approval doesn't help: the existing review must
  be edited, in the UI or with `gh api -X PUT repos/AnnabiGihed/RaidManager/pulls/<n>/reviews/<review id> -F body=@<file>`.
- **Dates and times** are absolute and in Europe/Brussels (sprints start inclusive and end exclusive at 00:00).
- **The owner's in-game file.** When the owner sends the path of `RaidManager.lua` after an addon check, read it,
  copy it into the scratchpad (the game may rewrite or move it at any moment) and run the contract checker on the
  copy (`wow-addon-335a-lua` §10). Record the evidence on the task without the characters' GUIDs.

## 14. Ending a session ("new session", mandatory)

The owner ends a session with the words "new session" (owner decision on #510). That alone starts this routine; never
ask the owner to repeat what it contains.

1. **Checkpoint.** Leave no step half done: finish the current command, or commit and push the work in progress and
   record its state on its task. Report anything waiting on the owner.
2. **Lessons work item.** Create, without asking again (standing approval on #510), an improvement under #156,
   "keep the session's lessons", with one task, selected into the active sprint, both with their contracts and the
   request recorded. If no sprint is active, stop and ask.
3. **What goes in, and where.** Only what a later session needs and can't read from the code or the board:
   - mistakes made and the rule that prevents them;
   - routines the owner asked for or corrected;
   - facts verified about external systems (the game client, GitHub, the server);
   - choices the owner made the same way several times, as the recommended option;
   - pitfalls met in tools and checks.

   Each goes into the skill that owns the topic, in both trees; a rule for every session goes into `AGENTS.md`.
   Never into the agent's memory: the skills hold working knowledge. The sprint record lists every item added
   during the session with its decision, and the changelog gets an entry.
4. **Pull request.** One task, one branch, one draft pull request, with the usual checks and both review comments.
5. **Handover message.** Give, in one fenced `text` block the owner can paste, a message with these parts, short and
   with issue and pull request numbers instead of explanations:
   - **Start:** read `AGENTS.md`, `raidmanager-conventions` (§13, §14), `raidmanager-board-operations` and
     `raidmanager-github-project-workflow`; run `python scripts/work_gate.py report` and report its violations.
   - **State at handover:** each open pull request with what to do when the owner says "merged" (which items close,
     which parents to validate, which check after the merge), each Blocked item with its unblock condition, the
     sprint and release with their dates.
   - **Automation in force** that changes what happens after a merge (dev deployment rule, Dependabot routine).
   - **Remaining order** of the sprint, and the backlog items not to start without the owner.
   - **Rules for every task:** preflight; one task, branch and draft pull request; the checks before handover; both
     review comments checked; never post, approve, mark ready or merge; reopen as Blocked a task its merge closed
     before its verification.
   - **Working with the owner:** owner steps one at a time (what, where, why, what to expect); no sign-in to the
     server, no credentials; questions on any conflict or product choice.
   - **Next:** the first action of the new session.
