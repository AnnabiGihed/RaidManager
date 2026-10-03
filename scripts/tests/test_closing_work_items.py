"""Regression tests for pull-request closing references."""

from __future__ import annotations

import sys
import unittest
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from closing_work_items import closing_numbers  # noqa: E402


class ClosingWorkItemsTests(unittest.TestCase):
    def test_only_standalone_preamble_references_close(self) -> None:
        body = "Closes #91\n\n## How it was tested\nAn example: `Fixes #7`, `resolves #12`.\n"
        self.assertEqual([91], closing_numbers(body))

    def test_parent_references_do_not_close(self) -> None:
        self.assertEqual([111], closing_numbers("Closes #111\nRefs #7\n\n## What changed\ntext"))

    def test_inline_or_section_examples_do_not_close(self) -> None:
        self.assertEqual([], closing_numbers("An example Closes #7\n\n## Details\nCloses #12"))

    def test_a_description_saved_in_the_browser_closes_the_same_tasks(self) -> None:
        body = "Closes #470\r\nCloses #471 \r\n\r\nRefs #461\r\n\r\n## What changed\r\nCloses #12\r\n"
        self.assertEqual([470, 471], closing_numbers(body))


if __name__ == "__main__":
    unittest.main()
