"""Tests for native issue hierarchy completion rules."""

from __future__ import annotations

import sys
import unittest
from pathlib import Path
from unittest import mock

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
import project_hierarchy  # noqa: E402
from project_hierarchy import Issue, completion_problem  # noqa: E402


def issue(number: int, state: str, label: str, reason: str | None = None) -> Issue:
    if reason is None and state == "closed":
        reason = "completed"
    return Issue(number, state, frozenset({label}), reason)


class CompletionRulesTests(unittest.TestCase):
    def test_epic_requires_a_story(self) -> None:
        self.assertIn("At least one", completion_problem(issue(7, "closed", "type:epic"), []))

    def test_epic_rejects_open_story(self) -> None:
        problem = completion_problem(issue(7, "closed", "type:epic"), [issue(13, "open", "type:story")])
        self.assertIn("#13", problem)

    def test_epic_accepts_completed_stories(self) -> None:
        self.assertIsNone(completion_problem(issue(7, "closed", "type:epic"), [issue(13, "closed", "type:story")]))

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

    def test_story_with_only_abandoned_work_items_fails(self) -> None:
        children = [issue(57, "closed", "type:task", "not_planned")]
        self.assertIn("must be completed", completion_problem(issue(13, "closed", "type:story"), children))

    def test_abandoned_work_item_next_to_a_completed_one_passes(self) -> None:
        children = [issue(57, "closed", "type:task"), issue(58, "closed", "type:task", "not_planned")]
        self.assertIsNone(completion_problem(issue(13, "closed", "type:story"), children))

    def test_story_closed_as_not_planned_is_left_closed(self) -> None:
        self.assertIsNone(completion_problem(issue(13, "closed", "type:story", "not_planned"), []))

    def test_epic_closed_as_duplicate_is_left_closed(self) -> None:
        self.assertIsNone(completion_problem(issue(7, "closed", "type:epic", "duplicate"), []))

    def test_closure_without_a_recorded_reason_counts_as_completed(self) -> None:
        self.assertIn("At least one", completion_problem(Issue(13, "closed", frozenset({"type:story"})), []))

    def test_open_story_is_not_checked(self) -> None:
        self.assertIsNone(completion_problem(issue(13, "open", "type:story"), []))

    def test_reads_the_close_reason_from_the_api(self) -> None:
        value = {"number": 13, "state": "CLOSED", "labels": [{"name": "type:story"}], "state_reason": "NOT_PLANNED"}
        self.assertFalse(Issue.from_api(value).completed)


class AncestorCheckTests(unittest.TestCase):
    """A change to a child re-checks the parent and grandparent above it."""

    def test_reopened_task_reopens_its_completed_story_and_epic(self) -> None:
        # Fake GitHub state: task #57 was reopened under completed story #18 and completed epic #8.
        issues = {57: issue(57, "open", "type:task"), 18: issue(18, "closed", "type:story"),
                  8: issue(8, "closed", "type:epic")}
        parents = {57: 18, 18: 8, 8: None}
        children = {18: [57], 8: [18]}
        reopened: list[int] = []

        def fake_gh(*arguments: str) -> str:
            self.assertEqual(arguments[:2], ("issue", "reopen"))
            number = int(arguments[2])
            issues[number] = Issue(number, "open", issues[number].labels)
            reopened.append(number)
            return ""

        def parent_of(_: str, number: int) -> Issue | None:
            return issues[parents[number]] if parents[number] else None

        with (
            mock.patch.object(project_hierarchy, "gh", side_effect=fake_gh),
            mock.patch.object(project_hierarchy, "parent_of", side_effect=parent_of),
            mock.patch.object(project_hierarchy, "children_for",
                              side_effect=lambda _, number: [issues[child] for child in children.get(number, [])]),
        ):
            project_hierarchy.inspect_with_ancestors("owner/repo", issues[57])

        self.assertEqual(reopened, [18, 8])

    def test_issue_without_parent_stops_the_walk(self) -> None:
        with (
            mock.patch.object(project_hierarchy, "parent_of", return_value=None) as parent_of,
            mock.patch.object(project_hierarchy, "inspect_issue", return_value=False) as inspect,
        ):
            project_hierarchy.inspect_with_ancestors("owner/repo", issue(7, "closed", "type:epic"))

        self.assertEqual(parent_of.call_count, 1)
        self.assertEqual(inspect.call_count, 1)


if __name__ == "__main__":
    unittest.main()
