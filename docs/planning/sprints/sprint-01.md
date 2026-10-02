# Sprint 1

The planning record of Sprint 1, kept as the
[Work Management and Delivery Specification](../../reference/work-management-specification.md) requires (§7, §14).
The outcome sections are completed at the sprint review (`work-sprint-review-and-carryover`).

| Field | Value |
| --- | --- |
| Sprint | Sprint 1 (Project iteration `Sprint 1`) |
| State | Active |
| Start | 2026-10-02 00:00 Europe/Brussels, inclusive |
| End | 2026-10-16 00:00 Europe/Brussels, exclusive |
| Duration | Two weeks (specification §22) |
| Release | `v1.0` (draft scope, see the [release record](../releases/v1.0.md)) |
| Decided by | Gihed Annabi, on #323 (decisions 3 and 14) |

## Sprint Goal

Adopt the Work Management and Delivery Specification: skills, ADR, forms, Project fields and views, validators.

## Capacity assumptions

No sprint has been completed yet, so there is no velocity to forecast from (specification §9). The conservative
assumptions are:

- The sprint holds only the adoption improvement and its tasks, and no product story (owner decision on #323).
- No Story Points are forecast: the selected work is an improvement and its tasks, which carry no points.
- Each task is delivered through one pull request and reviewed by the owner; review time is the limiting factor, so
  tasks are ordered by their dependencies rather than run all at once.

## Selected scope

| Item | Type | Delivery Stage | Prerequisites |
| --- | --- | --- | --- |
| #323 Adopt the Work Management and Delivery Specification | Improvement | Architecture Analysis | none |
| #324 Commit the specification and its adoption ADR | Task | Architecture Analysis | none |
| #325 Create the eight work management skills | Task | Development | #324 |
| #326 Apply the specification to the issue forms, labels and hierarchy guard | Task | Development | #324 |
| #327 Configure the Project's Status values, fields and views | Task | Development | #324 |
| #328 Add the work management preflight and board report | Task | Development | #324, #327 |
| #329 Migrate the open items to the new Status, contracts, estimates and stages | Task | Business Analysis | #326, #327 |
| #330 Write the Sprint 1 record and draft the release v1.0 record | Task | Business Analysis | #324 |

Delivery risk of the same-sprint dependencies (specification §10): #328 and #329 can start only after #327 (and #326
for #329) are merged. If the owner's reviews leave too little time, they are the tasks at risk of staying unfinished.

## Assignment history

| Date | Change |
| --- | --- |
| 2026-10-02 | Sprint 1 created; #323 and #324 to #330 selected. |

## Outcome

To be recorded at the sprint review: goal achievement, completed work, unfinished work and replanning decisions,
each separately.
