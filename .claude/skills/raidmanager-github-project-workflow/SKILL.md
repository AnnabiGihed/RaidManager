---
name: raidmanager-github-project-workflow
description: >-
  Mandatory repository mechanics for every RaidManager change: one task, one branch, one draft pull request, the PR
  description and checks, the review comments, merging and branch cleanup. Work-management policy (hierarchy,
  classification, sprints, the active-sprint gate, status, completion, releases) is in the work-* skills and the
  Work Management and Delivery Specification.
---

# RaidManager GitHub Project workflow

Use this skill whenever you deliver any change: product stories, improvements, bug fixes, tooling, documentation
or skills. Read `../raidmanager-conventions/SKILL.md` first. Its `main`-only branch model overrides the imported
`pr-and-branching-standards` GitFlow rules; the remaining commit and PR quality rules apply.

Work-management policy isn't repeated here. The
[Work Management and Delivery Specification](../../../docs/reference/work-management-specification.md) is the
authority (ADR-0026), applied by eight skills:

| Skill | Use it for |
| --- | --- |
| `work-classification-and-hierarchy` | Creating, classifying and linking any work item; nothing is standalone. |
| `work-backlog-refinement` | Contracts, criteria, Story Points, priority, dependencies, Ready or Backlog. |
| `work-release-planning` | Release goal, scope, target date, milestone, record and sprint sequence. |
| `work-sprint-planning-and-eligibility` | Sprints, capacity, selection, and the active-sprint gate before any execution. |
| `work-task-execution-and-completion` | Status and Delivery Stage while working, completion, cancellation, reopening. |
| `work-sprint-review-and-carryover` | Sprint outcomes and explicit replanning of unfinished work. |
| `work-stabilization-and-release-closure` | Stabilization, readiness, approval, delivery and release closure. |
| `work-board-configuration-and-validation` | Project fields, views, workflows, forms, the guard and the board report. |

The commands for all of this (ids, creating and linking items, setting fields, the closing order) are in
`raidmanager-board-operations`.

## Source of truth

- Repository: `AnnabiGihed/RaidManager`; [Raid Manager project](https://github.com/users/AnnabiGihed/projects/2).
- Releases are repository milestones with release records in `docs/planning/releases/` (`work-release-planning`).
- This personal GitHub Project uses `type:` labels, not organization-only issue types, and native parent/sub-issue
  and blocked-by links, not text references. Create issues with the issue forms in `.github/ISSUE_TEMPLATE/`.

## User interface work shows its design (ADR-0017)

Any item that changes what users see (website pages, companion windows, addon frames, Discord messages) carries the
`ui` label and links or shows its Penpot mockup: `docs/mockups/<screen>.svg` as an image or link, or a Penpot share
link while the design is in progress. Answer **Yes** to the form's **User interface** question to add the label.
Until the mockup is linked, the `project-hierarchy` workflow labels the item `needs-mockup`. Never start building a
screen whose item is `needs-mockup`: create its design task first (Delivery Stage Functional Analysis, spec §22) and
generate the mockup with the `penpot-mockups` skill (ADR-0018), then have the owner confirm it in Penpot.

## Mandatory delivery sequence

1. Find the task that carries the change, or create it with `work-classification-and-hierarchy`. Never create a
   duplicate to gain a new branch. If GitHub access fails, stop before implementation; don't substitute a local note.
2. Pass the active-sprint gate of `work-sprint-planning-and-eligibility` before starting and before resuming. If it
   fails, stop and report the violated rule.
3. Run `git branch --show-current`, fetch the latest `main`, and create one short-lived `feature/<task-number>-<slug>`
   or `fix/<task-number>-<slug>` branch from `origin/main`. Documentation, planning and skill changes use `feature/`
   too: no `docs/`, `chore/` or other prefix (`CONTRIBUTING.md`). No branch from an epic, feature, story, improvement,
   bug or spike number, no `develop` branch, and no direct push to `main`. Set Status and Delivery Stage as
   `work-task-execution-and-completion` describes.
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
     closed by a PR, and the `description` check fails unless each one reaches an epic through a story,
     improvement, bug or spike and a feature (ADR-0025).
   - When the change touches user-interface files, show the mockup it implements
     (`![<screen>](docs/mockups/<screen>.svg)`), or state `No visual change: <reason>` on its own line. The
     `description` check fails otherwise.
   - Reference the parents with `Refs #<number>`. Examples and parent references must not use closing keywords.
   - Map each task criterion to evidence, quote the coverage comment's numbers, and include the five required
     sections, in this order: `## What changed`, `## Why it changed`, `## How it was tested`, `## What to review
     carefully`, `## Migration or deployment notes` (write `None.` if none), then `### Author self-review` with
     its six checkboxes from `pr-and-branching-standards`. Do not describe a skipped or failing check as passed.
   - Check the description before opening the PR, as the `description` check does:
     `PR_BODY="$(cat <body file>)" python scripts/validate_pr.py` must print "Pull-request description passed"
     (#453 failed it once for the two last sections).
6. Never mark the PR ready and never write a review or review comment in the operator's name: those are the
   operator's review (ADR-0006, ADR-0009). Tell the operator the draft is ready for their review once required CI is
   green. The operator posts a meaningful review comment and marks it Ready for review; the workflow then requests
   the peer, and any new commit returns the PR to draft.
   - **Hand over only a clean PR:** wait for `python scripts/sonar_gate.py --project AnnabiGihed_RaidManager
     --pull-request <number> --commit <head sha> --timeout 1200` to report no finding, and fix every finding first
     (`raidmanager-conventions` §11). Run it only once the PR's `sonar` check has finished on the head commit
     (`gh pr checks <number>` no longer shows it pending): right after a push, the gate reported "no open issue" for
     #516 while SonarCloud was still analyzing, and the `sonar` check then failed on a code smell the owner had to
     point out. Wait for the `sonar` check by name, never for every check: `review-gate` stays pending until the
     reviews are posted, so a loop waiting for nothing pending never ends (the owner stopped one on #553). On
     2026-10-08 the owner stopped two background waits for CI and reported the results instead: once a pull request is
     open, tell the owner what is pending and run the gate when they say the checks finished.
   - **Check first, review texts after** (#523, #531). Posting both reviews lets the `review` workflow merge, and the
     merge closes the task. When the task's completion needs an owner check that can run before the merge (a
     companion build from the PR's artifact, a run on the server), give the check steps one at a time first, and the
     review texts only after the check passed. When the check can only run after the merge (a website change seen on
     dev), ask the owner first; their choice on #527, and again on #385, was to merge, reopen the task as Blocked with
     the unblock condition, and close it with the check's evidence. Offer it as the recommended option.
   - **Check every fact the description states** in the code or the run before opening the PR. #531's first draft
     said `/companion/me` was rate-limited; it isn't.
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

     The shape that works: a first sentence `Operator review of #<pr>. I checked ...` (or `Peer review of #<pr>. I
     compared ...`), then four bullets, each naming a file, identifier or issue from the change and what was verified
     about it. The operator speaks as the owner ("as I decided on #367"); the peer checks the code, records and links.
     Run the check from the repository folder, after the PR exists.

     When the operator asks for another version of one comment, write a new one and check it the same way.
   - **Dependabot pull requests** (#460): the owner is the operator. The `dependency-task` workflow writes the
     description and links the task; the agent selects the task (`raidmanager-board-operations`, "Dependency update
     tasks") and drafts both review comments as for any pull request.
   - The `review` workflow merges the PR itself once every gate passes (ADR-0008). Never queue or perform the merge
     yourself, and never use `--admin`, self-merge, or force-push. Merging is execution (spec §8, A5): a PR not
     merged when its sprint ends waits until its task is selected into a new active sprint. Set the task to
     `In Review` when you hand the PR over.
   - GitHub must wait for the peer approval, required checks, and resolved conversations. `review-gate` needs the
     operator's review comment first, then a peer approval of the head commit, with every review comment
     meaningful.
   - If the `review` job fails after every gate passed, GitHub refused the merge. Report it; re-running that job
     merges the PR. Never merge by hand.
   - **One pull request at a time, or a stated merge order** (#484). Branch protection merges only a branch that is
     up to date with `main`. When two handed-over pull requests are open and one merges, the other falls behind:
     the `review` job fails with "the head branch is not up to date", and updating the branch creates a new head,
     which needs a new review comment from the operator and a new peer approval given after it (#481). Hand over the
     second pull request after the first merges, or tell the operator which to merge first and that the second needs
     a new review; prefer different changelog sections so the update merges without a conflict.
   - **Bring a branch up to date by merging `origin/main` into it** (`git merge origin/main`, then a normal push),
     never by rebasing and force-pushing, even on an unreviewed draft (#479).
7. After merge, verify the source branch was deleted and the task is closed and `Done`, then validate its parents
   with `work-task-execution-and-completion`.
   - **Branch cleanup is mandatory whenever you are told a PR is merged**, before any other work in that turn:
     1. Check the merge yourself: `gh pr view <number> --json state,mergedAt,headRefName` must say `MERGED`.
     2. Run `git fetch --prune origin` and confirm `git ls-remote --heads origin <branch>` prints nothing. If the remote
        branch still exists, delete it (`git push origin --delete <branch>`).
     3. Switch to `main`, pull it, and delete the local branch with `git branch -D <branch>`. `-D` is needed because a
        squash merge leaves the branch looking unmerged. Delete only branches whose PR you confirmed as merged.
     4. Delete any other local branch whose upstream shows `[gone]` in `git branch -vv`, again only after confirming
        its PR merged.
   - The `project-hierarchy` workflow reopens a parent closed too early and re-checks the ancestors of every changed
     issue. Set the reopened item's Status by its evidence (`work-task-execution-and-completion`).
   - Never add a personal token or secret to change Project fields (spec §22).

A milestone tracks release scope; it doesn't authorize a release. Release, tagging and deployment follow
`work-stabilization-and-release-closure`.
