"""Behavior tests for the operator-then-peer review gate."""

from __future__ import annotations

import sys
import unittest
from datetime import datetime, timedelta, timezone
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from verify_review_viewed import FAILURE, PENDING, SUCCESS, PullRequest, Review, gate_result, operator_errors, token_variable  # noqa: E402


HEAD = "a" * 40
OPERATOR = "AnnabiGihed"
PEER = "anthermook"
READY_AT = datetime(2026, 9, 29, 20, 0, tzinfo=timezone.utc)
ALL_VIEWED = {"src/Raid.cs": "VIEWED", "docs/index.md": "VIEWED"}


def pull_request(is_draft: bool = False, ready_at: datetime | None = READY_AT, reviews: list[Review] | None = None) -> PullRequest:
    return PullRequest(OPERATOR, is_draft, HEAD, ready_at, reviews or [])


def approval(author: str = PEER, commit: str = HEAD, minutes_after_ready: int = 5) -> Review:
    return Review(author, "APPROVED", commit, READY_AT + timedelta(minutes=minutes_after_ready))


def viewed(operator: dict[str, str] | None = ALL_VIEWED, peer: dict[str, str] | None = ALL_VIEWED) -> dict[str, dict[str, str] | None]:
    return {OPERATOR.lower(): operator, PEER.lower(): peer}


class OperatorReviewTests(unittest.TestCase):
    def test_draft_means_the_operator_has_not_finished(self) -> None:
        errors = operator_errors(pull_request(is_draft=True), ALL_VIEWED)
        self.assertEqual(1, len(errors))
        self.assertIn("mark the draft Ready for review", errors[0])

    def test_never_marked_ready_means_the_operator_has_not_finished(self) -> None:
        self.assertTrue(operator_errors(pull_request(ready_at=None), ALL_VIEWED))

    def test_operator_must_view_every_file(self) -> None:
        errors = operator_errors(pull_request(), {**ALL_VIEWED, "src/Character.cs": "UNVIEWED"})
        self.assertEqual(["not marked as viewed by @AnnabiGihed: src/Character.cs"], errors)

    def test_file_changed_after_the_operator_viewed_it_fails(self) -> None:
        errors = operator_errors(pull_request(), {**ALL_VIEWED, "src/Raid.cs": "DISMISSED"})
        self.assertEqual(["changed since @AnnabiGihed viewed it: src/Raid.cs"], errors)

    def test_missing_operator_token_names_the_secret(self) -> None:
        self.assertEqual(
            ["no review token for @AnnabiGihed: add the REVIEW_TOKEN_ANNABIGIHED repository secret"],
            operator_errors(pull_request(), None),
        )


class GateResultTests(unittest.TestCase):
    def test_peer_approval_after_the_operator_review_succeeds(self) -> None:
        self.assertEqual((SUCCESS, []), gate_result(pull_request(reviews=[approval()]), viewed()))

    def test_a_draft_is_pending_on_the_operator(self) -> None:
        state, messages = gate_result(pull_request(is_draft=True, reviews=[approval()]), viewed())
        self.assertEqual(PENDING, state)
        self.assertIn("waiting for @AnnabiGihed to finish their review", messages[0])

    def test_operator_files_not_yet_viewed_are_pending(self) -> None:
        state, _ = gate_result(pull_request(), viewed(operator={**ALL_VIEWED, "a.md": "UNVIEWED"}))
        self.assertEqual(PENDING, state)

    def test_missing_operator_token_fails(self) -> None:
        self.assertEqual(FAILURE, gate_result(pull_request(), viewed(operator=None))[0])

    def test_no_peer_approval_is_pending(self) -> None:
        self.assertEqual(
            (PENDING, ["waiting for a peer approval: someone other than @AnnabiGihed must approve"]),
            gate_result(pull_request(), viewed()),
        )

    def test_the_author_cannot_be_the_peer(self) -> None:
        self.assertEqual(PENDING, gate_result(pull_request(reviews=[approval(author=OPERATOR)]), viewed())[0])

    def test_peer_approval_before_the_operator_marked_ready_fails(self) -> None:
        self.assertEqual(
            (FAILURE, ["@anthermook approved before @AnnabiGihed marked the pull request ready"]),
            gate_result(pull_request(reviews=[approval(minutes_after_ready=-5)]), viewed()),
        )

    def test_peer_approval_of_an_older_commit_fails(self) -> None:
        state, messages = gate_result(pull_request(reviews=[approval(commit="b" * 40)]), viewed())
        self.assertEqual(FAILURE, state)
        self.assertTrue(any("not the current head" in message for message in messages))

    def test_peer_approval_without_viewing_every_file_fails(self) -> None:
        self.assertEqual(
            (FAILURE, ["not marked as viewed by @anthermook: a.md"]),
            gate_result(pull_request(reviews=[approval()]), viewed(peer={**ALL_VIEWED, "a.md": "UNVIEWED"})),
        )

    def test_changes_requested_by_a_peer_fails(self) -> None:
        review = Review(PEER, "CHANGES_REQUESTED", HEAD, READY_AT + timedelta(minutes=5))
        self.assertEqual((FAILURE, ["@anthermook requested changes"]), gate_result(pull_request(reviews=[review]), viewed()))

    def test_missing_peer_token_fails_and_names_the_secret(self) -> None:
        self.assertEqual(
            (FAILURE, ["no review token for @anthermook: add the REVIEW_TOKEN_ANTHERMOOK repository secret"]),
            gate_result(pull_request(reviews=[approval()]), viewed(peer=None)),
        )

    def test_login_matching_is_case_insensitive(self) -> None:
        self.assertEqual(SUCCESS, gate_result(pull_request(reviews=[approval(author="Anthermook")]), viewed())[0])


class TokenVariableTests(unittest.TestCase):
    def test_login_becomes_an_upper_case_variable_name(self) -> None:
        self.assertEqual("REVIEW_TOKEN_SOME_USER", token_variable("some-user"))


if __name__ == "__main__":
    unittest.main()
