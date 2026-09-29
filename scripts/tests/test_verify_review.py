"""Behavior tests for the comment-proven, operator-then-peer review gate."""

from __future__ import annotations

import sys
import unittest
from datetime import datetime, timedelta, timezone
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from verify_review import (  # noqa: E402
    FAILURE, PENDING, SUCCESS, Change, InlineComment, PullRequest, Review, comment_problems, gate_result,
)


HEAD = "a" * 40
OPERATOR = "AnnabiGihed"
PEER = "anthermook"
READY_AT = datetime(2026, 9, 30, 9, 0, tzinfo=timezone.utc)
CHANGE = Change(
    ("src/Core/RaidManager.Domain/Features/Raids/Aggregates/Raid.cs", "CHANGELOG.md"),
    "+    public RaidSignupId SubmitSignup(UserId userId)\n+        EnsureNotLocked(readiness);\n",
)
OPERATOR_SUMMARY = "I checked that SubmitSignup in Raid.cs rejects a locked character and the changelog entry is accurate."
PEER_SUMMARY = "Verified EnsureNotLocked blocks locked characters in Raid.cs and that the tests cover the rejection path."


def review(author: str, state: str, body: str, minutes: int, commit: str = HEAD) -> Review:
    return Review(author, state, commit, READY_AT + timedelta(minutes=minutes), body)


def operator_review(minutes: int = -5, body: str = OPERATOR_SUMMARY) -> Review:
    return review(OPERATOR, "COMMENTED", body, minutes)


def approval(minutes: int = 5, body: str = PEER_SUMMARY, author: str = PEER, commit: str = HEAD) -> Review:
    return review(author, "APPROVED", body, minutes, commit)


def pull_request(
    reviews: list[Review],
    is_draft: bool = False,
    ready_at: datetime | None = READY_AT,
    comments: list[InlineComment] | None = None,
) -> PullRequest:
    return PullRequest(OPERATOR, is_draft, HEAD, ready_at, reviews, comments or [], CHANGE)


class CommentRuleTests(unittest.TestCase):
    def test_a_specific_summary_passes(self) -> None:
        self.assertEqual([], comment_problems(OPERATOR_SUMMARY, CHANGE, summary=True))

    def test_generic_praise_only_fails(self) -> None:
        for text in ("LGTM", "Looks good to me, thanks!", "Approved 👍", "+1", "ok"):
            with self.subTest(text=text):
                self.assertEqual(
                    ["it only contains generic praise such as LGTM or looks good"],
                    comment_problems(text, CHANGE, summary=True),
                )

    def test_an_empty_comment_fails(self) -> None:
        self.assertEqual(["it is empty"], comment_problems("  ", CHANGE, summary=True))

    def test_a_short_summary_fails(self) -> None:
        problems = comment_problems("Raid.cs change is fine", CHANGE, summary=True)
        self.assertIn("at least 10 are needed", problems[0])

    def test_keyboard_mash_fails(self) -> None:
        problems = comment_problems("asdf qwerty checked Raid.cs and the SubmitSignup change thoroughly today", CHANGE, summary=True)
        self.assertTrue(any("random text" in problem for problem in problems))

    def test_real_words_resembling_keyboard_runs_pass(self) -> None:
        text = "The property handling in Raid.cs keeps the liberty to retry the SubmitSignup call safely."
        self.assertEqual([], comment_problems(text, CHANGE, summary=True))

    def test_a_summary_that_names_nothing_from_the_change_fails(self) -> None:
        text = "Reviewed everything carefully and it all seems correct to me, no issues were found at all."
        self.assertEqual(
            ["it does not name a changed file or an identifier from the diff"],
            comment_problems(text, CHANGE, summary=True),
        )

    def test_a_backticked_term_from_the_diff_counts_as_naming_the_change(self) -> None:
        text = "The new `EnsureNotLocked(readiness)` call runs before the signup is stored, which is the right order."
        self.assertEqual([], comment_problems(text, CHANGE, summary=True))

    def test_inline_comments_need_fewer_words_and_no_file_name(self) -> None:
        self.assertEqual([], comment_problems("Please rename this variable to something clearer", CHANGE, summary=False))

    def test_quoted_text_does_not_count_as_the_commenters_words(self) -> None:
        text = "> I checked that SubmitSignup in Raid.cs rejects a locked character and more.\nLGTM"
        self.assertEqual(
            ["it only contains generic praise such as LGTM or looks good"],
            comment_problems(text, CHANGE, summary=True),
        )


class GateResultTests(unittest.TestCase):
    def test_operator_review_then_peer_approval_succeeds(self) -> None:
        self.assertEqual((SUCCESS, []), gate_result(pull_request([operator_review(), approval()])))

    def test_without_an_operator_review_the_gate_waits_on_the_operator(self) -> None:
        state, messages = gate_result(pull_request([], is_draft=True))
        self.assertEqual(PENDING, state)
        self.assertIn("waiting for @AnnabiGihed's review", messages[0])

    def test_after_the_operator_review_a_draft_still_waits_for_ready(self) -> None:
        state, messages = gate_result(pull_request([operator_review()], is_draft=True, ready_at=None))
        self.assertEqual((PENDING, ["waiting for @AnnabiGihed to mark the draft Ready for review"]), (state, messages))

    def test_an_operator_review_of_an_older_commit_does_not_count(self) -> None:
        stale = review(OPERATOR, "COMMENTED", OPERATOR_SUMMARY, -5, commit="b" * 40)
        self.assertEqual(PENDING, gate_result(pull_request([stale]))[0])

    def test_no_peer_approval_is_pending(self) -> None:
        self.assertEqual(
            (PENDING, ["waiting for a peer approval: someone other than @AnnabiGihed must approve"]),
            gate_result(pull_request([operator_review()])),
        )

    def test_a_generic_approval_fails_with_the_fix(self) -> None:
        state, messages = gate_result(pull_request([operator_review(), approval(body="LGTM")]))
        self.assertEqual(FAILURE, state)
        self.assertIn("edit @anthermook's approval comment", messages[0])

    def test_an_empty_approval_fails(self) -> None:
        self.assertEqual(FAILURE, gate_result(pull_request([operator_review(), approval(body="")]))[0])

    def test_a_generic_inline_comment_fails_and_names_the_file(self) -> None:
        comments = [InlineComment(PEER, "CHANGELOG.md", "nit")]
        state, messages = gate_result(pull_request([operator_review(), approval()], comments=comments))
        self.assertEqual(FAILURE, state)
        self.assertEqual("edit @anthermook's comment on CHANGELOG.md: it only contains generic praise such as LGTM or looks good", messages[0])

    def test_bot_comments_are_ignored(self) -> None:
        comments = [InlineComment("github-actions[bot]", "CHANGELOG.md", "ok")]
        self.assertEqual(SUCCESS, gate_result(pull_request([operator_review(), approval()], comments=comments))[0])

    def test_an_approval_before_the_sign_off_fails(self) -> None:
        state, messages = gate_result(pull_request([operator_review(), approval(minutes=-1)]))
        self.assertEqual((FAILURE, ["@anthermook approved before @AnnabiGihed's review and sign-off"]), (state, messages))

    def test_an_approval_of_an_older_commit_fails(self) -> None:
        state, messages = gate_result(pull_request([operator_review(), approval(commit="b" * 40)]))
        self.assertEqual(FAILURE, state)
        self.assertIn("not the current head", messages[0])

    def test_the_author_cannot_be_the_peer(self) -> None:
        self.assertEqual(PENDING, gate_result(pull_request([operator_review(), approval(author=OPERATOR)]))[0])

    def test_changes_requested_fails(self) -> None:
        request = review(PEER, "CHANGES_REQUESTED", "Please make EnsureNotLocked in Raid.cs report every locked target, not only the first.", 5)
        self.assertEqual((FAILURE, ["@anthermook requested changes"]), gate_result(pull_request([operator_review(), request])))

    def test_a_later_approval_replaces_a_change_request(self) -> None:
        request = review(PEER, "CHANGES_REQUESTED", "Please make EnsureNotLocked in Raid.cs report every locked target, not only the first.", 5)
        self.assertEqual(SUCCESS, gate_result(pull_request([operator_review(), request, approval(minutes=10)]))[0])


if __name__ == "__main__":
    unittest.main()
