---
name: work-task-execution-and-completion
description: >-
  Mandatory when executing a task and when closing or reopening any work item: status and Delivery Stage while
  working, blocked work, evidence, completion, cancellation, reopening, and independent validation of parents.
---

# Task execution and completion

The [Work Management and Delivery Specification](../../../docs/reference/work-management-specification.md) is the
authority; this skill applies it. The repository mechanics (branch, pull request, review, merge) are in
`raidmanager-github-project-workflow`.

## Triggers

- Starting, resuming, pausing or finishing work on a task.
- Closing, canceling or reopening any work item.
- A sprint ends while work is underway.

## Required inputs

- The task, its parent and the gate result of `work-sprint-planning-and-eligibility`.
- The task's completion conditions and verification method (spec §4).

## Preflight checks

1. The gate passes (`work-sprint-planning-and-eligibility`). If it fails, don't start.
2. Read spec §11 (Status and Delivery Stage) and §13 (completion, A4).

## Actions

1. **Start:** set Status `In Progress` and the task's Delivery Stage; reflect the current focus on the parent.
2. **Execute** through the repository workflow (`raidmanager-github-project-workflow`): one task, one branch, one
   draft pull request.
3. **In review:** set `In Review` when the deliverable awaits review or acceptance.
4. **Blocked:** set `Blocked`, keep the Delivery Stage, and comment with the reason, the person responsible and the
   unblock condition (spec §11, §14).
5. **Complete a task** when its deliverable is complete and its verification is evidenced (spec §13): verify the
   evidence, close the issue as completed, set Status `Done` (A4). A pull request's merge closes its task; check that
   the task shows `Done` afterwards.
6. **Cancel** an item: record the reason and its effect on the parent's scope, close it as not planned, and set
   Status `Canceled` (A3, A4).
7. **Reopen** an item: reassess the remaining work and set `Backlog`, `Ready`, `In Progress`, `In Review` or `Blocked`
   by the evidence; remove `Done` or `Canceled`; comment with the reason; pass the gate before resuming (A4). A
   reopened item meets the current contract before execution resumes (A7).
8. **Validate the parent independently** (spec §13, §18): close a story, improvement, bug or spike only when its own
   criteria are evidenced, its checks passed, and every task is closed with at least one completed. Close a feature,
   then an epic, the same way, one level at a time.
9. **When the sprint ends**, stop at a safe checkpoint: commit and push what exists, record the state on the task, and
   don't begin another execution action. Merging is execution (A5): an unmerged pull request waits for the next
   active sprint.

## Prohibited actions

- Executing outside an active sprint, or after the sprint's end.
- Marking an item `Done` because its sprint ended, its children closed, or a date passed (spec §13).
- Counting partial or canceled work as complete, or closing a canceled item as completed.
- Closing a parent whose own criteria lack evidence.

## Outputs and evidence

- The task's Status and Delivery Stage, kept current.
- Completion evidence on the task: the merged pull request, test and check results, and the criteria-to-evidence map.
- Cancellation and reopening reasons as comments.

## Failure behavior

- If verification fails, the task stays open in `In Progress` or `Blocked` with the failure recorded.
- If the evidence for a parent's criteria is missing, leave it open and report what is missing.

## Sources

- Spec §8, §11, §13 (A3, A4, A5), §18 and §20 (A7).
- [ADR-0026](../../../docs/adr/0026-adopt-the-work-management-specification.md).
- Owner decisions on #323.
