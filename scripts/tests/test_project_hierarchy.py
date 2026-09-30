"""Tests for native issue hierarchy completion rules."""

from __future__ import annotations

import sys
import unittest
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from project_hierarchy import Issue, completion_problem, project_status_problem  # noqa: E402


def issue(number: int, state: str, label: str) -> Issue:
    return Issue(number, state, frozenset({label}))


class CompletionRulesTests(unittest.TestCase):
    def test_epic_requires_a_story(self) -> None:
        self.assertIn("At least one", completion_problem(issue(7, "closed", "type:epic"), []))

    def test_epic_rejects_open_story(self) -> None:
        problem = completion_problem(issue(7, "closed", "type:epic"), [issue(13, "open", "type:story")])
        self.assertIn("#13", problem)

    def test_story_requires_a_work_item(self) -> None:
        self.assertIn("At least one", completion_problem(issue(13, "closed", "type:story"), []))

    def test_story_rejects_any_open_work_item(self) -> None:
        children = [issue(57, "closed", "type:task"), issue(58, "open", "type:bug")]
        self.assertIn("#58", completion_problem(issue(13, "closed", "type:story"), children))

    def test_story_accepts_closed_work_items(self) -> None:
        children = [issue(57, "closed", "type:task"), issue(58, "closed", "type:spike")]
        self.assertIsNone(completion_problem(issue(13, "closed", "type:story"), children))

    def test_unexpected_child_type_fails(self) -> None:
        children = [issue(57, "closed", "type:task"), issue(58, "closed", "type:story")]
        self.assertIn("#58", completion_problem(issue(13, "closed", "type:story"), children))

    def test_project_requires_done_children(self) -> None:
        children = [issue(13, "closed", "type:story")]
        self.assertIn("#13", project_status_problem(issue(7, "closed", "type:epic"), children, {13: "In Progress"}))
        self.assertIsNone(project_status_problem(issue(7, "closed", "type:epic"), children, {13: "Done"}))


if __name__ == "__main__":
    unittest.main()
