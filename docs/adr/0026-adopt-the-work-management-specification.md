# ADR-0026: Adopt the Work Management and Delivery Specification

- Status: Proposed
- Date: 2026-10-02
- Deciders: Gihed Annabi
- Supersedes in part: [ADR-0025](0025-classify-work-items-with-spikes-under-features.md) (overlapping rules only)

## Context

RaidManager's work rules grew one decision at a time:

- the hierarchy and completion guard ([ADR-0016](0016-epic-feature-story-task-hierarchy.md), then ADR-0025);
- the milestone rule of [#321](https://github.com/AnnabiGihed/RaidManager/issues/321);
- the workflow skill.

There were no sprints, no Delivery Stage, no Story Points, and no Ready, In Review, Blocked or Canceled status. The
built-in Project workflow marked canceled items Done. On 2026-10-02 the owner consolidated the delivery policies into
one Work Management and Delivery Specification and asked for it to be applied in full, with every conflict asked
rather than decided. The owner's answers are recorded on
[#323](https://github.com/AnnabiGihed/RaidManager/issues/323).

## Decision

The [Work Management and Delivery Specification](../reference/work-management-specification.md) is the current
operational authority for RaidManager's work management. Its text is the owner's, unchanged except for the amendments
it marks and lists in its amendment log:

- **A1** adoption;
- **A2** release milestones per level: a task shares its parent's release, while stories, improvements, bugs and
  spikes choose theirs; a feature or epic has a milestone only when all its scope is in that release;
- **A3** a seventh Status, Canceled;
- **A4** closing and reopening set Status deliberately, and the built-in close and reopen workflows are off;
- **A5** merging a pull request is execution;
- **A6** and **A7** how existing items are migrated;
- **A8** RaidManager's project policies.

Under A8:

- Sprints last two weeks, in Europe/Brussels, with start inclusive and end exclusive. Sprint 1 runs from 2026-10-02
  to 2026-10-16.
- Planning records are repository files.
- No token is added: the hierarchy guard keeps enforcing what `GITHUB_TOKEN` can read. Project-field rules, such as
  the active-sprint gate, run as an agent preflight and board report with the owner's local `gh` login.
- Agents estimate Story Points, and Penpot mockup tasks are Functional Analysis.

Skills, issue forms, validators and board configuration link to the specification instead of copying it. Eight
skills follow its section 19 decomposition. `raidmanager-github-project-workflow` keeps only the repository's branch,
pull request and review mechanics.

This ADR supersedes the parts of ADR-0025 that the specification now owns: the classification guide, the work-item
contracts and the completion rules. ADR-0025 stays as the record of how spikes became peers of stories. Its
enforcement is kept where the specification agrees with it:

- the `needs-parent` guard;
- pull requests that close tasks only;
- reopening early closures.

The milestone rule of #321 is replaced by A2.

## Consequences

**Positive**

- One document answers every work-management question, and agents can't meet conflicting copies.
- No task, story, improvement, bug or spike is executed outside an active sprint, and canceled work never counts as
  delivered.

**Negative**

- The active-sprint gate is checked by the agent preflight and an on-demand report, not by the scheduled workflow,
  because no token can read the user-owned Project. Between sessions, a scheduling violation is visible only when the
  report runs.
- Every story needs an estimate and every task a Delivery Stage before execution, which adds planning work.

## Alternatives considered

- **A token stored as a repository secret:** the scheduled workflow could enforce every rule. The owner chose not to
  add a token.
- **Keep #321's strict milestone rule:** it fixed the release view, but it forces features and epics spanning releases
  into one milestone, which the specification forbids.
- **Fold the eight responsibilities into fewer skills:** fewer files, but it departs from section 19's decomposition.
