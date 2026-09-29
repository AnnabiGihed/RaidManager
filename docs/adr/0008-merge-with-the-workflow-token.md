# ADR-0008: Merge approved pull requests with the workflow token

- Status: Proposed; [ADR-0009](0009-prove-reviews-with-meaningful-comments.md) renames the status to `review-gate`
  and adds closing linked issues and deleting the branch after merging
- Date: 2026-09-29
- Deciders: Gihed Annabi
- Supersedes: [ADR-0007](0007-queue-auto-merge-when-ready.md)

## Context

[ADR-0007](0007-queue-auto-merge-when-ready.md) queues auto-merge with `AUTO_MERGE_TOKEN`, a personal token owned by
Gihed Annabi that can write to the repository. It must be rotated, and a stolen copy acts as the owner.
[ADR-0004](0004-use-protected-automatic-squash-merges.md) rejected merging with the workflow token because a merge
made with it starts no push workflows, so documentation would not publish.

The `review` workflow already knows when a pull request may merge: it sets the required `review-files-viewed`
status. GitHub also allows the workflow token to start other workflows explicitly with `workflow_dispatch`.

## Decision

The `review` job merges a pull request itself with the workflow token, and no personal write token is used.

- When the job sets `review-files-viewed` to success, it squash-merges the pull request, pinned to the head commit
  it evaluated. It retries for a minute while GitHub takes the new status into account.
- Branch protection still decides. GitHub refuses the merge unless the operator's review, the peer approval, the
  required checks, and resolved conversations are all in place. The workflow token cannot bypass branch protection.
- If `build-test` or `validate` finishes after the approval, the completion of the `ci` or `docs` workflow for that
  pull request runs the job again through `workflow_run`, which evaluates the gate and merges then.
- After merging, the job starts `ci`, `docs`, and `docs-publish` on `main` with `workflow_dispatch`, which replaces
  the push event the workflow-token merge does not produce. Documentation therefore still publishes.
- `AUTO_MERGE_TOKEN` is no longer read. The owner deletes the secret and revokes the token.

## Consequences

**Positive**

- No personal write token exists to rotate, expire, or leak.
- The merge follows directly from the gate that already decides whether a pull request is ready.

**Negative**

- Merges are attributed to `github-actions` instead of the owner.
- Main's workflows start through `workflow_dispatch`, so their runs show that event instead of `push`.
- A conversation resolved after every other gate passed raises no event, so the merge waits for the next event,
  such as a re-submitted approval.

**Residual risk**

- The job's workflow token can write to the repository. Review events run the pull request's copy of `review.yml`,
  so a pull request could edit it; branch protection still blocks any merge without the required reviews and
  checks. The operator and the reviewer must view that file like any other.

## Alternatives considered

- **Keep `AUTO_MERGE_TOKEN` (ADR-0007):** works, but keeps a personal write token to rotate and protect.
- **GitHub App installation token:** avoids a personal token but adds an app, a private key secret, and more setup.
