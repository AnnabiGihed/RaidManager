---
name: raidmanager-github-project-workflow
description: >-
  Mandatory GitHub Project workflow for all RaidManager work: the Epic, Feature, Story/Improvement/Bug, Task/Spike
  hierarchy, work-item contracts, an issue-linked branch and pull request per task, and release tracking.
---

# RaidManager GitHub Project workflow

Use this skill whenever you plan work items or deliver any change: product stories, improvements, bug fixes,
tooling, documentation or skills. Read `../raidmanager-conventions/SKILL.md` first. Its `main`-only branch model
overrides the imported `pr-and-branching-standards` GitFlow rules; the remaining commit and PR quality rules apply.

## Source of truth

- Repository: `AnnabiGihed/RaidManager`; [Raid Manager project](https://github.com/users/AnnabiGihed/projects/2).
- The first release is the repository milestone `v1.0`. Do not invent a release date or change its scope without
  owner approval.
- This personal GitHub Project uses `type:` labels, not organization-only issue types, and native parent/sub-issue
  links, not text references. Create issues with the issue forms in `.github/ISSUE_TEMPLATE/`.
- Set Project Area, Priority, and Status on every item. Status flows `Todo` -> `In Progress` -> `Done`.
  Keep repository milestone, assignee, labels, and dependencies accurate.

## The hierarchy (mandatory, ADR-0016)

Every work item sits in exactly one chain of native sub-issues. There are no exceptions and no standalone tasks.

| Type | Label | Parent | Children |
| --- | --- | --- | --- |
| Epic | `type:epic` | none | features |
| Feature | `type:feature` | an epic | stories, improvements, bugs |
| Story | `type:story` | a feature | tasks, spikes |
| Improvement | `type:improvement` | a feature | tasks, spikes |
| Bug | `type:bug` | a feature | tasks, spikes |
| Task | `type:task` | a story, improvement or bug | none |
| Spike | `type:spike` | a story, improvement or bug | none |

- Only an epic has no parent. Give every issue exactly one type label, and link it to its parent as soon as it is
  created; after 10 minutes the `project-hierarchy` workflow labels an unlinked item `needs-parent`.
- **User interface work shows its design (ADR-0017).** Any epic, feature, story, improvement, bug, task or spike
  that changes what users see (website pages, companion windows, addon frames, Discord messages) carries the `ui`
  label and links or shows its Penpot mockup: `docs/mockups/<screen>.svg` as an image or link, or a Penpot share link
  while the design is in progress. Answer **Yes** to the form's **User interface** question to add the label. Until the
  mockup is linked, the `project-hierarchy` workflow labels the item `needs-mockup`. Never start building a screen
  whose item is `needs-mockup`: create its design task first and generate the mockup with the `penpot-mockups` skill
  (ADR-0018), then have the owner confirm it in Penpot.
- Choose the middle level by intent: a **story** adds a capability for a user; an **improvement** makes existing
  behaviour, tooling, documentation or process better; a **bug** fixes behaviour that doesn't match its
  specification. Repository tooling belongs under the **Engineering platform and delivery** epic.
- A bug is never a work item: it is fixed by a child task, and the pull request closes that task.
- A new capability that fits no feature needs a new feature under the right epic first. Ask the owner before
  creating an epic.

## Work-item contracts

- **Epic:** State the user or business outcome, boundaries, release milestone, and observable exit criteria. Its
  features must cover the outcome; an epic title alone is not a deliverable.
- **Feature:** State the capability users get and its exit criteria. Its stories, improvements and bugs must cover
  it.
- **Story:** State the actor, desired capability, and benefit. Write independently verifiable acceptance criteria,
  including relevant invalid, unavailable, and boundary behavior. Identify affected website, Discord bot, addon,
  companion, and API surfaces, plus dependencies and data/security constraints.
- **Improvement:** State what gets better and for whom, with verifiable acceptance criteria.
- **Bug:** State the problem, the expected behaviour, steps to reproduce, and how the fix is verified.
- **Task:** Before any implementation, create or reuse a task and make it a native child of its story, improvement
  or bug. Give it one bounded deliverable, concrete acceptance criteria, verification steps, dependencies,
  `type:task`, assignee, Area, Priority, Status, and the release milestone. Don't merely copy the parent's criteria.
- **Spike:** Time-boxed research with a question and exit criteria; it counts as a task.

**Completion.** Keep an epic, feature, story, improvement or bug open and out of `Done` until at least one child of
the level below is closed as completed, every child is closed, and its own criteria have evidence. A story,
improvement or bug therefore never closes without a completed task or spike. Close an abandoned item as *not
planned* instead; the guard exempts it.

Missing acceptance criteria, parent linkage, project metadata, or permissions are blockers. Resolve them before
implementation. Discussion, research, and backlog refinement may happen without an implementation task; source,
tests, documentation, and skill edits may not.

## Mandatory delivery sequence

1. Check the item's chain against the contracts above: epic, feature, then the story, improvement or bug. Clarify
   ambiguous outcomes with the owner and record the agreed criteria in the issues before starting code.
2. Find an existing child task with the same deliverable or create one under the story, improvement or bug. Add it
   to the Project, set the required fields, and confirm its native parent, milestone, and acceptance criteria.
   Verify the whole chain reaches an epic. Never create a duplicate to gain a new branch. If GitHub access fails,
   stop before implementation; do not substitute a local note.
3. Run `git branch --show-current`, fetch the latest `main`, and create one short-lived `feature/<task-number>-<slug>`
   or `fix/<task-number>-<slug>` branch from `origin/main`. No branch from an epic, feature or story number, no
   `develop` branch, and no direct push to `main`. Set the task to `In Progress`; reflect active work on its parents.
4. For UI work, open the item's mockup first and build to it. If the implementation must differ, update the
   `.penpot` source and the SVG export in the same change (`docs/how-to/design-a-screen.md`).
   Implement only the task's scope while satisfying the parent's applicable criteria. Add automated tests for
   changed behavior and failure paths. Before pushing, run the build, tests, format, and these gates locally, and
   record the results:
   - **Coverage** (`raidmanager-conventions` §8): at least 80% of changed lines covered and the total at or above
     60%.
   - **Documentation:** `python scripts/validate_docs.py` and `python scripts/build_wiki.py`
     (`raidmanager-conventions` §7). Every new document gets its `#` title and a `mkdocs.yml` nav entry; never
     edit the wiki.
   - **Skills:** edit both trees identically.

   Update affected documentation, API contracts, diagrams, ADRs, and changelog in the same change. Record a
   justified `none` for an artifact that genuinely does not apply; never leave a required check unexplained.
5. Review the final diff and stage only the files the task changed (never `git add -A`). Commit with a Conventional
   Commit title containing the task number, and open one draft PR for that task against `main`:
   - Link the task with `Closes #<task-number>` on a standalone line before the first heading. Only tasks and spikes
     may be closed by a PR, and the docs `validate` check fails unless each one reaches an epic through a story,
     improvement or bug and a feature.
   - When the change touches user-interface files, show the mockup it implements
     (`![<screen>](docs/mockups/<screen>.svg)`), or state `No visual change: <reason>` on its own line. The docs
     `validate` check fails otherwise.
   - Reference the parents with `Refs #<number>`. Examples and parent references must not use closing keywords.
   - Map each task criterion to evidence, quote the coverage comment's numbers, and include the five required
     sections and the completed author checklist from `pr-and-branching-standards`. Do not describe a skipped or
     failing check as passed.
6. Never mark the PR ready and never write a review or review comment in the operator's name: those are the
   operator's review (ADR-0006, ADR-0009). Tell the operator the draft is ready for their review once required CI is
   green. The operator posts a meaningful review comment and marks it Ready for review; the workflow then requests
   the peer, and any new commit returns the PR to draft.
   - The `review` workflow merges the PR itself once every gate passes (ADR-0008). Never queue or perform the merge
     yourself, and never use `--admin`, self-merge, or force-push.
   - GitHub must wait for the peer approval, required checks, and resolved conversations. `review-gate` needs the
     operator's review comment first, then a peer approval of the head commit, with every review comment
     meaningful.
   - If the `review` job fails after every gate passed, GitHub refused the merge. Report it; re-running that job
     merges the PR. Never merge by hand.
7. After merge, verify the source branch was deleted and the task is closed and `Done`. Close a story, improvement
   or bug only when every acceptance criterion is evidenced and all child tasks are closed, at least one completed.
   Close a feature, then an epic, the same way, one level at a time after checking the full hierarchy.
   - **Branch cleanup is mandatory whenever you are told a PR is merged**, before any other work in that turn:
     1. Check the merge yourself: `gh pr view <number> --json state,mergedAt,headRefName` must say `MERGED`.
     2. Run `git fetch --prune origin` and confirm `git ls-remote --heads origin <branch>` prints nothing. If the remote
        branch still exists, delete it (`git push origin --delete <branch>`).
     3. Switch to `main`, pull it, and delete the local branch with `git branch -D <branch>`. `-D` is needed because a
        squash merge leaves the branch looking unmerged. Delete only branches whose PR you confirmed as merged.
     4. Delete any other local branch whose upstream shows `[gone]` in `git branch -vv`, again only after confirming
        its PR merged.
   - The `project-hierarchy` workflow reopens a parent closed too early and re-checks the ancestors of every changed
     issue. The Project's built-in *Item reopened* workflow then returns it to `In Progress`.
   - Never add a personal token or secret to change Project status: the loop needs none
     (`docs/reference/project-automation.md`).

The `v1.0` milestone tracks release scope; it does not itself authorize a release. Release or tagging requires
all included stories accepted, release checks completed, and explicit owner approval.
