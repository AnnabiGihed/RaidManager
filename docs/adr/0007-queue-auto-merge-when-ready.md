# ADR-0007: Queue auto-merge when the operator marks a pull request ready

- Status: Proposed
- Date: 2026-09-29
- Deciders: Gihed Annabi

## Context

[ADR-0004](0004-use-protected-automatic-squash-merges.md) has the task owner queue each ready pull request for an
automatic squash merge. Since [ADR-0006](0006-review-by-operator-before-peer.md), the operator marks the draft
Ready for review, and GitHub cannot queue auto-merge on a draft. Nobody queued the merge after that click, so an
approved pull request waited for a manual step. GitHub offers no repository setting that queues auto-merge on
every pull request, and merge queues are only available to organization-owned repositories.

ADR-0004 rejected a workflow that queues merges with the workflow token, because a merge queued that way may not
start the `main` push workflow that publishes documentation.

## Decision

The `review` workflow queues a squash auto-merge when a same-repository pull request into `main` becomes ready for
review and the operator's sign-off checks out. This replaces ADR-0004's manual queue step; everything else in
ADR-0004 still applies.

- It uses the owner's `AUTO_MERGE_TOKEN`: a fine-grained token owned by Gihed Annabi, limited to this repository,
  with Pull requests and Contents read and write. The merge happens as the owner, so the `main` push publishes the
  documentation as before.
- It runs on `pull_request_target`, so GitHub always uses `main`'s copy of the workflow and a pull request cannot
  change the step that holds the token. The job never checks out or runs pull-request code. Pull requests from forks
  are skipped.
- Queueing does not bypass anything. Branch protection still requires the operator's review, the peer approval,
  the required checks and resolved conversations. A pull request returned to draft cannot merge until it is ready
  again, which queues it again.

## Consequences

**Positive**

- An approved pull request merges without anyone returning to it.
- Marking a pull request ready remains the operator's only action after reviewing.

**Negative**

- The repository holds a token that can write. It must be rotated before it expires.
- The workflow change reaches `main` before it can run, so its own pull request is queued by hand.

**Residual risk**

- A stolen `AUTO_MERGE_TOKEN` acts as the owner. Branch protection stops it from pushing to `main` or skipping the
  required reviews and checks, but it could approve pull requests that another collaborator authored. The token's
  single-repository scope and expiry limit that exposure.

## Alternatives considered

- **Operator clicks Enable auto-merge after Ready for review:** no secret, but one more manual step on every pull
  request.
- **Workflow token:** rejected for the documentation-publishing reason in ADR-0004.
- **Merge queue:** not available to repositories owned by a personal account.
