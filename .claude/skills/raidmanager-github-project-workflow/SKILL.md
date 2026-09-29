---
name: raidmanager-github-project-workflow
description: >-
  Mandatory GitHub Project workflow for RaidManager story work, from acceptance criteria and a child task
  through an issue-linked branch, pull request, and release tracking.
---

# RaidManager GitHub Project workflow

Use this skill when planning work items or developing a RaidManager user story. Read
`../raidmanager-conventions/SKILL.md` first. Its `main`-only branch model overrides the imported
`pr-and-branching-standards` GitFlow rules; the remaining commit and PR quality rules apply.

## Source of truth

- Repository: `AnnabiGihed/RaidManager`; [Raid Manager project](https://github.com/users/AnnabiGihed/projects/2).
- The first release is the repository milestone `v1.0`. Do not invent a release date or change its scope without
  owner approval.
- This personal GitHub Project uses `type:epic`, `type:story`, `type:task`, `type:bug`, and `type:spike` labels, not
  organization-only issue types. Use native parent/sub-issue links, not just text references.
- Set Project Area, Priority, and Status on every item. Status flows `Todo` -> `In Progress` -> `Done`.
  Keep repository milestone, assignee, labels, and dependencies accurate.

## Work-item contracts

- **Epic:** State the user or business outcome, boundaries, release milestone, and observable exit criteria. Its
  child stories must cover the outcome; an epic title alone is not a deliverable.
- **Story:** State the actor, desired capability, and benefit. Write independently verifiable acceptance criteria,
  including relevant invalid, unavailable, and boundary behavior. Identify affected website, Discord bot, addon,
  companion, and API surfaces, plus dependencies and data/security constraints. Link the parent epic.
- **Task:** Before any story-related implementation, create or reuse a dedicated repository task issue and make it
  a native child of the story. Give it one bounded deliverable, concrete acceptance criteria, verification steps,
  dependencies, `type:task`, assignee, Area, Priority, Status, and the story's release milestone. Do not merely copy
  the story's criteria. A story-related bug or spike is likewise a child issue with its own label and exit criteria.
- A standalone cross-cutting task is allowed only when no single story owns it; state that reason in the issue.
  Do not use this exception to skip the child task for story development.

Missing acceptance criteria, parent linkage, project metadata, or permissions are blockers. Resolve them before
implementation. Discussion, research, and backlog refinement may happen without an implementation task; source,
tests, documentation, and skill edits for a story may not.

## Mandatory delivery sequence

1. Check the relevant epic and story against the contracts above. Clarify ambiguous outcomes with the owner and
   record the agreed criteria in the issues before starting code.
2. Find an existing child task with the same deliverable or create one. Add it to the Project, set required fields,
   and confirm its native parent story, milestone, and acceptance criteria. Never create a duplicate to gain a new
   branch. If GitHub access fails, stop before implementation; do not substitute a local note.
3. Fetch the latest `main` and create one short-lived `feature/<task-number>-<slug>` or
   `fix/<task-number>-<slug>` branch from it. No branch from an epic or story number, no `develop` branch, and no
   direct push to `main`. Set the task to `In Progress`; reflect active work on its parent story and epic.
4. Implement only the task's scope while satisfying the story's applicable criteria. Add automated tests for
   changed behavior and failure paths, run the relevant build, tests, format, and documentation checks, and record
   results. Update affected documentation, API contracts, diagrams, ADRs, and changelog in the same change. Record
   a justified `none` for an artifact that genuinely does not apply; never leave a required check unexplained.
5. Review the final diff, commit with a Conventional Commit title containing the task number, and open one draft PR
   for that task against `main`. The PR must link the task with `Closes #<task-number>` and, for story work, the
   parent with `Refs #<story-number>`. Map each task criterion to evidence and include the five required sections
   and completed author checklist from `pr-and-branching-standards`. Do not describe a skipped or failing check as
   passed.
6. Never mark the PR ready and never mark files as viewed: those actions are the operator's review (ADR-0006). Tell
   the operator the draft is ready for their review once required CI is green. The operator views every file and
   marks it Ready for review; the workflow then requests the peer, and any new commit returns the PR to draft. The
   `review` workflow merges the PR itself once every gate passes (ADR-0008); never queue or perform the merge
   yourself. Never use `--admin`, self-merge, or force-push. GitHub must wait for the peer approval, required checks
   (including `review-files-viewed`, which needs the operator's review first and then a peer approval of the head
   commit, both after viewing every file), and resolved conversations. The repository deletes the source branch after
   merge. If the PR does not merge after every gate passes, report the reason; never merge manually to bypass a gate.
   After merge, verify the source branch was deleted and the task is closed and `Done`; mark a story `Done` only when
   every acceptance criterion is evidenced and all required child work is merged. Mark an epic `Done` only when all
   child stories satisfy their exit criteria.

The `v1.0` milestone tracks release scope; it does not itself authorize a release. Release or tagging requires
all included stories accepted, release checks completed, and explicit owner approval.
