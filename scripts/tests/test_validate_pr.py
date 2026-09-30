"""Behavior tests for pull-request description validation."""

from __future__ import annotations

import sys
import unittest
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from validate_pr import validate  # noqa: E402


VALID_BODY = """Closes #44

## What changed
Adds documentation gates.

## Why it changed
Completes the documentation work item.

## How it was tested
Unit and workflow checks passed.

## What to review carefully
Check the Pages publication path.

## Migration or deployment notes
The documentation site is published after merge.

### Author self-review
- [x] I reviewed the diff.
"""


class PullRequestValidationTests(unittest.TestCase):
    def test_valid_description_passes(self) -> None:
        self.assertEqual([], validate(VALID_BODY))

    def test_missing_task_reference_fails(self) -> None:
        self.assertTrue(any("Closes" in error for error in validate(VALID_BODY.replace("Closes #44", "Refs #44"))))

    def test_closing_example_in_body_does_not_count(self) -> None:
        body = VALID_BODY.replace("Closes #44\n\n", "").replace("Completes the documentation work item.", "For example: Closes #7.")
        self.assertTrue(any("Closes" in error for error in validate(body)))

    def test_missing_section_fails(self) -> None:
        self.assertTrue(any("five required sections" in error for error in validate(VALID_BODY.replace("## How it was tested", "## Tests"))))

    def test_unchecked_self_review_item_fails(self) -> None:
        body = VALID_BODY + "- [ ] I confirmed that no credentials or personal information are committed.\n"
        self.assertEqual(
            ["Author self-review item is not checked: I confirmed that no credentials or personal information are committed."],
            validate(body),
        )

    def test_ci_and_approval_item_may_stay_unchecked(self) -> None:
        body = VALID_BODY + "- [ ] Required CI checks pass on the latest commit, and an independent reviewer approved the PR.\n"
        self.assertEqual([], validate(body))

    def test_unfilled_template_fails(self) -> None:
        self.assertTrue(any("unfilled template" in error for error in validate(VALID_BODY + "\nDescribe the user or maintainer-visible outcome.")))


if __name__ == "__main__":
    unittest.main()
