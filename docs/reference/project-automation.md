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
| Start date, Target date | Date | Planned, then actual. Until work starts, the planned window: a selected item's window follows its sprint's dependency order, an unselected item takes its release's sprint window (#394). When work starts, Start date becomes that day, and a parent starts with its first child; when the item closes, Target date becomes the closing day (Europe/Brussels) (#397) | §14 (owner decisions, #394, #397) |
| Assignees | Issue assignees | The author, `@AnnabiGihed`, on every item from the day work on it starts (#397) | §4, §14 |
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
| Deployed to dev | Table | `is:issue label:"deployed:dev"` | What runs in dev (§22, A9). |
| Deployed to test | Table | `is:issue label:"deployed:test"` | What runs in test (§22, A9). |
| Deployed to production | Table | `is:issue label:"deployed:production"` | What runs in production (§22, A9). |
| Failed deployments | Table | `is:issue label:"deploy-failed:dev","deploy-failed:test","deploy-failed:production"` | Items a failed deployment didn't deliver (§22, A9). |
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

## Deployment status

Every deployment marks what it delivered, so the Project shows what runs in each environment (#392, specification
§22, A9). The deployment workflow's `record` job runs `scripts/record_deployment.py` after the deployment, whether
it succeeded or failed, with the built-in token.

A merge deploys dev only when it changes a file that can affect the deployment (#491, A10): anything under `src/`
except the WoW addon in `src/Addon/`, which players install in the game, anything under `deploy/`, the build files
(`Directory.Build.props`, `Directory.Packages.props`, `nuget.config`, `global.json`, `dotnet-tools.json`,
`RaidManager.sln`) or `deploy-dev.yml`. The `changes` job of `deploy-dev` runs `scripts/deploy_changes.py`, which
compares `main` with the commit of the last successful dev deployment, so a change from a failed or skipped run is
deployed by the next one. When nothing deployed changed, the build and deployment are skipped and `record` still runs
as a success, since dev already runs the same images. A run started by hand from the Actions tab always deploys. The
labels follow these rules:

| Label | Color | Meaning |
| --- | --- | --- |
| `deployed:dev` | Light green | Delivered by a successful dev deployment. |
| `deployed:test` | Light blue | Delivered by a successful test deployment. |
| `deployed:production` | Green | Delivered by a successful production deployment. |
| `deploy-failed:<environment>` | Red | A deployment that would have delivered it failed; the next success removes it. |

- **Items:** a completed item is delivered when every merged pull request that closed it is in the history of the
  deployed commit; one closed without a pull request counts once it is closed. A completed parent is delivered
  when all its completed children are; canceled children don't count. A reopened item loses its label.
- **Releases:** a release milestone whose issues are all closed and delivered gets a line in its description,
  `Deployed to <environment>: <date>, <run>, <commit>`, between `deployments` markers; the line goes away if that
  stops being true. Released still needs the release record and the owner's approval (specification §6, §13).
- **Comments:** each newly labeled item gets a comment linking the run, except during the first run in an
  environment, which labels everything delivered before it without comments.
- The labels are only ever set by the workflow, never by hand. Each environment's labels are independent, so a
  card shows every environment its item runs in. The test and production workflows (#427, #433) run the same job.

## Dependency updates

Dependabot proposes the new releases of the GitHub Actions the workflows use every Monday, all in one grouped pull
request that keeps commit-id pins and their version comment (`.github/dependabot.yml`, #460), so the owner reviews
once a week. Every pull request closes a task, so the
`dependency-task` workflow runs `scripts/dependency_task.py` with the built-in token:

- **Opened or reopened:** it creates a task under the standing improvement #461, with its contract, the improvement's
  milestone and the owner as assignee, and writes `Closes #<task>` and the five required sections into the pull
  request. The self-review checklist is left for the owner to tick.
- **Closed without merging,** usually because a newer release replaced it: it closes the task as not planned with a
  comment. A rerun creates nothing twice.

The rest follows the usual rules, with two differences recorded on #460. The owner is the operator of a Dependabot
pull request: they tick its checklist while reviewing, which re-runs `validate`, post their review comment and mark
it ready, and a peer approves (`verify_review.py`, `review`). The built-in token can't write Project fields, so the
board report lists the open pull request until the agent selects #461 and the task into the active sprint, with the
owner's standing approval. Dependabot's branches, `dependabot/github_actions/...`, are the one exception to the
branch names in `CONTRIBUTING.md`. Its runs see only Dependabot secrets, so the package feed credentials the `ci`
build needs are also stored there.

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

## Automation coverage

As of 2026-10-03, at the owner's request. **Fully automated** means neither the owner nor the agent does anything; **not
automated** means one of them does it. Each gap the owner wants automated has a work item.

### Fully automated

| Area | Behavior | Done by |
| --- | --- | --- |
| Pull requests | Check formatting, build, run every test | `ci` |
| Pull requests | Measure coverage, comment it, fail under 80% of changed lines or 60% in total | `ci` |
| Pull requests | Fail on any SonarCloud finding | `ci` (`sonar` job), SonarCloud |
| Pull requests | Check the description, the linked task's hierarchy and the mockup of user-interface changes | `docs` |
| Pull requests | Check spelling, prose, Markdown, links, the OpenAPI contract, the site and wiki build, and identical skill trees | `docs` |
| Review | Return a ready pull request to draft on new commits | `review` |
| Review | Check the operator's comment when the pull request is marked ready, then request the peer | `review` |
| Review | Keep the `review-gate` status current | `review` |
| Review | Merge once every check and both reviews pass, delete the branch, close the linked tasks, run the checks on `main` | `review` |
| Documentation | Publish GitHub Pages and the wiki after each merge | `docs-publish` |
| Dependencies | Propose the week's new releases of GitHub Actions as one grouped pull request; give it a task under #461 and its description; cancel the task of an update closed without merging | Dependabot, `dependency-task` |
| Documentation | Check external links every Monday | `docs-links` |
| Board | Check type, parent, contract, dependencies and mockup; keep the rule labels current | `project-hierarchy` |
| Board | Reopen a parent closed too early, and a release milestone closed before its record shows the delivery | `project-hierarchy` |
| Board | Start a new item in Backlog, add a new sub-issue to the Project | Project workflows |
| Board | Set a task In Progress when a pull request links it, and close an issue set to Done | Project workflows |
| Deployment | Deploy `main` to dev after a merge that changes a deployed file, smoke-test it, put the previous version back on a failure | `deploy-dev` |
| Deployment | Label the delivered items, their parents and releases `deployed:dev`, or `deploy-failed:dev` on a failure | `deploy-dev` (`record` job) |
| Server | Install security updates daily | `unattended-upgrades` |
| Server | Obtain and renew the six RaidManager certificates | shared Caddy |
| Server | Back up the server daily | OVH Automated Backup |

### Not automated

| Area | Behavior | Who | Work item to automate it |
| --- | --- | --- | --- |
| Work items | Create, classify, link and write contracts; run the active-sprint gate; set Status, dates, assignee, sprint, estimates, Risk, stage and priority | Agent | none (judgment) |
| Work items | After a merge, confirm it and delete the local branch | Agent | none |
| Work items | Write the evidence comment, set Done and the Target date on the merged task | Agent | #424, #425 |
| Work items | Validate and close parents against their criteria | Agent | #426 |
| Work items | Run the board report and keep `scheduling-violation` current | Agent | #424 |
| Work items | Plan sprints and releases and write their records | Agent | none (decisions) |
| Changes | Branch, implement, check locally, open the draft pull request, fix findings, draft both review comments | Agent | none |
| Review | Post the operator's review comment and mark the pull request ready | Owner | none (by design) |
| Review | Approve the pull request | Peer | none (by design) |
| Deployment | Deliver the secrets to the server | Owner | #153, #386 |
| Deployment | Configure each environment | Agent and owner | #375, #388 |
| Deployment | Deploy each release to dev and test | Agent and owner | #427 |
| Deployment | Confirm a release and deploy it to production | Owner | #432, #433 |
| Deployment | Apply database migrations safely, with rollback | Agent | #434 |
| Deployment | Record test and production deployments on the items (the `record` job in their workflows) | Agent | #427, #433 |
| Releases | Tag, publish the GitHub release and update the release record | Agent | #435 |
| Server | Provision the server, install the deploy key | Owner | none (done once, #374; key with #386) |
| Dependencies | Select each update's task, and #461, into the active sprint; set a canceled update's Status | Agent | none (§22: no token for Project fields) |
| Dependencies | Review an update as its operator and tick its checklist | Owner | none (by design) |
| Dependencies | Update NuGet packages and container images | Agent | none yet |
| Other | Fix a broken external link after the weekly report | Agent | none |

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
