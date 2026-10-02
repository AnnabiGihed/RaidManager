# Project automation

The [Raid Manager Project](https://github.com/users/AnnabiGihed/projects/2) applies the
[Work Management and Delivery Specification](work-management-specification.md)
([ADR-0026](../adr/0026-adopt-the-work-management-specification.md)). This page documents how the Project is
configured and what automation enforces; the specification is the authority for the rules themselves. Automation
runs without a personal token or repository secret (specification §22).

## Hierarchy

Every item sits in one chain of native sub-issues, Epic → Feature → User Story, Improvement, Bug or Spike → Task, with
exactly one `type:` label (specification §2, §3, §15). The issue forms set the label and ask for the parent; add the
new issue as a sub-issue of that parent at once.

## Fields

| Field | Type | Values | Specification |
| --- | --- | --- | --- |
| Status | Single select | Backlog, Ready, In Progress, In Review, Blocked, Done, Canceled | §11, §15 (A3) |
| Delivery Stage | Single select | Business Analysis, Functional Analysis, Architecture Analysis, Development, Testing, Deployment; on every task, and on stories, improvements, bugs and spikes as their current focus (the stage of their earliest open task) | §11 (owner decision, #394) |
| Sprint | Iteration | Two weeks from Sprint 5 (2026-10-03); Sprints 1 to 4 were one-day history sprints (#339) | §7, §22 |
| Story Points | Number | 1, 2, 3, 5, 8 or 13, on stories, improvements, bugs and spikes, never on tasks (#346) | §9 |
| Priority | Single select | P0 Critical, P1 High, P2 Normal, P3 Later, ordered highest first | §15 |
| Risk | Single select | Low, Medium, High, on open stories, improvements, bugs and spikes, the reason in a comment on the item | §10, §14 (owner decision, #394) |
| Start date, Target date | Date | The planned window of an open item: a selected item's window follows its sprint's dependency order, an unselected item takes its release's sprint window | §14 (owner decision, #394) |
| Area | Single select | Product areas, kept from before the specification | not in the specification |
| Milestone | Repository milestone | One per release, its due date the target date, its description linking the release record | §6, §15 |

Every item's Status value was kept when the field changed on 2026-10-02: the former `Todo` was renamed Backlog with the same
option, and the new values were added beside it. Change single-select options only with their option ids
(`work-board-configuration-and-validation`), or every item loses its value.

## Views

| View | Layout | Filter | Purpose (specification §17) |
| --- | --- | --- | --- |
| Product backlog | Table | `is:issue is:open -status:Done -status:Canceled` | All outstanding work by priority and hierarchy. |
| Release backlog | Table | outstanding items with `milestone:"v1.0"` | Outstanding items in the release, grouped under their parents. |
| Release progress | Table | `is:issue milestone:"v1.0"` | All release items, cancellations separate from completed delivery. |
| Sprint planning | Table | `is:issue is:open status:Ready,Backlog` | Candidates with estimates, stages and prerequisites. |
| Sprint backlog | Board | `is:issue sprint:@current` | The selected sprint's items, completed ones included, by Status. |
| Current sprint | Table | `is:issue sprint:@current` | Active sprint execution progress. |
| Future sprints | Table | `is:issue sprint:>@current` | Prepared upcoming sprint backlogs. |
| Delivery stages | Board | outstanding items | Items by Delivery Stage, showing Status. |
| Stabilization | Table | `is:issue sprint:"v1.0 stabilization"` | The release's final sprint quality work. |
| Blocked work | Table | `is:issue is:open status:Blocked` | Blocking reasons and prerequisites. |
| Scheduling violations | Table | `is:issue label:"scheduling-violation"` | Items the board report flags. |
| Roadmap | Roadmap | epics and features | Feature and epic progress and forecasts. |
| Release plan | Roadmap | stories, improvements, bugs and spikes | Every release on one timeline, like an Azure DevOps delivery plan. |
| Releases | Table | stories, improvements, bugs and spikes | Each release's items with their count and Story Points. |
| Sprints | Table | stories, improvements, bugs and spikes with a sprint | Each sprint's outcome items with their count and Story Points, like sprint backlogs. |

- Release plan, Releases and Sprints were added on 2026-10-02 at the owner's request (#343) so releases and sprints can
  be read at a glance:
  - Release plan groups by milestone and draws each item between its sprint's start and end, at Quarter zoom, with
    a marker for every sprint start and every release's due date. Items not yet selected into a sprint are listed in
    their release without a bar.
  - Releases groups by milestone and Sprints by sprint, each group showing its item count and its Story Points sum.
- The Stabilization view expects the release's last sprint to be titled `v1.0 stabilization`; change the filter if
  the release record names it otherwise.
- Capacity assumptions aren't a Project field: they are in the sprint record (`docs/planning/sprints/`).
- The `scheduling-violation` label is set and cleared by the agent's board report (specification §17, §22). An
  ordinary filter can't evaluate dates and cross-item rules.

## Rules

- **Parent:** every open issue has exactly one type label, every item except an epic has a parent of the allowed
  type, and an epic has none (specification §2).
- **Milestones** (specification §15, A2): a task shares its parent's milestone. Stories, improvements, bugs and
  spikes choose their release independently of their feature. A feature or epic has a milestone only when all its
  children are in that release; one that spans releases has none. A milestone change is checked at once on the
  changed issue, and on its parent by the next audit.
- **Contract** (specification §4): an open item created since the specification's adoption answers every heading its
  type requires, or `Unknown: needs clarification`. The issue forms ask exactly those headings. Older open items are
  completed by the migration (A7).
- **Dependencies** (specification §10): no dependency cycle, and no open item waiting on a prerequisite closed without
  being completed.
- **Releases** (specification §6, §13): a release milestone closes only after its record,
  `docs/planning/releases/<milestone>.md`, shows the state Released and an actual delivery date.
- **Completion:** an epic, feature, story, improvement, bug or spike may be closed as completed only when at least one
  child of the level below is closed as completed and every child is closed. A story, improvement, bug or spike
  therefore never closes without a completed task.
- **Pull requests:** a pull request closes tasks only (`Closes #N`), and each one must reach an epic through a
  story, improvement, bug or spike and a feature. Reference other items with `Refs #N`.
- A child closed as *not planned* or *duplicate* counts as closed but not as completed. An item closed that way is
  abandoned, so the completion and parent rules don't apply to it.

## User-interface mockups

Every item that changes what users see carries the `ui` label and links or shows its Penpot mockup
([ADR-0017](../adr/0017-penpot-mockups-for-ui-work.md), [how to design a screen](../how-to/design-a-screen.md)). A
mockup is `docs/mockups/<screen>.svg`, rendered from its `.penpot` file, shown as an image or link, or a Penpot
share link ([ADR-0018](../adr/0018-generate-penpot-mockups-in-the-repository.md)). A named SVG counts only once it
exists on `main`; until then the item keeps `needs-mockup`, and its comment names the missing file. The issue forms
ask **User interface**, and answering yes adds the `ui` label. The hierarchy workflow labels a `ui` item without a
mockup `needs-mockup`, with one comment, and removes the label once the mockup is linked. A pull request that changes
user-interface files must show its mockup, or state `No visual change:` with a reason.

## How the rules are enforced

The [hierarchy workflow](https://github.com/AnnabiGihed/RaidManager/blob/main/.github/workflows/project-hierarchy.yml)
runs with the built-in `GITHUB_TOKEN`:

- When an issue is closed or reopened, or its labels change, it checks that issue and its three ancestors. A task
  reopened under a completed story therefore reopens the story, its feature and its epic.
- Every 15 minutes it audits every issue, as a safety net for events it missed and for sub-issue changes, which
  start no workflow.
- It reopens an invalid parent with a comment that names the missing or open children.
- It labels a misplaced item, or one without a type label, `needs-parent` with one comment that says where it
  belongs; an item missing part of its contract `needs-contract`; an item in a dependency cycle or waiting on a
  canceled prerequisite `dependency-problem` (audit only); and a `ui` item without a mockup `needs-mockup`. It removes
  each label once the item is fixed. A new issue gets 10 minutes before it is flagged.
- When a milestone is closed, and in every audit, it reopens a release milestone whose record doesn't show the
  delivery yet.
- Rules that need Project fields, such as the active-sprint gate, Status against the close reason, estimates and
  Delivery Stage, run in the agent preflight and board report instead, because the token can't read a user-owned
  Project (specification §22).

## Agent preflight and board report

[`scripts/work_gate.py`](https://github.com/AnnabiGihed/RaidManager/blob/main/scripts/work_gate.py) reads the Project
with the owner's local `gh` login. Agents run it before every execution and at the start of every session, as the
scheduled check the workflow token can't do (specification §18, §22):

- `python scripts/work_gate.py preflight <task>` checks the seven conditions of the active-sprint gate
  (specification §8): hierarchy and contract, an active sprint (Europe/Brussels midnight, end exclusive, not canceled
  in its record), the same sprint and release as the parent, assignee and Delivery Stage, completed prerequisites, and
  an open item. It prints PASS or FAIL with the correction for each, and exits 1 on any failure.
- `python scripts/work_gate.py report` lists, by category:
  - scheduling violations: work in progress without any sprint, a task in another sprint than its parent, or an open
    pull request for a task without an active sprint;
  - Status and closure mismatches;
  - sprint and release mismatches: an item whose release (milestone) differs from its sprint's release, as listed
    in the Sprint sequence of the release records (owner rule, #346);
  - unestimated selected stories;
  - contract gaps on Ready or selected items;
  - unavailable prerequisites;
  - Blocked items without a recorded reason;
  - open items with contract gaps, which are allowed in Backlog.

  It exits 1 when any category other than the last has findings. With `--apply-labels`, it keeps the
  `scheduling-violation` label on exactly the items it flags, which the Scheduling violations view shows.

The docs `validate` check runs the pull-request rules on every pull request, so a task without a full chain, or a
user-interface change without its mockup, can't merge. After you fix a parent link, re-run that check from the pull
request's Checks tab. The review workflow closes only tasks when it merges.

The Project's built-in workflows run on GitHub's side and need no token. Closing and reopening never set Status by
themselves (specification §13, A4):

| Built-in workflow | Setting | Role |
| --- | --- | --- |
| Item closed | **Off** | It marked every closed item Done, canceled ones included (specification §15). |
| Item reopened | **Off** | It marked every reopened item In Progress, whatever its real state. |
| Auto-close issue | On | Setting Status to `Done` closes the issue as completed, so the guard checks it. |
| Item added to project | On | A new item starts in `Backlog`. |
| Auto-add sub-issues to project | On | A new child of a Project item joins the Project. |
| Pull request linked to issue, Pull request merged | On, unchanged | Must not set an issue to `Done`; checked in step 1 of the setup. |

Whoever closes or reopens an item sets its Status (`work-task-execution-and-completion`):

- completed: `Done`;
- not planned: `Canceled`, with the reason;
- reopened: the status that matches the remaining work.

The board report flags a mismatch between an item's close reason and its Status.

## One-time setup

The API can create fields and views, but it can't switch a workflow off or set a view's grouping, so the owner does
these steps once in the browser:

1. **Workflows:** open the Project menu, select **Workflows**, and switch off **Item closed** and **Item reopened**.
   Open **Pull request linked to issue** and **Pull request merged**: if either sets an issue's Status to `Done`,
   switch it off too, because only verified completion sets `Done` (specification §13).
2. **Views:** in each view, open the view menu and set:

   | View | Setting |
   | --- | --- |
   | Product backlog | Sort by Priority; show hierarchy. |
   | Release backlog | Group by Parent issue, so each item shows its parent without the parent joining the release (A2). |
   | Release progress | Group by Status, so Done and Canceled stay apart. |
   | Sprint planning | Group by Status; sort by Priority. |
   | Sprint backlog | Column by Status. |
   | Current sprint | Group by Status. |
   | Future sprints | Group by Sprint. |
   | Delivery stages | Column by Delivery Stage. |
   | Stabilization | Group by Status. |
   | Roadmap | Dates: Sprint (or Start date and Target date). |
   | Release plan | Group by Milestone; Dates: Sprint; Markers: Milestone and Sprint; Zoom level: Quarter. |
   | Releases | Group by Milestone; Field sum: Count and Story Points. |
   | Sprints | Group by Sprint; Field sum: Count and Story Points. |

## Verify the rules

1. Create a test issue labeled `type:story` with no children, and add it to the Project. It starts in `Backlog`.
2. Set its Project status to `Done`. The issue closes as completed.
3. Within a few minutes, the guard reopens it with a completion comment. Its Status stays `Done` until someone sets
   it, and the board report flags the reopened item marked `Done`.
4. After 10 minutes, the next audit labels it `needs-parent`, because it has no feature.
5. Close the test issue as *not planned* and set its Status to `Canceled`. It stays closed, and the next audit removes
   the label.
