# Sprint 5

The planning record of Sprint 5, kept as the [Work Management and Delivery
Specification](../../reference/work-management-specification.md) requires (§7, §14).

| Field | Value |
| --- | --- |
| Sprint | Sprint 5 (Project iteration `Sprint 5`) |
| State | Active |
| Start | 2026-10-03 00:00 Europe/Brussels, inclusive |
| End | 2026-10-17 00:00 Europe/Brussels, exclusive |
| Duration | Two weeks (specification §22) |
| Release | [`v0.3`](../releases/v0.3.md), its first delivery sprint |

## Sprint Goal

Finish Discord sign-in on evidence and clean SonarCloud on `main`; settle the companion threat model, the deployed
secrets store and the OVH runtime layout; then deliver the character sync journey (pairing, addon capture, upload,
profiles) and RaidManager's first automatic deployment to the OVH test environment, with its status on GitHub and the
Project.

## Capacity assumptions

The assumption is 70 Story Points per two-week sprint, raised from 20 on the owner's decision of 2 October (#390), from
the delivery pace of Sprints 1 to 4. It isn't a measured velocity: Sprints 1 to 4 were one-day history sprints
estimated afterwards, so Sprint 5 is the first sprint whose completed points count as velocity (specification §9).
Spikes carry Story Points (#346) and keep their timebox.

The sprint held 75 points at its start: 72 of delivery work and 3 of planning (#390, #394). That is 5 above the
assumption, a risk the owner accepted when adding #392 and #394; the sprint review re-plans what doesn't finish.

On 3 October, the owner's decisions added 11 points (#409, #415, #417, #421, #436), so the sprint holds 86 points, 16
above the assumption, which is unchanged.

## Selected scope

Every item was Ready, with no contract unknowns, before selection. Each task's planned window (Start date to Target
date on the Project, both inclusive) follows the blocked-by order; the item's own window spans its tasks'.

| Item | Type | Story Points | Planned window | Tasks (Delivery Stage) | Waits for |
| --- | --- | --- | --- | --- | --- |
| #390 Record the full Sprint 5 selection | Improvement | 1 | 3 Oct | #391 (Business Analysis) | None |
| #394 Record stage, risk and planned dates on the Project | Improvement | 2 | 3 Oct | #395 (Business Analysis) | None |
| #37 Define companion pairing and claim threat model | Spike | 3 (3 days) | 3 to 5 Oct | #363 (Architecture Analysis) | None |
| #103 Decide how deployed secrets are stored and delivered | Spike | 3 (3 days) | 3 to 5 Oct | #364 (Architecture Analysis) | None |
| #373 Choose how RaidManager runs on the OVH VPS-1 | Spike | 3 (2 days) | 3 to 4 Oct | #380 (Architecture Analysis) | None |
| #13 Sign in with Discord and keep a secure session | User story | 5 | 3 to 6 Oct | #100 (Architecture Analysis), #362 (Testing) | None |
| #209 Fix the SonarCloud findings on main | Improvement | 5 | 3 to 7 Oct | #365 (Development) | None |
| #16 Capture visited characters in the WoW addon | User story | 8 | 5 to 9 Oct | #38 (Architecture Analysis), #383 (Development) | None |
| #374 Provision the OVH test server | Improvement | 5 | 5 to 7 Oct | #387 (Deployment) | #373 |
| #15 Pair and revoke a desktop companion | User story | 8 | 6 to 9 Oct | #382 (Development) | #37 |
| #153 Store and deliver deployment secrets | Improvement | 5 | 6 to 8 Oct | #386 (Deployment) | #103 |
| #19 Inspect and manage character profiles | User story | 5 | 7 to 10 Oct | #385 (Development) | #38 |
| #375 Configure the test environment | Improvement | 3 | 8 to 9 Oct | #388 (Deployment) | #103, #374 |
| #17 Discover WoW accounts and upload snapshots reliably | User story | 8 | 10 to 13 Oct | #384 (Development) | #15, #16 |
| #376 Deploy main to the dev environment automatically | Improvement | 8 | 3 to 9 Oct | #444 (Development), #389 (Development), #445 (Deployment) | #374, #388 |
| #392 Show deployment status on GitHub and the Project | Improvement | 3 | 3 Oct | #393 (Deployment) | #376 |
| #415 Keep the server provisioning lessons in the skills | Improvement | 2 | 3 Oct | #416 (Business Analysis) | None |
| #417 provision.sh lists the SSH rule among the other firewall rules | Bug | 1 | 3 Oct | #418 (Development) | None |
| #421 Keep closing keywords out of pull request prose | Improvement | 1 | 3 Oct | #422 (Business Analysis) | None |
| #436 Record the day's planning and automation changes | Improvement | 2 | 3 Oct | #437 (Business Analysis) | None |
| #409 Move the database from SQL Server to PostgreSQL | Improvement | 5 | 5 to 7 Oct | #410 (Architecture Analysis), #411 (Development) | None |
| #455 Keep the session's deployment and review lessons in the skills | Improvement | 1 | 3 Oct | #456 (Business Analysis) | None |
| #460 Update pinned GitHub Actions automatically | Improvement | 3 | 3 to 5 Oct | #462 (Development) | None |
| #461 Keep pinned GitHub Actions current | Improvement | 1 | 3 to 17 Oct | generated per update (#469 to #473 canceled, #483) | None |
| #474 validate misses the Closes line of a description saved with `\r\n` line endings | Bug | 1 | 3 Oct | #475 (Development) | None |
| #476 The generated Dependabot description mentions a pinned commit id for tag updates | Bug | 1 | 3 Oct | #477 (Development) | None |
| #484 Keep the merge-order and branch-update lessons in the skills | Improvement | 1 | 3 Oct | #485 (Business Analysis) | None |

16 October is kept as a buffer.

Not selected, with the reason:

- #14 Link a community and enforce officer permissions: moved to v0.4 (#367), since its remaining task #291 needs the
  raid commands of #20.
- #171 Resolve character ownership conflicts as an officer, and #18 Review new characters and resolve ownership
  conflicts: their remaining criteria need rosters, which come in release v1.0 (#390).

## Dependencies and delivery risk

The "Waits for" column lists the blocked-by links on the board, on the item or its task (#19 through #385). The three
spikes and #38's snapshot schema run first, since half the sprint builds on them; #380 needs the server's specifications
from the owner, and #374, #375 and #153 need steps only the owner can take (OVH, DNS, the Discord redirect, secrets).
Each outcome item carries a Risk value on the Project with its reason in a comment (#394). The items rated High are
stories #15, #16 and #17 and improvement #376, which form the two long chains of the sprint. The 14 vulnerabilities
of improvement #209 may need more than one pull request.

On 3 October the owner put the deployment chain first (#369): #387 and #386, then #388, then #389, with #363 done
while a deployment task waits on an owner step. #409 (PostgreSQL) goes before the test environment's settings: #375
and its task #388 wait for #409 and #411.

## Assignment history

| Date | Change |
| --- | --- |
| 2026-10-02 | Created on the owner's decision on #339 (it replaces the two-week sprint that started on 2 October). |
| 2026-10-02 | Planned on the owner's delegation (#360): #13, #209, #37 and #103 selected with their tasks. |
| 2026-10-02 | Spike #373 and its task #380 added on the owner's decision (#378), so both deployment decisions finish in this sprint. |
| 2026-10-02 | Every doable v0.3 item added on the owner's decision (#390): #15, #16, #17, #19, #153, #374, #375 and #376 with their tasks, and #390 to record it; the capacity assumption rises to 70 points. |
| 2026-10-02 | #392 added on the owner's decision (#390), after #376. |
| 2026-10-02 | #394 added on the owner's decision recorded on it: Delivery Stage, Risk and planned dates on the Project. |
| 2026-10-03 | The deployment chain moved first on the owner's decision on #369. |
| 2026-10-03 | #409 with #410 and #411 added on the owner's decision recorded on #409: PostgreSQL instead of SQL Server, at P1, so three environments fit the shared server. |
| 2026-10-03 | #415 with #416, and bug #417 with #418, added on the owner's decision recorded on them: the provisioning lessons in the skills and the script's firewall message, split by intent. |
| 2026-10-03 | #421 with #422 added on the owner's decision recorded on #421: keep closing keywords out of pull request prose. |
| 2026-10-03 | #436 with #437 added on the owner's decision recorded on #436: record the day's selections, the automation coverage and the skill pitfalls; #410 narrowed to its ADR. |
| 2026-10-03 | #376 pointed at dev (owner decision on #369) and its task #389 split, with the owner's approval, into #444 (the Compose files), #389 (the workflow) and #445 (the first deployment's verification); #376 and #389 now wait for #388 instead of #375, which closes after #445. |
| 2026-10-03 | #392 and #393 widened to dev, test and production, raised to P0 and moved first, on the owner's decisions recorded on #392: a label per environment on every delivered item, parent and release, a failure label, and the dev backfill. |
| 2026-10-03 | #455 with #456 added on the owner's decision recorded on #455: the session's lessons in the skills before a new session. |
| 2026-10-03 | #460 with #462 added on the owner's decisions recorded on #460: Dependabot updates of pinned actions, each with its own task under #461, which the agent selects into the active sprint as its tasks appear. |
| 2026-10-03 | #461 selected with the generated tasks #469 to #473 (later canceled when grouping replaced them) and #483, under the standing approval on #460; #480 added to #460 on the owner's grouping decision. |
| 2026-10-03 | Bugs #474 with #475 and #476 with #477 added at P0 on the owner's decision recorded on them: `validate` failed on a description saved in the browser, and the generated wording was wrong for tag updates. |
| 2026-10-03 | #484 with #485 added on the owner's decision recorded on #484: the merge-order and branch-update lessons in the skills. |

## Outcome

To be recorded at the sprint review: goal achievement, completed work, unfinished work and replanning decisions, each
separately.
