# Sprint 5

The planning record of Sprint 5, kept as the [Work Management and Delivery
Specification](../../reference/work-management-specification.md) requires (§7, §14).

| Field | Value |
| --- | --- |
| Sprint | Sprint 5 (Project iteration `Sprint 5`) |
| State | Planned |
| Start | 2026-10-03 00:00 Europe/Brussels, inclusive |
| End | 2026-10-17 00:00 Europe/Brussels, exclusive |
| Duration | Two weeks (specification §22) |
| Release | [`v0.3`](../releases/v0.3.md), its first delivery sprint |

## Sprint Goal

Finish Discord sign-in on evidence, clean SonarCloud on `main`, and settle the companion threat model, the deployed
secrets store and how RaidManager runs on the OVH test server, so v0.3's pairing and deployment work can start on decided
ground.

## Capacity assumptions

No sprint has a recorded velocity yet; the planning assumption is 20 Story Points per two-week sprint (specification §9).
The retrospective points of Sprints 1 to 4 aren't used as velocity: they were estimated after one-day history sprints.
Three spikes have their own timebox (3, 3 and 2 working days). Since the owner's decision of 2 October
(#346), spikes carry Story Points too, so the sprint holds 19 points (#13 5, #209 5, #37 3, #103 3, #373 3), inside
the assumption.

## Selected scope

Selected on 2026-10-02 by the agent on the owner's delegation (#360, #361). Every item was Ready, with no contract
unknowns, before selection.

| Item | Type | Story Points or timebox | Tasks (Delivery Stage) |
| --- | --- | --- | --- |
| #13 Sign in with Discord and keep a secure session | User story | 5 | #100 (Architecture Analysis), #362 (Testing) |
| #209 Fix the SonarCloud findings on main | Improvement | 5 | #365 (Development) |
| #37 Define companion pairing and claim threat model | Spike | 3 days | #363 (Architecture Analysis) |
| #103 Decide how deployed secrets are stored and delivered | Spike | 3 days | #364 (Architecture Analysis) |
| #373 Choose how RaidManager runs on the OVH VPS-1 | Spike | 2 days | #380 (Architecture Analysis) |

Not selected, with the reason:

- #14 Link a community and enforce officer permissions: its remaining task #291 is blocked by the raid commands of
  #20 (v0.4).
- #15 Pair and revoke a desktop companion, #153 Store and deliver deployment secrets, #374 Provision the OVH test
  server and #375 Configure the test environment: they wait for spikes #37, #103 and #373; substantial prerequisites
  are completed in an earlier sprint (specification §10).
- #171 Resolve character ownership conflicts as an officer: its decision screen shows last uploads (#17) and rosters
  (release v1.0), which don't exist yet.

## Dependencies and delivery risk

No selected item depends on another. The three spikes run first, since #15, #153, #374 and #375 in Sprint 6 depend on
their findings; #380 needs the server's specifications from the owner. Risk: #209's 14 vulnerabilities may need more than one pull request.

## Assignment history

| Date | Change |
| --- | --- |
| 2026-10-02 | Created on the owner's decision on #339 (it replaces the two-week sprint that started on 2 October). |
| 2026-10-02 | Planned on the owner's delegation (#360): #13, #209, #37 and #103 selected with their tasks. |
| 2026-10-02 | Spike #373 and its task #380 added on the owner's decision (#378), so both deployment decisions finish in this sprint. |

## Outcome

To be recorded at the sprint review: goal achievement, completed work, unfinished work and replanning decisions, each
separately.
