# ADR-0016: One Epic, Feature, Story and Task hierarchy

- Status: Superseded by [ADR-0025](0025-classify-work-items-with-spikes-under-features.md)
- Date: 2026-09-30
- Deciders: Gihed Annabi

## Context

The Project had epics, stories, tasks, bugs and spikes, but no level between an epic and its stories, and no
distinction between new capabilities and improvements. About 30 tasks had no parent, mostly repository tooling. A
bug counted as a work item and could close from a pull request without any task. The completion guard of
[#116](https://github.com/AnnabiGihed/RaidManager/issues/116) only knew epics and stories.

## Decision

Every work item sits in one chain of native sub-issues:

- An **epic** has no parent. Its children are **features**.
- A **feature** has an epic as its parent. Its children are **stories**, **improvements** and **bugs**.
- A **story**, **improvement** or **bug** has a feature as its parent. Its children are **tasks** and **spikes**.
- A **task** or **spike** has a story, improvement or bug as its parent, and no children. A spike counts as a task.

Each type has a `type:` label and an issue form that asks for the parent; blank issues are disabled.

An epic, feature, story, improvement or bug closes as completed only when at least one child of the level below is
completed and every child is closed. A pull request closes only tasks and spikes, and each must reach an epic through
the full chain. Items closed as *not planned* or *duplicate* are abandoned and exempt.

The `project-hierarchy` workflow enforces the rules with the built-in `GITHUB_TOKEN`:

- It reopens invalid closures, labels misplaced items `needs-parent` with one comment, and checks the ancestors of
  every changed issue.
- It audits the whole board every 15 minutes through one GraphQL query per 50 issues.
- The docs `validate` check blocks a pull request whose closed work items don't reach an epic.

The existing board was migrated:

- 17 features group the 30 product stories.
- An **Engineering platform and delivery** epic holds the repository tooling as features, improvements and a bug.
- Every earlier orphan task now has a parent.

## Consequences

**Positive**

- The board reads top-down: every task explains which capability and outcome it serves.
- A story, improvement or bug can no longer look done without delivered work.

**Negative**

- Every new item needs a parent before its first pull request, and a new capability needs a feature first.
- GitHub starts no workflow when a sub-issue link changes, so the guard may take up to 15 minutes to flag a
  misplaced item.

## Alternatives considered

- **Keep bugs as work items:** simpler, but a bug could close without a task, which hides how it was fixed.
- **A custom Project field for the type instead of labels:** it can drive board grouping, but issue forms and the
  guard can't set or read it with the built-in token.
- **Only migrate open items:** less churn, but the closed history would break the rule the audit enforces.
