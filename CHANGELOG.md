# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project follows
[Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- The GitHub Wiki now shows the `docs/` documentation, generated and published after every merge, and pull requests
  check that it builds. The site navigation lists every decision record (ADR-0014) (#119).
- Project completion rules and a hierarchy audit that reopens epics or stories closed before their native child
  work is complete (#111).
- An interactive API reference page at `/scalar` in Development, linked from the Aspire dashboard (ADR-0013) (#109).
- Players can sign in to the website with Discord and sign out; a cancelled or failed sign-in explains why and
  offers a retry. The website now runs in Interactive Server mode with Radzen (ADR-0012) (#99).
- The API now hosts the application and persistence and exposes a website-only endpoint that resolves a Discord
  sign-in to its user; the Aspire AppHost generates the shared website key (ADR-0011) (#95).
- A sign-in command that resolves a Discord account to exactly one persisted user, refreshing a changed Discord
  profile, with a unique Discord id in the database (#92).
- A query listing a player's pending and conflicted character claims, read with no-tracking queries on the write
  database (ADR-0010) (#89).
- SQL Server persistence for characters with EF Core: claims, loadouts and raid saves, an initial migration,
  domain events recorded in an outbox table, and integration tests against a SQL Server container (#87).
- Application commands to approve or reject a character claim, with validation and handler tests; the domain
  repository contracts now extend Pivot's command repository (#85).
- A how-to page on reviewing a pull request, with comments that pass and fail the review gate (#81).
- Warmane lockout evidence matrix with explicit unknown reset, difficulty, extension, and encounter rules (#40).
- Version 1 visual guide with UML use cases, domain models, components, sequences, state transitions,
  readiness decisions, editable sources, and rendered diagrams.
- Initial Clean Architecture solution skeleton.
- Pivot.Framework package-source configuration.
- Initial Identity, Characters, Communities and Raids domain model.
- Domain events and repository contracts for aggregate roots.
- Architecture decision record for Discord-only authentication and Warmane-first integration.
- Combined raids in the domain: a raid requires one or more distinct instance and difficulty targets (#49).
- Character claim lifecycle in the domain: pending, approved, rejected, and conflict claims, where a repeated
  upload never approves, reopens, or transfers a claim (#48).
- Signup availability in the domain: confirmed, tentative, late with an arrival time, or declined, independent
  of roster selection (#50).
- Raid-start readiness in the domain: each raid target gets an available, resets-before-raid, needs-fresh-sync,
  or locked-through-raid verdict, a combined raid takes the most restrictive, and a locked character cannot
  sign up or be rostered (#54).
- Raid-save scan evidence in the domain: only a complete scan replaces a character's raid saves, even when it
  finds none; incomplete and out-of-order scans keep the newer saves, and readiness uses the latest complete
  scan rather than any addon upload (#57).

### Changed

- The settings file of the API no longer carries an unused Discord section or a placeholder database password (#102).
- The README now explains step by step how to give a machine access to the Pivot.Framework package feed (#97).
- Pull requests now need the reviewer to mark every changed file as viewed before approving the latest commit;
  a new push dismisses approval, and unchecked author self-review items fail the description check (#59).
- The `review-files-viewed` check is now required on `main`, and its runs no longer cancel each other (#61).
- Submitting or dismissing a review now re-evaluates the required `review-files-viewed` check automatically (#63).
- The operator who ran the agent now reviews every file and marks the draft ready before a peer is requested, and
  a peer approval counts only after that review; new commits return the pull request to draft (#65).
- Marking a pull request ready now queues its squash auto-merge automatically (#67).
- The review workflow no longer carries code for the retired single-reviewer gate (#69).
- The review gate now shows as a pending status while a review is outstanding, and fails only for a real
  problem, instead of failing until both reviews are done (#73).
- Pull requests now show only `build-test`, `validate`, and one `review` line: the review jobs and auto-merge
  are one job, and documentation publishing and the weekly link check run in their own workflows (#75).
- The review workflow now merges approved pull requests itself with the workflow token, so no personal write
  token is needed (#77).
- Reviews are now proven by meaningful review comments instead of Viewed marks, so no review token is needed;
  the required status is renamed `review-gate`, and merges close linked issues and delete the branch (#79).
- A review comment that copies another person's review comment, even lightly edited, now fails the review gate
  (#83).
- Pull requests now use protected automatic squash merging and delete their source branches after merge (#46).
- Documentation contributions now pass root-file, style, link, diagram, and rendered-site checks before merging (#44).

### Fixed

- The completion guard now re-checks a parent and grandparent when a child is reopened or its labels change, and
  requires at least one completed child. A parent closed as not planned stays closed. A parent marked `Done` early
  returns to `In Progress` through the Project's built-in workflows, with no manual status correction (#116).
- Pull request examples can no longer close epics or stories when the review workflow merges a change (#111).
- Starting the Aspire AppHost now opens the dashboard at a fixed address instead of leaving only a console (#105).
- The review workflow now closes a pull request's tasks even when GitHub does not link its "Closes #N" (#91).
- Two reviews in quick succession no longer leave the required review check stale: the refresh waits for a
  running check and re-runs it after the latest review (#71).
