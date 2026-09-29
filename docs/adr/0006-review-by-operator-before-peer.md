# ADR-0006: Review by the operator before a peer

- Status: Proposed; amended by [ADR-0009](0009-prove-reviews-with-meaningful-comments.md), which replaces the Viewed
  marks and per-person tokens with meaningful review comments
- Date: 2026-09-29
- Deciders: Gihed Annabi

## Context

An AI agent writes most RaidManager pull requests and opens them under the identity of the person who ran it, the
operator. [ADR-0005](0005-require-viewed-files-before-approval.md) requires one peer to view every file and approve,
but nothing requires the operator to read the agent's changes before asking someone else to. GitHub never lets an
author approve their own pull request, so the operator's review needs another signal.

## Decision

The operator reviews first, and a peer review is expected only after that.

- The operator is the pull-request author. Agent pull requests open as drafts. The operator marks every changed file
  as viewed, then marks the draft **Ready for review**. That event is the operator's sign-off.
- When a pull request becomes ready, the `review` job reads the operator's viewed marks with the operator's
  own token. If a file is not viewed, the pull request returns to draft with a comment listing the files. Otherwise
  every other listed reviewer is requested.
- A new commit on a ready pull request returns it to draft, so the operator reviews the new
  changes before a peer is asked again. Branch protection already dismisses the peer's approval.
- The required `review-files-viewed` status stays pending while the pull request is a draft or the operator has not
  viewed every file. A peer approval counts only when it is from someone other than the author, covers the head commit,
  was submitted after the latest Ready for review event, and follows the peer viewing every file.
- Each reviewer has their own read-only token in the `REVIEW_TOKEN_<LOGIN>` secret, created as ADR-0005 describes.
  The workflow's `REVIEWERS` list names the people who review. `REVIEW_GATE_TOKEN` is accepted for `anthermook`
  until `REVIEW_TOKEN_ANTHERMOOK` exists.
- The agent never marks files as viewed and never marks a pull request ready. Those actions are the operator's
  review, even though the agent could perform them with the operator's identity.

## Consequences

**Positive**

- Every merged change was read in full by the person who ran the agent and by a peer, in that order.
- A peer is never asked to review code its operator has not read.

**Negative**

- Each new commit costs a second operator pass before the peer sees it.
- Each reviewer maintains a token, and the workflow's `REVIEWERS` list and secrets change when people join or leave.

**Residual risk**

- The agent acts with the operator's GitHub identity, so the gate cannot tell the operator's clicks from the
  agent's. The rule that the agent never marks files viewed or pull requests ready is a process control, not a
  technical one.

## Alternatives considered

- **Operator submits a Comment review:** workable, but a draft pull request already means "not ready for others",
  and the Ready for review event gives the peer request a natural trigger.
- **Operator identified as always Gihed Annabi:** rejected because it breaks when another collaborator runs the agent.
