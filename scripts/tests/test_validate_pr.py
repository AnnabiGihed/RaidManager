"""Behavior tests for pull-request description validation."""

from __future__ import annotations

import sys
import unittest
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from validate_pr import validate  # noqa: E402


VALID_BODY = """## What changed
Adds documentation gates.

## Why it changed
Closes #44.

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

    def test_missing_section_fails(self) -> None:
        self.assertTrue(any("five required sections" in error for error in validate(VALID_BODY.replace("## How it was tested", "## Tests"))))

    def test_unfilled_template_fails(self) -> None:
        self.assertTrue(any("unfilled template" in error for error in validate(VALID_BODY + "\nDescribe the user or maintainer-visible outcome.")))


if __name__ == "__main__":
    unittest.main()
