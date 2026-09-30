# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project follows
[Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- Players sign in to the website with Discord and sign out; a canceled or failed sign-in explains why and offers a
  retry. The website runs in Interactive Server mode with Radzen (ADR-0012) (#99).
- The API resolves a Discord sign-in to exactly one local user for the website, refreshing a changed Discord profile.
  Only the website can call it, with a key the Aspire AppHost generates (ADR-0011) (#92, #95).
- The API lets the website list a player's character claims awaiting a decision, oldest first, and approve or reject
  one. Approving a character another player owns keeps the claim in conflict review (ADR-0010) (#85, #89, #161).
- Characters are stored in SQL Server with EF Core: claims, loadouts and raid saves, with domain events recorded in an
  outbox table (#87).
- Character claims in the domain: pending, approved, rejected and conflict. A repeated upload never approves,
  reopens or transfers a claim (#48).
- Combined raids in the domain: a raid requires one or more distinct instance and difficulty targets (#49).
- Signup availability in the domain: confirmed, tentative, late with an arrival time, or declined, independent of
  roster selection (#50).
- Raid-start readiness in the domain: each raid target gets an available, resets-before-raid, needs-fresh-sync or
  locked-through-raid verdict. A combined raid takes the most restrictive one, and a locked character can't sign up
  or be rostered (#54).
- Raid-save scan evidence in the domain: only a complete scan replaces a character's raid saves, even when it finds
  none. Incomplete and out-of-order scans keep the newer saves (#57).
- Warmane lockout evidence matrix with explicit unknown reset, difficulty, extension and encounter rules (#40).
- Version 1 visual guide with UML use cases, domain models, components, sequences, state transitions, readiness
  decisions, editable sources and rendered diagrams.
- Initial Clean Architecture solution on Pivot.Framework and its package source, with the Identity, Characters,
  Communities and Raids domain model, its domain events and repository contracts, and the decision record for
  Discord-only authentication and Warmane-first integration.
- An interactive API reference page at `/scalar` in Development, linked from the Aspire dashboard (ADR-0013) (#109).
- Every work item sits in one Epic, Feature, Story, Improvement or Bug, Task or Spike hierarchy. Issue forms ask for
  the parent. The hierarchy workflow labels misplaced items and reopens a parent closed before at least one child is
  completed. A pull request closes only tasks and spikes whose chain reaches an epic (ADR-0016) (#111, #116, #159).
- CI measures test coverage on every run and shows it on each pull request. A pull request fails when its changed
  lines are under 80% covered or total coverage is under 60% (ADR-0015) (#121).
- The GitHub Wiki shows the `docs/` documentation, generated and published after every merge, and pull requests check
  that it builds. The site navigation lists every decision record (ADR-0014) (#119).
- Penpot is the UI design tool. Every item and pull request that changes what users see shows its mockup from
  `docs/mockups/`, and issue forms ask about user-interface impact (ADR-0017) (#165).
- UI mockups are generated in the repository as native Penpot files with editable text, shared colors and text
  styles, a clickable prototype and WCAG AA contrast. Each mockup's SVG is rendered from its `.penpot` file, and the
  docs check fails on a stale SVG or on a mockup outside `docs/mockups/`. The `penpot-mockups` skill holds the
  workflow (ADR-0018) (#169).
- A dark design system from the owner's reference design: named colors, Open Sans text styles, and an app shell on
  every website screen with the Player and Officer navigation, community, breadcrumb, realm status and user. Mockups
  are generated with it, and the website will be re-themed to match (ADR-0019) (#202).
- A how-to page on reviewing a pull request, with comments that pass and fail the review gate (#81).

### Changed

- A pull request merges only after its operator posts a meaningful review comment and marks it ready, and a peer
  then approves it with a meaningful comment. The review workflow squash-merges it with the workflow token, closes
  its tasks and deletes the branch; no personal token is involved, and a comment copied from another reviewer fails
  the gate (ADR-0006, ADR-0008, ADR-0009) (#46, #59, #65, #73, #75, #77, #79, #83).
- Documentation contributions pass root-file, style, link, diagram and rendered-site checks before merging (#44).
- The README explains step by step how to give a machine access to the Pivot.Framework package feed (#97).
- The API settings file no longer carries an unused Discord section or a placeholder database password (#102).

### Fixed

- A `ui` item keeps its `needs-mockup` label until the mockup it names exists on `main`; naming a file that a task
  will create no longer counts as a mockup (#199).
- Documentation spelling is checked as strictly as the editor: US English plus one project word list that CI, Vale
  and Visual Studio share. British spellings and code names written as prose are fixed (#173).
- The repository scripts and their tests pass a type check. CI runs `mypy`, and a Pyright configuration lets editors
  resolve the scripts' imports (#165).
- The review workflow no longer leaves a ready pull request open without a sign. It retries a refused merge for about
  five minutes and, if nothing else would retry it, fails the `review` job with GitHub's message (#123).
- A parent marked `Done` too early returns to `In Progress` through the Project's built-in workflows, with no manual
  status correction (#116).
- Starting the Aspire AppHost opens the dashboard at a fixed address instead of leaving only a console (#105).
- The review workflow closes a pull request's tasks even when GitHub doesn't link its "Closes #N" (#91).
