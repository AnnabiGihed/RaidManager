"""Behavior tests for the viewed-files review gate."""

from __future__ import annotations

import sys
import unittest
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from verify_review_viewed import Review, evaluate  # noqa: E402


HEAD = "a" * 40
REVIEWER = "anthermook"
ALL_VIEWED = {"src/Raid.cs": "VIEWED", "docs/index.md": "VIEWED"}


class ReviewGateTests(unittest.TestCase):
    def test_approval_of_head_after_viewing_every_file_passes(self) -> None:
        self.assertEqual([], evaluate(REVIEWER, HEAD, ALL_VIEWED, [Review(REVIEWER, "APPROVED", HEAD)]))

    def test_reviewer_login_is_case_insensitive(self) -> None:
        self.assertEqual([], evaluate(REVIEWER, HEAD, ALL_VIEWED, [Review("Anthermook", "APPROVED", HEAD)]))

    def test_unviewed_file_fails_and_is_named(self) -> None:
        files = {**ALL_VIEWED, "src/Character.cs": "UNVIEWED"}
        errors = evaluate(REVIEWER, HEAD, files, [Review(REVIEWER, "APPROVED", HEAD)])
        self.assertEqual(["not marked as viewed by @anthermook: src/Character.cs"], errors)

    def test_file_changed_after_viewing_fails(self) -> None:
        files = {**ALL_VIEWED, "src/Raid.cs": "DISMISSED"}
        errors = evaluate(REVIEWER, HEAD, files, [Review(REVIEWER, "APPROVED", HEAD)])
        self.assertEqual(["changed since @anthermook viewed it: src/Raid.cs"], errors)

    def test_approval_of_an_older_commit_fails(self) -> None:
        errors = evaluate(REVIEWER, HEAD, ALL_VIEWED, [Review(REVIEWER, "APPROVED", "b" * 40)])
        self.assertTrue(any("not the current head" in error for error in errors))

    def test_changes_requested_fails(self) -> None:
        errors = evaluate(REVIEWER, HEAD, ALL_VIEWED, [Review(REVIEWER, "CHANGES_REQUESTED", HEAD)])
        self.assertTrue(any("not an approval" in error for error in errors))

    def test_missing_review_fails(self) -> None:
        errors = evaluate(REVIEWER, HEAD, ALL_VIEWED, [Review("someone-else", "APPROVED", HEAD)])
        self.assertEqual(["@anthermook has not reviewed this pull request"], errors)


if __name__ == "__main__":
    unittest.main()
