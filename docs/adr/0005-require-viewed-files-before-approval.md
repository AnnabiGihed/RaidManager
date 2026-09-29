# ADR-0005: Require viewed files before approval

- Status: Proposed
- Date: 2026-09-29
- Deciders: Gihed Annabi

## Context

[ADR-0004](0004-use-protected-automatic-squash-merges.md) merges a pull request automatically once one independent
approval, the required checks, and resolved conversations are in place. An approval alone does not show that the
reviewer read every changed file. Branch protection also kept an approval after a later push, although ADR-0004
states that a later push should invalidate it.

GitHub records a file's **Viewed** mark per user and exposes it only to that user, through the
`viewerViewedState` GraphQL field. A workflow token cannot read another person's viewed marks.

## Decision

An approval counts only when the designated reviewer approved the current head commit after marking every changed
file as viewed.

- The `review` workflow's required `review-files-viewed` check reads the reviewer's viewed marks and latest review
  with the reviewer's own fine-grained, read-only token, stored as the `REVIEW_GATE_TOKEN` repository secret. It fails
  on any unviewed file, any file changed since it was viewed, and an approval of an older commit. It fails closed when
  the secret is missing.
- The check runs on pull-request updates and on review submission, edit, or dismissal. Viewing a file raises no
  event, so the reviewer submits the approval after viewing every file, or re-submits it to re-run the check.
- The workflow always runs the gate script from `main`, so a pull request cannot change the code that reads the
  reviewer token.
- Branch protection dismisses stale approvals on a new push, requires approval of the most recent push by someone
  other than its pusher, and requires linear history.
- The pull-request description check rejects unchecked author self-review items, except the CI and approval item,
  and runs again when the description is edited.

## Consequences

**Positive**

- A merge proves that the reviewer opened every changed file of the exact commit being merged.
- A push after approval requires a fresh review of the changed files.
- Authors cannot open a pull request with self-review items left unchecked.

**Negative**

- The review gate depends on one person's token. It must be rotated before it expires, and replaced when the
  designated reviewer changes. Until the secret exists, the check cannot pass, so it is made required only afterwards.
- The reviewer must approve, or re-approve, after marking the last file as viewed.
- The Viewed mark shows that a file was opened, not how carefully it was read.

**Residual risk**

- A pull request can still edit `.github/workflows/review.yml`. On `pull_request` events GitHub runs the workflow
  definition from the pull request, which could skip the check or expose the reviewer token. The reviewer must view
  that file like any other before approving. Scoping the token to this repository with read-only pull-request access
  limits the harm.

## Alternatives considered

- **Reviewer checklist in the description:** rejected because anyone who can edit the description can tick it.
- **Required code-owner review:** it adds no guarantee with a single reviewer, and does not prove files were viewed.
- **GitHub App or bot token:** rejected because viewed marks are private to the viewing user.
