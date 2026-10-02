---
name: raidmanager-github-project-workflow
description: >-
  Mandatory GitHub Project workflow for all RaidManager work: the Epic, Feature, Story/Improvement/Bug/Spike, Task
  hierarchy (nothing standalone), classifying each item by intent then scope, work-item contracts, an issue-linked
  branch and pull request per task, and release tracking.
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

## The hierarchy (mandatory, non-negotiable, ADR-0025)

Every work item sits in exactly one chain of native sub-issues. There are no exceptions: no standalone task, story,
improvement, bug, spike or feature, in this Project or any other board you plan work on. Breaking this rule is a
major failure; fix it before anything else.

```text
Epic
└── Feature
    ├── User Story ── Task
    ├── Improvement ── Task
    ├── Bug ── Task
    └── Spike ── Task
```

| Type | Label | Parent | Children |
| --- | --- | --- | --- |
| Epic | `type:epic` | none | features |
| Feature | `type:feature` | an epic | user stories, improvements, bugs, spikes |
| User Story | `type:story` | a feature | tasks |
| Improvement | `type:improvement` | a feature | tasks |
| Bug | `type:bug` | a feature | tasks |
| Spike | `type:spike` | a feature | tasks |
| Task | `type:task` | a user story, improvement, bug or spike | none |

- Only an epic has no parent. Give every issue exactly one type label, and link it to its parent as soon as it is
  created; after 10 minutes the `project-hierarchy` workflow labels an unlinked, misplaced or untyped open item
  `needs-parent`.
- **Every item carries its parent's milestone.** A milestone view such as **Release v1.0** filters out a parent on
  another milestone (or none) and shows its children as if they were standalone. When you set or change a milestone,
  set it on the whole chain: the parent, its parent, up to the epic. The guard flags a mismatch `needs-parent`.
- A task describes how bounded work is done and verified; its parent says why the work exists: deliver a capability
  (story), enhance something (improvement), correct a defect (bug) or answer a research question (spike).
- Before creating any issue, classify it with the guide below and check its parent exists. When no feature fits,
  propose one under the right epic; ask the owner before creating an epic.
- **User interface work shows its design (ADR-0017).** Any epic, feature, story, improvement, bug, task or spike
  that changes what users see (website pages, companion windows, addon frames, Discord messages) carries the `ui`
  label and links or shows its Penpot mockup: `docs/mockups/<screen>.svg` as an image or link, or a Penpot share link
  while the design is in progress. Answer **Yes** to the form's **User interface** question to add the label. Until the
  mockup is linked, the `project-hierarchy` workflow labels the item `needs-mockup`. Never start building a screen
  whose item is `needs-mockup`: create its design task first and generate the mockup with the `penpot-mockups` skill
  (ADR-0018), then have the owner confirm it in Penpot.
- Repository tooling, documentation and process work belongs under the **Engineering platform and delivery** epic
  (#142).
- Only a task is a work item: a story, improvement, bug or spike is delivered through its child tasks, and the pull
  request closes those tasks.

## Classify by intent first, then scope (mandatory)

| Type | Identifying question | Expected result |
| --- | --- | --- |
| User Story | Does a user need a capability they can't perform today? | A new, independently verifiable user capability |
| Improvement | Does existing behavior, tooling, documentation or process need to become better? | A verifiable enhancement |
| Bug | Does actual behavior contradict agreed requirements or intended behavior? | Correct behavior restored |
| Spike | Must we resolve an unknown before choosing a direction or implementing? | Evidence, findings or a recommendation |
| Feature | Are several related outcomes needed to deliver one coherent capability? | A capability delivered through its child items |
| Epic | Are several features needed to achieve a broader user or business objective? | An objective achieved across features |

1. **Choose a user story when** you can name an actor, a capability they lack and its benefit; acceptance criteria
   describe observable user behavior.
   - Not an improvement: the capability is missing; nothing existing is refined.
   - Not a bug: no agreed requirement is violated.
   - Not a spike: the deliverable is usable behavior, not research findings.
   - Not a feature: it is one independently verifiable outcome within a larger capability.
   - Not an epic: it doesn't span several features.
   - Example: an officer can create a raid event from Discord. Its tasks implement the command and verify
     permissions.
2. **Choose an improvement when** existing behavior, tooling, documentation or process works as specified but needs
   a verifiable enhancement.
   - Not a bug: current behavior meets its agreed requirements.
   - Not a spike: the enhancement and its success criteria are understood.
   - Not a user story: the main intent is improving something that exists.
   - Not a feature: it is one bounded enhancement, not a group of outcomes.
   - Not an epic: it doesn't span several features.
   - Example: fewer steps to sign up for an existing raid event. Its tasks make and verify the changes.
3. **Choose a bug when** actual behavior differs from agreed or intended behavior. Describe expected behavior, actual
   behavior, steps to reproduce, and how the fix is verified.
   - Not an improvement: the behavior is wrong, not merely less convenient or efficient.
   - Not a spike: the defect is established; not knowing its cause doesn't change its type.
   - Not a user story: it restores intended behavior rather than adding a capability.
   - Not a feature: it is one failure within a capability.
   - Not an epic: it is a defect, not an objective.
   - If the expected behavior was never agreed, ask the owner to settle it before calling it a bug.
   - Example: a player signs up once but appears twice in the roster. Its tasks reproduce it, fix the cause and add
     regression coverage.
4. **Choose a spike when** the main deliverable is knowledge needed to make a decision. State a specific question, a
   time box and exit criteria.
   - Not an improvement: it decides what should change rather than delivering an agreed enhancement.
   - Not a bug: it investigates a question rather than promising to fix an established defect.
   - Not a user story: it produces evidence or a recommendation, not a usable capability.
   - Not a feature: it answers one bounded question supporting a capability.
   - Not an epic: it investigates uncertainty supporting a broader objective.
   - A spike belongs directly to a feature. It owns the question and the conclusion; its tasks own the bounded
     activities that produce evidence: compare candidates, build a disposable proof of concept, test, and write up
     the findings and recommendation.
   - It can inform sibling stories, improvements or bugs through dependency links. Its conclusion may recommend
     implementation, more research or abandoning an approach; research succeeds without finding a viable solution.
   - Example: can a tunnel carry Blazor sessions and Discord OAuth without opening router ports?
5. **Choose a feature when** one coherent capability needs several related stories, improvements, bugs or spikes.
   - Not an improvement or a bug: it groups capability delivery rather than one enhancement or failure.
   - Not a spike: its exit criteria need a delivered capability, not findings alone.
   - Not a user story: several independently verifiable outcomes sit under it.
   - Not an epic: it is one capability within a broader objective.
   - Example: Discord raid planning, covering event creation, signup, withdrawal and roster viewing.
6. **Choose an epic when** a broad user or business objective needs several features. Define its boundaries and
   observable exit criteria.
   - Not an improvement, bug, spike, user story or feature: one enhancement, defect, finding, user outcome or
     capability can't satisfy it.
   - Example: communities organize raids through RaidManager, across community management, raid planning,
     character eligibility and Discord integration.

## Work-item contracts

- **Epic:** State the user or business outcome, boundaries, release milestone, and observable exit criteria. Its
  features must cover the outcome; an epic title alone is not a deliverable.
- **Feature:** State the capability users get and its exit criteria. Its stories, improvements, bugs and spikes
  must cover it.
- **Story:** State the actor, desired capability, and benefit. Write independently verifiable acceptance criteria,
  including relevant invalid, unavailable, and boundary behavior. Identify affected website, Discord bot, addon,
  companion, and API surfaces, plus dependencies and data/security constraints.
- **Improvement:** State what gets better and for whom, with verifiable acceptance criteria.
- **Bug:** State the problem, the expected behaviour, steps to reproduce, and how the fix is verified.
- **Task:** Before any implementation, create or reuse a task and make it a native child of its story, improvement,
  bug or spike. Give it one bounded deliverable, concrete acceptance criteria, verification steps, dependencies,
  `type:task`, assignee, Area, Priority, Status, and the release milestone. Don't merely copy the parent's criteria.
- **Spike:** A specific question, a time box and exit criteria, under a feature. Its research is done through
  child tasks, and the task that records the findings and recommendation completes it.

**Completion.** Keep an epic, feature, story, improvement, bug or spike open and out of `Done` until at least one
child of the level below is closed as completed, every child is closed, and its own criteria have evidence. A story,
improvement, bug or spike therefore never closes without a completed task. Close an abandoned item as *not
planned* instead; the guard exempts it.

Missing acceptance criteria, parent linkage, project metadata, or permissions are blockers. Resolve them before
implementation. Discussion, research, and backlog refinement may happen without an implementation task; source,
tests, documentation, and skill edits may not.

## Mandatory delivery sequence

1. Check the item's type against the classification guide and its chain against the contracts above: epic,
   feature, then the story, improvement, bug or spike. Clarify
   ambiguous outcomes with the owner and record the agreed criteria in the issues before starting code.
2. Find an existing child task with the same deliverable or create one under the story, improvement, bug or spike. Add it
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
   - **Vale and spelling** whenever any Markdown changed, the changelog included (`raidmanager-conventions` §7).
   - **UI:** compare the rendered page with its mockup board, with the real styles (`raidmanager-conventions` §10).

   Update affected documentation, API contracts, diagrams, ADRs, and changelog in the same change. Record a
   justified `none` for an artifact that genuinely does not apply; never leave a required check unexplained.
5. Review the final diff and stage only the files the task changed (never `git add -A` or `git add .`). Other agent
   sessions share this checkout and leave their own untracked or modified files: check `git status --short` before
   committing and leave every file you didn't create alone. Commit with a Conventional Commit title containing the
   task number, and open one draft PR for that task against `main`:
   - Link the task with `Closes #<task-number>` on a standalone line before the first heading. Only tasks may be
     closed by a PR, and the docs `validate` check fails unless each one reaches an epic through a story,
     improvement, bug or spike and a feature.
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
   - **Hand over only a clean PR:** wait for `python scripts/sonar_gate.py --project AnnabiGihed_RaidManager
     --pull-request <number> --commit <head sha> --timeout 1200` to report no finding, and fix every finding first
     (`raidmanager-conventions` §11).
   - **Draft both review comments, every PR (mandatory).** With the summary of the PR, give the operator two texts
     to post, never posting them yourself, approving, or marking ready:
     1. the **operator's review comment**, about product and design intent: what the operator checked and why it is
        what they asked for;
     2. the **peer's approval comment**, about code and tests: which files or identifiers were reviewed and what
        they guarantee.

     Each must name something the PR changes (a file or an identifier from the diff), have at least ten meaningful
     words and no generic praise, and the two must not be copies of each other. Check them with the review gate's
     own rules before handing them over:

     ```bash
     python - <<'PY'
     import subprocess, sys
     sys.path.insert(0, "scripts")
     from verify_review import Change, comment_problems, is_copy
     PR = "<number>"
     OPERATOR = """<operator comment>"""
     PEER = """<peer comment>"""
     run = lambda *a: subprocess.run(["gh", "pr", "diff", PR, *a], capture_output=True, text=True, check=True).stdout
     change = Change(tuple(run("--name-only").split()), run())
     print("operator:", comment_problems(OPERATOR, change, summary=True) or "passes")
     print("peer:", comment_problems(PEER, change, summary=True) or "passes")
     print("copy of each other:", is_copy(PEER, OPERATOR))
     PY
     ```

     When the operator asks for another version of one comment, write a new one and check it the same way.
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
