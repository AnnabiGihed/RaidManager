# Review a pull request

Every pull request into `main` is reviewed twice: first by its operator, the person who ran the agent and authored
the pull request, then by a peer. Each review is proven by a meaningful review comment, and the required
`review-gate` status checks those comments. The reasons are recorded in
[ADR-0006](../adr/0006-review-by-operator-before-peer.md) and
[ADR-0009](../adr/0009-prove-reviews-with-meaningful-comments.md).

## Review as the operator

1. Wait until the agent reports that the draft pull request passes `description`, `docs`, `build-test` and `sonar`
   (a check skipped because the pull request doesn't change its files counts as passed).
2. Read every changed file in the **Files changed** tab.
3. Select **Review changes**, choose **Comment**, and describe what you checked. Name at least one changed file or
   an identifier from the diff.
4. Select **Ready for review**.

Without a meaningful review comment, the pull request returns to draft with a comment that gives the reason.
Otherwise the peer is asked to review. A new commit returns the pull request to draft, so repeat these steps for
the new changes.

## Review as the peer

1. Wait until the operator has marked the pull request ready. An approval given earlier does not count.
2. Read every changed file, and leave inline comments where something needs attention.
3. Select **Review changes**, choose **Approve** or **Request changes**, and describe what you checked, naming at
   least one changed file or an identifier from the diff.

When every gate passes, the `review` workflow merges the pull request, closes its task, deletes the branch, and
publishes the documentation. Nobody merges by hand.

## Write a comment that passes

A review comment, whether a review summary or an inline comment, must be meaningful:

- A summary has at least 10 words and an inline comment at least 5, not counting generic phrases.
- It is not only generic praise such as LGTM, looks good, ok, approved, +1, or an emoji.
- It contains no random text, such as a keyboard mash.
- A summary names a changed file or an identifier from the diff. An inline comment is already attached to a file.
- It does not copy another person's review comment, even with small edits. Write what you checked yourself.

| Comment | Result |
| --- | --- |
| Checked `review.yml`: the merge step closes linked issues and deletes the branch, as ADR-0009 describes. | Passes |
| Verified that `SubmitSignup` in Raid.cs rejects a character locked through raid start. | Passes |
| LGTM | Fails: only generic praise |
| Implementation looks correct, awaiting test results | Fails: 6 words, and it names nothing from the change |
| Reviewed everything carefully and it all seems correct to me, no issues were found at all. | Fails: it names nothing from the change |
| The operator's review comment, pasted as the approval comment | Fails: it repeats another person's comment |

Quoted text, which starts with `>`, and links do not count as your own words.

## Read the review-gate status

| Status | Meaning | What to do |
| --- | --- | --- |
| Pending: waiting for the operator's review | No meaningful operator review comment on the latest commit yet | The operator reviews and marks the draft ready |
| Pending: waiting for a peer approval | The operator's review is done | The peer reviews and approves |
| Failure: edit a comment | A review comment breaks a rule; the status names it and the fix | Edit that comment; the gate checks again |
| Failure: changes requested | A peer asked for changes | Push the fix; the operator reviews again |
| Failure: approved before the sign-off, or an older commit | The approval does not cover the current review | The peer approves again |
| Success | Both reviews are complete | Nothing; the pull request merges by itself |

If `review-gate` is green but the `review` job fails with "Every gate passed but GitHub refused the merge", GitHub
kept refusing the merge for about five minutes, for example with "Merge already in progress". Open the failed run in
the Actions tab and select **Re-run jobs**: the workflow merges on its own again. Don't merge by hand.
