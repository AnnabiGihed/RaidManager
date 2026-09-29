# ADR-0009: Prove reviews with meaningful comments

- Status: Proposed
- Date: 2026-09-30
- Deciders: Gihed Annabi
- Supersedes: [ADR-0005](0005-require-viewed-files-before-approval.md)
- Amends: [ADR-0006](0006-review-by-operator-before-peer.md)

## Context

[ADR-0005](0005-require-viewed-files-before-approval.md) proves a review by the reviewer's Viewed marks. GitHub
shows those marks only to the person who made them, so every reviewer keeps a personal token in a repository
secret. The tokens expire, need rotating, and are the last personal credentials the review process depends on. A
Viewed mark also only shows that a file was opened, not what the reviewer concluded.

Reviews and review comments are readable with the workflow token, and a comment shows what the reviewer checked.

## Decision

A review is proven by meaningful review comments, and the Viewed marks and every review token are dropped.

- The operator reviews first: a review comment on the head commit that explains what they checked, then Ready for
  review on the draft. Marking a draft ready without that comment returns it to draft with the reason.
- The peer approves the head commit with a comment, after the operator's review comment and the Ready for review
  event.
- Every human review comment must be meaningful, both review summaries and inline comments. A comment fails the
  gate when it:
  - is shorter than 10 words for a summary or 5 words for an inline comment, after removing generic phrases;
  - contains only generic praise such as LGTM, looks good, ok, approved, +1, or an emoji;
  - contains random text: a keyboard mash, or many words without a pronounceable shape;
  - is a review summary that names neither a changed file nor an identifier from the diff. An inline comment is
    already anchored to a changed file.
- A failing comment turns the required `review-gate` status red and names the comment and the fix. Editing the
  comment re-evaluates the gate, so no new review is needed.
- The gate reads reviews, comments, the diff, and the Ready for review event with the workflow token. The
  `REVIEW_TOKEN_*` and `REVIEW_GATE_TOKEN` secrets are deleted and their tokens revoked.
- The required status is renamed from `review-files-viewed` to `review-gate`.
- Because a merge made with the workflow token ([ADR-0008](0008-merge-with-the-workflow-token.md)) triggers
  neither closing keywords nor branch deletion, the job closes the linked issues and deletes the branch itself.
- The agent never writes a review or review comment in the operator's name. The operator's comment is their own
  review.

## Consequences

**Positive**

- No personal token remains in the review or merge process.
- A review leaves a written record of what was checked, which a Viewed mark never did.
- Random or generic comments cannot count as a review.

**Negative**

- The rules are heuristics. A determined person can still write a plausible comment without reading the change;
  the rules only stop empty, generic, or unrelated ones.
- Short but useful inline remarks, such as a one-word typo fix, need a few more words.

**Residual risk**

- The agent acts with the operator's identity, so the gate cannot tell the operator's review comment from one the
  agent wrote. The rule that the agent never writes it is a process control.

## Alternatives considered

- **Keep the Viewed marks (ADR-0005):** strict about opening every file but needs a personal token per reviewer.
- **Judge comments with a language model:** better at "meaningful" but needs an API key, which is another token.
