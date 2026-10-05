---
name: raidmanager-board-operations
description: >-
  Mandatory recipes for operating the Raid Manager GitHub Project through `gh` and the GraphQL API: the field and option
  ids, creating and linking work items, setting fields, starting, handing over and closing work in the right order,
  bulk changes with a dry run and read-back, and the pitfalls met so far. Use it with the work-* skills, which hold
  the rules; this skill holds the commands.
---

# Raid Manager board operations

The work-* skills and the [Work Management and Delivery
Specification](../../../docs/reference/work-management-specification.md) say what must happen; this skill says how
to do it on this board without relearning it. Every command runs with the owner's local `gh` login, never a personal
token or a repository secret (spec §22).

## Ids

Repository `AnnabiGihed/RaidManager`, Project `PVT_kwHOAPL9-M4BlGGp`
([Raid Manager](https://github.com/users/AnnabiGihed/projects/2)). Re-read them with the configuration query in
`work-board-configuration-and-validation` if a call fails with an unknown id.

| Field | Field id | Values (option or iteration id) |
| --- | --- | --- |
| Status | `PVTSSF_lAHOAPL9-M4BlGGpzhj0P78` | Backlog `f75ad846`, Ready `d41c15e0`, In Progress `47fc9ee4`, In Review `46ac76c5`, Blocked `b83f351d`, Done `98236657`, Canceled `ac06ee45` |
| Sprint | `PVTIF_lAHOAPL9-M4BlGGpzhkI2OE` | Sprint 5 `fec91ce7`, Sprint 6 `cdd05023`, Sprint 7 `053be665`, Sprint 8 `31bb5757`, Sprint 9 `cada5356`, Sprint 10 `80b42dc6`, Sprint 11 `67fd9191`, Sprint 12 `b54e4a68`, Sprint 13 `41a62d5c`, Sprint 14 `fbb423c7`, Sprint 15 `52e6c533`, Sprint 16 `4b0add8b` (Sprints 1 to 4: `8ffad868`, `269b4c63`, `f7855cd9`, `b9635598`) |
| Delivery Stage | `PVTSSF_lAHOAPL9-M4BlGGpzhkI2OI` | Business Analysis `9d9164e9`, Functional Analysis `8eadb407`, Architecture Analysis `e1670f61`, Development `ecb25aca`, Testing `68dcaffd`, Deployment `9313482e` |
| Story Points | `PVTF_lAHOAPL9-M4BlGGpzhkI2PE` | a number |
| Risk | `PVTSSF_lAHOAPL9-M4BlGGpzhkNU0o` | Low `047760b3`, Medium `8b7214fc`, High `099e0d7c` |
| Priority | `PVTSSF_lAHOAPL9-M4BlGGpzhj0VHg` | P0 Critical `2e29488f`, P1 High `14c3c65c`, P2 Normal `1abe36cd`, P3 Later `fe47cb25` |
| Area | `PVTSSF_lAHOAPL9-M4BlGGpzhj0VFo` | Access & Community `8b9dc37d`, Character Sync `838ab2cb`, Raid Planning `a8e0cade`, Readiness `f05c3162`, Roster `50ac204c`, Raid Night `ac665257`, Platform `5ddc6272` |
| Start date | `PVTF_lAHOAPL9-M4BlGGpzhj0VdY` | a date |
| Target date | `PVTF_lAHOAPL9-M4BlGGpzhj0VeU` | a date |

Milestones are releases: v0.1 #2, v0.2 #3, v0.3 #4, v0.4 #5, v1.0 #1. The sprint each release owns is in the
"Sprint sequence" table of its record in `docs/planning/releases/`.

## Recipes

Write every multi-step change as a short Python script in the scratchpad that calls `gh` through `subprocess`,
rather than as shell one-liners: quoting and escaping in the agent's shell mangle `\n`, `$` and apostrophes.

```python
import json, subprocess
from pathlib import Path
REPO_DIR = "D:/Work/Personal Project/RaidManager"
PROJECT = "PVT_kwHOAPL9-M4BlGGp"

def gh(*args):
    return subprocess.run(["gh", *args], capture_output=True, text=True, check=True,
                          encoding="utf-8", cwd=REPO_DIR).stdout

def set_field(item_id, field_id, value):
    """value: 'singleSelectOptionId:"<id>"', 'iterationId:"<id>"', 'number:3' or 'date:"2026-10-03"'."""
    gh("api", "graphql", "-f", "query=mutation{updateProjectV2ItemFieldValue(input:{projectId:\"%s\",itemId:\"%s\","
       "fieldId:\"%s\",value:{%s}}){projectV2Item{id}}}" % (PROJECT, item_id, field_id, value))

def item_id(number):
    return gh("api", "graphql", "-f", "query={repository(owner:\"AnnabiGihed\",name:\"RaidManager\"){issue(number:%d)"
              "{projectItems(first:2){nodes{id}}}}}" % number,
              "--jq", ".data.repository.issue.projectItems.nodes[0].id").strip()
```

### Create a work item

1. Classify it first (`work-classification-and-hierarchy`) and pick its parent.
2. Write the body with exactly the headings of its issue form in `.github/ISSUE_TEMPLATE/` (`### Parent`,
   `### Purpose`, and so on), to a file written with `newline="\n"`. Record what the owner decided in the body under
   `### Owner decisions (<date>, recorded here)`.
3. Create it with its type label, milestone and assignee:
   `gh issue create --title "<Type>: <title>" --body-file <absolute path> --label type:<type> --milestone v0.3
   --assignee AnnabiGihed`. Pass an absolute path: `gh` runs from the repository folder.
4. Link the parent at once with the child's database id (not its number):
   `gh api -X POST repos/AnnabiGihed/RaidManager/issues/<parent>/sub_issues -F sub_issue_id=<child id>`, where the id
   comes from `gh api repos/AnnabiGihed/RaidManager/issues/<child> --jq .id`. Add `-F replace_parent=true` to move it.
5. Add it to the Project with `addProjectV2ItemById(input:{projectId, contentId: <issue node id>})` and set Status,
   Sprint, Delivery Stage, Priority, Area and, for outcome items, Story Points and Risk.
6. Record the estimate and the risk as a comment on outcome items:
   `Estimate: **3 Story Points**. Rationale: ... Estimated by the agent (specification sections 9 and 22).` and
   `Risk: **Low**. Reason: ... (agent assessment; the owner may change it).`
7. Dependencies are native links:
   `gh api -X POST repos/AnnabiGihed/RaidManager/issues/<n>/dependencies/blocked_by -F issue_id=<blocker id>`.

### Start work

1. `python scripts/work_gate.py preflight <task>`. A new item can be missing from the Project's item list for a minute:
   if the result says it isn't on the Project, wait 20 seconds and run it again, up to a few minutes. Never change
   dates or sprints to pass a failing gate.
2. Set the task `In Progress`, check its assignee, and set its Start date to today in Europe/Brussels. Do the same for
   each parent up to the epic that has no actual start yet (#397).
3. Branch from `origin/main` as `feature/<task>-<slug>` or `fix/<task>-<slug>` only.

### Hand over

1. Open the draft pull request. Its prose names other items without a closing keyword: never write close, closes,
   closed, fix, fixes, fixed, resolve, resolves or resolved right before an issue number, except in the one
   `Closes #<task>` line. Write "the merge of #414 shut #387" or "#387 was closed by a merge" instead.
2. Check which issues GitHub linked, a few seconds after opening (right after, the list is still empty):
   `gh pr view <n> --json closingIssuesReferences --jq '[.closingIssuesReferences[].number]'` must print exactly
   `[<task>]`. If not, edit the description with `gh pr edit <n> --body-file <file>` and check again before anything
   else; a linked closed item is set back to In Progress by the Project workflow "Pull request linked to issue".
3. Set the task `In Review`, run `sonar_gate.py` on the head commit, and draft both review comments
   (`raidmanager-github-project-workflow`, step 6).

### Close after a merge

1. Confirm the merge on its own: `gh pr view <n> --json state,mergedAt`. Never clean up on the owner's word alone;
   an earlier session deleted a branch and closed a task before the merge had happened. Check that `mergedAt` falls
   inside the task's active sprint (A5).
2. Clean up the branches (`raidmanager-github-project-workflow`, step 7).
3. **Comment first, then set Done.** The Project closes an issue as soon as its Status becomes Done, so post the
   evidence comment before changing the Status, or the issue closes without it. Then set the Target date to the
   closing day and Status `Done`.
4. After a few seconds, check `gh issue view <n> --json state,stateReason`. If it is still open, close it with
   `gh issue close <n> -r completed`. `gh issue close` has no `-q` flag.
5. Validate the parent independently (`work-task-execution-and-completion`, action 8) and close it the same way, with
   its own evidence comment. A spike closes when its question is answered and the owner reviewed the answer.
6. Run `python scripts/work_gate.py report` and report its blocking categories.
7. The merge starts the dev deployment. When it finishes, check that the task carries `deployed:dev`
   (`gh issue view <n> --json labels`) and say so in the report; if it carries `deploy-failed:dev`, report the failed
   run and the bug it needs. The **first real** `deploy-failed:dev` is also noted on #392 with the run and the
   labeled items: its failure path was accepted on unit tests (owner decision on #392).

### Deployment labels

The deployment workflows' `record` job sets `deployed:dev`, `deployed:test`, `deployed:production` and
`deploy-failed:<environment>` on items, and a `Deployed to <environment>` line on release milestones
(`scripts/record_deployment.py`, spec §22, A9, owner decisions on #392). They show the owner what runs where.

- **Never set or remove these labels, or edit the milestone's `deployments` block, by hand.** The job recomputes
  them from the history of the deployed commit, so a wrong label means a wrong rule or input: fix the script with a
  bug.
- To check what a deployment would change, run the script with `--dry-run` from a worktree checked out at the
  deployed commit (`git worktree add <folder> <commit>`), with the full commit id; it reads with your `gh` login and
  writes nothing.
- A merge that changes no deployed file (documentation, skills, tests, scripts, other workflows) skips the build
  and deployment, and its `record` job still labels the items `deployed:dev` (#491, A10). Read "the dev deployment
  of a merge succeeded" as both jobs of `deploy-dev` succeeding or `deploy` being skipped with a successful `record`.
- A task closed without a pull request (evidence only) gets `deployed:<environment>` at the next deployment after it
  closes; a parent gets it when all its completed children have it, so it can follow its close by one deployment.
- A release's `Deployed to production` line is evidence for its record, not the Released state: that still needs the
  owner's approval and the record (`work-stabilization-and-release-closure`).

### Dependency update tasks

Dependabot opens one grouped pull request a week for the new releases of GitHub Actions, and the `dependency-task`
workflow gives it a task under #461 (`docs/reference/project-automation.md`, "Dependency updates"). The workflow can't write Project fields,
so at the start of a session the board report shows `Pull request #<n> is open, but the task has no active sprint`.
With the owner's standing approval recorded on #460, and without asking again:

1. Select #461 into the active sprint if it isn't there (Status `In Progress`, Sprint, Delivery Stage Development),
   and the task (Sprint, Status `In Review`, Delivery Stage Development, Start date today, assignee checked).
2. Draft both review comments for the pull request, as for any other (`raidmanager-github-project-workflow`, step 6),
   and tell the owner to tick its checklist while reviewing.
3. A task the workflow closed as not planned gets Status `Canceled`, its Start and Target dates the closing day.
4. After the merge, close it like any task ("Close after a merge"). #461 stays open.

### Create a Project view

The REST API creates a view with its filter; grouping, sorting and field sums are still set in the browser
(`docs/reference/project-automation.md`, "One-time setup"):

```bash
gh api -X POST users/AnnabiGihed/projectsV2/2/views -f name="<name>" -f layout=table -f filter='<filter>'
```

Add the view to the Views table of `project-automation.md` in the same change.

### Bulk changes

For any change to many items (filling past values, field values, splits):

1. Read the items with a paginated query that prints one JSON object per line:
   `gh api graphql --paginate -f query=<query with $endCursor> --jq '.data.node.items.nodes[]'`, with
   `pageInfo { hasNextPage endCursor }` in the query.
2. **Dry run:** compute every change and print the counts and the exceptions; look at the exceptions before writing.
3. Apply with `--apply`, skipping values already set, so a rerun after a failure is safe.
4. **Read back** from the Project and check for gaps; post the read-back on the improvement that asked for it.

## Pitfalls met so far

| Pitfall | Fix |
| --- | --- |
| A body written on Windows without `newline="\n"` got `\r\r\n` line endings, and the guard couldn't read its headings. | Write bodies with `newline="\n"`; when editing a body read with `gh issue view --json body`, replace `\r\n` with `\n` first. |
| Setting Status to Done closed the issue before its evidence comment was posted. | Comment first, then set Done (above). |
| Single-select options recreated without their ids wiped every item's value. | Update options with their existing ids (`work-board-configuration-and-validation`). |
| `gh issue view` refuses `--comments` together with `--json`. | Use `--json body,comments -q ...`. |
| A record task created afterwards to document finished work gave its parent a start after its close. | When a derived start falls after the close, use the item's own creation date. |
| Planning records drifted from the board after splits and moves. | Generate item lists in records from the Project, never by hand, and compare them before a review. |
| A failed edit command didn't stop the next one, so a creation script ran twice and made four duplicates (#428 to #431). | Chain dependent commands with `&&`. Close duplicates as not planned, Status Canceled, with a comment naming the original. |
| A creation script that isn't safe to rerun creates duplicates after any partial failure. | Read the open issues first, reuse an item whose title exists, refuse to run while two open items share a title, and skip links that already exist. |
| `addProjectV2ItemById` failed right after the sub-issue link, racing the Project's own "Auto-add sub-issues" workflow. | Retry the add with growing waits; it is idempotent and succeeds on retry. |
| The description of #419 said a merge "closed" #387. GitHub read it as a closing keyword and linked #387, and the Project workflow "Pull request linked to issue" set the closed #387 back to In Progress. `Refs #N` doesn't link. | Keep closing keywords out of prose and check `closingIssuesReferences` after opening each pull request ("Hand over"). The workflow is right to stay on: it only acts on linked issues. |
| Two handed-over pull requests were open; #479 merged first, so #481 fell behind `main`, the `review` job failed fifteen merge attempts, and updating the branch made both reviews start again. | Hand over one pull request at a time or state the merge order (`raidmanager-github-project-workflow`, step 6). On a new head, give the operator and the peer new review texts for that commit. |
| A feature met its exit criteria while its backfilled contract still had `Unknown` fields (#125, #206, #128). | The owner chose each time to fill the fields from what was delivered, record that on the feature, then close it: offer this as the recommended option. A bug found after the last story closes goes under the feature first, and the feature closes after it. |
| Task #385 carried a whole story's screens (four boards, domain, migration, API and website) in one pull request. | Offer to split by intent: viewing in the task, editing in a new task under the same story, blocked by it. The owner chose it as the recommended option (#385, #545); record the split on the story and narrow the task's contract. |
| An in-game or post-merge check found a defect in work already merged (#499, #503, #507). | Raise a bug under the feature with its own task, ask the owner whether it goes in the active sprint, and fix it before the next feature task. Never fold a correction into a feature task. |
| A rewrapped Markdown paragraph broke a code span across two lines, and the spell check read its words as prose. | Wrap by hand, or with a script that never splits inside backticks. |
| GitHub showed no closing link (`closingIssuesReferences` was empty) for Dependabot descriptions written with the workflow token; the link appeared once the owner saved the description. | Nothing to fix: the review workflow also closes the tasks named on the `Closes` line. Don't read the empty link as a missing task. |
| Importing a scratch script to reuse its helpers re-ran its top-level actions: closing comments, statuses and dates were sent again (harmless only because the script skipped existing comments). | Put a scratch script's actions under `if __name__ == "__main__":` before reusing it, and keep every action idempotent: skip a comment already posted, a link already made. |
| A pull request whose task is verified outside the repository (a run on the server) closed the task on merge, before the verification, three times in #387. | Say in the pull request and the report to merge only after the verification. If it merges first, reopen the task, set it Blocked with the reason, the person responsible and the unblock condition, clear its Target date, and close it again with the evidence once verified. |

## Sources

- Owner decisions on #323, #346, #392, #394 and #397; spec §8, §11, §13, §15 and §22.
- `docs/reference/project-automation.md` for the configuration this skill operates.
