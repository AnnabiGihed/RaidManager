# ADR-0004: Use protected automatic squash merges

- Status: Proposed
- Date: 2026-09-29
- Deciders: Gihed Annabi

## Context

RaidManager requires independent review, passing checks, and resolved conversations before merging to `main`.
Manual merge timing and branch cleanup add work after those requirements have been met. Documentation publication
depends on the `main` push event after a merge.

## Decision

We permit only squash merges and enable GitHub auto-merge. The task owner queues each ready pull request for an
automatic squash merge. Branch protection continues to require one independent approval, current `build-test` and
`validate` checks, and resolved conversations. GitHub deletes the source branch after merging. We do not merge
through a privileged GitHub Actions token or bypass branch protection.

## Consequences

**Positive**

- Approved, validated pull requests merge without a second manual action.
- `main` retains one conventional commit per task, and merged source branches do not accumulate.
- A user-initiated auto-merge preserves the existing post-merge publication trigger.

**Negative**

- Every pull request still needs an explicit auto-merge queue action and an independent reviewer.
- A later push can invalidate approval or checks and delay the merge until requirements pass again.

## Alternatives considered

- **Manual squash merge:** rejected because it requires another action after all gates pass.
- **Privileged workflow that queues every pull request:** rejected because token-triggered merge events may not
  start the `main` push workflow that publishes documentation, and elevated pull-request workflows increase risk.
