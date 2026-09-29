# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project follows
[Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

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
- Pull requests now use protected automatic squash merging and delete their source branches after merge (#46).
- Documentation contributions now pass root-file, style, link, diagram, and rendered-site checks before merging (#44).

### Fixed

- Two reviews in quick succession no longer leave the required review check stale: the refresh waits for a
  running check and re-runs it after the latest review (#71).
