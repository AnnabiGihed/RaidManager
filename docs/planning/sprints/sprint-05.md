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
| #16 Capture visited characters in the WoW addon | User story | 8 | 3 to 9 Oct | #38 (Architecture Analysis), #383, #487, #488, #489 (Development) | None |
| #374 Provision the OVH test server | Improvement | 5 | 5 to 7 Oct | #387 (Deployment) | #373 |
| #15 Pair and revoke a desktop companion | User story | 8 | 4 to 16 Oct | #382, #513, #514 (Development) | #37 |
| #153 Store and deliver deployment secrets | Improvement | 5 | 6 to 8 Oct | #386 (Deployment) | #103 |
| #19 Inspect and manage character profiles | User story | 5 | 7 to 10 Oct | #385, #545 (Development) | #38 |
| #375 Configure the test environment | Improvement | 3 | 8 to 9 Oct | #388 (Deployment) | #103, #374 |
| #17 Discover WoW accounts and upload snapshots reliably | User story | 8 | 10 to 13 Oct | #384, #550, #551 (Development) | #15, #16 |
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
| #491 Deploy to dev only when a merge changes what dev runs | Improvement | 3 | 4 to 5 Oct | #492 (Deployment) | None |
| #493 Every merge dispatches the main workflows twice | Bug | 1 | 4 Oct | #494 (Deployment) | None |
| #499 The snapshot contract calls gem enchantment ids gem ids | Bug | 1 | 4 Oct | #500 (Development) | None |
| #503 A missing equipment set item is recorded with `itemId` -1 | Bug | 1 | 4 Oct | #504 (Development) | None |
| #507 The snapshot contract misreads the difficulty of single-size raids | Bug | 1 | 4 Oct | #508 (Development) | None |
| #510 Keep the session's lessons and a standing new-session routine | Improvement | 2 | 4 Oct | #511 (Business Analysis) | None |
| #517 Keep the companion session's lessons in the skills | Improvement | 1 | 4 Oct | #518 (Business Analysis) | None |
| #520 Write the Avalonia skills for the desktop companion | Improvement | 2 | 4 to 5 Oct | #521 (Development) | None |
| #524 A running companion never learns it was revoked | Bug | 2 | 4 to 5 Oct | #525 (Development) | None |
| #526 The paired companions list misses a computer after Confirm | Bug | 1 | 6 Oct | #527 (Development) | None |
| #528 Tell the player the companion keeps running when its window closes | Improvement | 3 | 6 to 8 Oct | #529 (Functional Analysis), #530 (Development) | None |
| #532 Publish the companion through the Microsoft Store | Spike | 3 (2 days) | 9 to 10 Oct | #533 (Architecture Analysis) | None |
| #542 Keep the Avalonia companion session's lessons in the skills | Improvement | 1 | 4 to 5 Oct | #543 (Business Analysis) | None |
| #547 Keep the character profiles session's lessons in the skills | Improvement | 1 | 5 Oct | #548 (Business Analysis) | None |
| #554 Keep the snapshot upload session's lessons in the skills | Improvement | 1 | 5 Oct | #555 (Business Analysis) | None |
| #557 A pairing test of the companions list fails since 2026-10-05 | Bug | 1 | 7 Oct | #558 (Development) | None |
| #561 Run the repository's workflows on the owner's self-hosted runner | Improvement | 2 | 8 Oct | #562 (Development) | None |
| #564 The dev deployment fails on the self-hosted runners | Bug | 1 | 8 Oct | #565 (Development) | None |
| #567 `setup-tools` keeps an outdated `gh` from the runner image | Bug | 1 | 8 Oct | #568 (Development) | #565 |
| #571 Keep the background sync and self-hosted runner session's lessons in the skills | Improvement | 2 | 8 Oct | #572 (Business Analysis) | None |

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
| 2026-10-03 | #383 split on the owner's decision recorded on #16: #383 keeps the addon, its tools and identity; #487 (professions and gear), #488 (loadouts) and #489 (raid saves) follow it. #38 started early, on 3 October. |
| 2026-10-04 | #491 with #492 and bug #493 with #494 added on the owner's decisions recorded on them: deploy dev only when a merge changes what it runs, and dispatch the main workflows once per merge. |
| 2026-10-04 | #383 started; on the owner's decision recorded on #491 and #383, an addon change doesn't deploy dev. |
| 2026-10-04 | Bugs #499 with #500, #503 with #504 and #507 with #508 added on the owner's decisions recorded on them: defects the owner's in-game checks found in the contract and the addon. |
| 2026-10-04 | #510 with #511 added on the owner's decision recorded on #510: the session's lessons and the standing "new session" routine. |
| 2026-10-04 | #382 split on the owner's decision recorded on #15: #382 keeps the pairing API (ADR-0030 Accepted in its pull request), #513 takes the website's confirm and revoke pages (12 to 14 Oct) and #514 the Windows companion (14 to 16 Oct), both blocked by #382. |
| 2026-10-04 | #513 widened on the owner's decisions recorded on it: mockup boards for the missing states first, "No upload yet" until #384, expired companions, and Companion & sync in the sidebar. |
| 2026-10-04 | #517 with #518 added under the standing approval on #510: the companion session's lessons in the skills. |
| 2026-10-04 | #514 started with ADR-0032 (Proposed, approved by the owner): an Avalonia companion instead of WPF, so its tests run on Linux CI; a tray-menu board, close to tray and a self-contained executable, recorded on #514. |
| 2026-10-04 | #520 with #521 added under a reopened #259 on the owner's decision recorded on them: the Avalonia skills before #514 writes code, which now waits for #521; Avalonia build telemetry turned off everywhere. |
| 2026-10-04 | #514 widened on the owner's decisions recorded on it: boards 18 (tray menu), 19 (getting a code) and 20 (code request failed) added to the companion pairing mockup and confirmed; "Choose folders" hidden from state 6 until #384. |
| 2026-10-04 | #523 merged before the owner's check on dev, so #514 was reopened as Blocked. The check found four things; on the owner's decisions recorded on them, bugs #524 with #525 and #526 with #527 and improvement #528 with #529 and #530 were added before #385, and re-pairing the same computer stays as designed. #514 waits for #525. |
| 2026-10-04 | #531 merged before the owner's check too, so #525 was reopened as Blocked; the owner's check on dev then passed both ways (on opening and after 5 minutes), which closed #525, #524, #514 and story #15. |
| 2026-10-04 | Spike #532 with #533 added on the owner's decision recorded on it: Chrome and SmartScreen block the unsigned companion, so players will get it from the Microsoft Store, investigated first, after #526 and #528. |
| 2026-10-04 | #527 merged and was checked on dev after the merge, as the owner chose; #529 and #530 delivered the keeps-running notification (#528), the owner checking #530 on Windows before its merge. Spike #532 answered with ADR-0033, approved; its follow-ups (story #538, improvement #539 with #540 and #541) wait in the Backlog by owner decision. |
| 2026-10-04 | #542 with #543 added under the standing approval on #510: the session's lessons in the skills. |
| 2026-10-05 | #385 split on the owner's decision recorded on #19: it shows the pages (boards 1 and 2), and the new task #545 under #19, blocked by #385, edits them (boards 3 and 4). On the owner's decisions recorded on #19 and #385, only the owner opens a profile until #545, a profile is visible to the community by default, and the Equipment card lists every slot. |
| 2026-10-05 | #546 merged and closed #385, which was reopened as Blocked until the owner checks the pages on dev, as the owner chose on #385. The Microsoft Store work (#538, #539 to #541) stays in the Backlog: the owner confirmed the decision of 2026-10-04. |
| 2026-10-05 | #547 with #548 added under the standing approval on #510: the session's lessons in the skills. |
| 2026-10-05 | #384 split on the owner's decision recorded on it: #384 keeps the API that imports snapshots, and the new tasks #550 (the companion's background sync, blocked by #384) and #551 (its sync screens and the Choose folders button, blocked by #550) go under #17. Story #552 (GearScore and stats from an item catalog) was created under #130 and waits in the Backlog: on the owner's decisions recorded on #384, imports store what the addon reports, GearScore and stats show a dash until #552, and loadouts are the talent groups. |
| 2026-10-05 | #553 merged and closed #384; #550 started on the owner's decision recorded on it (search the usual folders, then let the player add one) and stopped at a safe checkpoint with no code when the session ended. |
| 2026-10-05 | #554 with #555 added under the standing approval on #510: the session's lessons in the skills. |
| 2026-10-07 | #556 had merged before the session began. On the owner's decisions recorded on #550, a queued snapshot of an excluded account is dropped, a snapshot RaidManager refuses with 400 is dropped and shown, and the newest snapshot of a character replaces an older one. |
| 2026-10-07 | Bug #557 with #558 added under #127 on the owner's decision recorded on it: a test of #527 failed every build from 2026-10-05; fixed first, in #559, before #550's pull request. |
| 2026-10-07 | #560 merged and closed #550. #551 started: on the owner's decisions recorded on it, the window follows each board's size, board 1 gains "Add a folder", and boards 6 to 8 show the refused, unsupported and unreadable cases; the owner confirmed the mockup in Penpot. |
| 2026-10-08 | #551 paused at its checkpoint for the owner's request: improvement #561 with #562 under #150, moving every workflow to the owner's self-hosted runners (ADR-0034), with a fork guard and the approval setting on the owner's decision recorded on #561, and jobs that install their own tools. |
| 2026-10-08 | #563 merged and closed #562 before its deployment criterion; the dev deployment failed, so bug #564 with #565 was added under #150 and #562 reopened as Blocked (owner decision on #564). The review merge of #566 failed on the image's old `gh`: the owner upgraded it, and bug #567 with #568 was added after #565 (owner decisions on #567). #566 and #569 merged, and #562 closed with the deployment's evidence. |
| 2026-10-08 | Improvement #570 added to the Backlog under #156 at the owner's request: automate the evidence comment later; spike #424 stays in the Backlog. |
| 2026-10-08 | #571 with #572 added under the standing approval on #510: the session's lessons in the skills; #551 resumes in the next session. |
| 2026-10-08 | #573 merged and closed #572 and #571. #551 resumed: on the owner's decisions recorded on it, a computer already paired opens on the sync screens, board 6 shows only after a new pairing, and a folder that isn't a WoW installation shows a warning on a new board 9 of the companion sync mockup. |
| 2026-10-08 | The owner's check of the companion build of #574 found board 1 running past the window, fixed in #574. On the owner's decisions recorded on #551, a whole WoW folder can be excluded (board 10, built in #574), and improvement #575 with task #576 was added to the Backlog under #130: My characters will show the characters waiting for review, which today appear only at the next sign-in as #18 specifies. |
| 2026-10-08 | The owner reported that website notifications never close and show too low: bug #577 with task #578 added under #163 and selected into Sprint 5 after #551, a notification closing after 6 seconds or with a close button (owner decisions on #577). #574 merged and closed #551, and #17 closed with its evidence. The owner then asked, at the highest priority, to stop the workflows running more often than needed with the same behavior: bug #579 with task #580 added under #150, in Sprint 5 before #578 and #545 (owner request on #579). |
| 2026-10-08 | #581 merged and closed #580. On the owner's decisions recorded on #579 and #582, task #582 was added under #579, in Sprint 5 before #578: the checks split into `pull-request` and `checks` so each runs only when its files changed, the required checks renamed (`description`, `docs`), and `project-hierarchy` starting once per issue opened, closed or reopened. |
| 2026-10-08 | #583 merged and closed #582; the owner updated the branch protection of `main` (`description`, `docs`), and feature #129 closed on the owner's decision recorded on it. The end-to-end check of task #584 (on #585) showed six runs per commit, a `checks` run re-testing each merge and `deploy-dev` running with nothing to deploy: task #586 added under #579 on the owner's decisions recorded there, with amendment A11 of the specification; #585 finishes its check after #586 merges. |
| 2026-10-08 | #587 merged and closed #586; the owner relaxed `description` for that merge only and added it back. #585 resumes #584's end-to-end check with the new expectations. |

## Outcome

To be recorded at the sprint review: goal achievement, completed work, unfinished work and replanning decisions, each
separately.
