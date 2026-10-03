"""Regression tests for recording a deployment on the items and releases it delivered."""

from __future__ import annotations

import sys
import unittest
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from record_deployment import (  # noqa: E402
    MARKER_END, MARKER_START, Issue, Milestone, delivered_items, main, milestone_description, parse_issue,
    plan_deployment,
)

RUN = "https://github.com/o/r/actions/runs/1"


def done(number: int, *, commits=(), children=(), labels=(), milestone=None) -> Issue:
    return Issue(number, completed=True, closed=True, labels=frozenset(labels), merge_commits=tuple(commits),
                 children=tuple(children), milestone=milestone)


def canceled(number: int, **kwargs) -> Issue:
    issue = done(number, **kwargs)
    return Issue(issue.number, False, True, issue.labels, issue.merge_commits, issue.children, issue.milestone)


def open_issue(number: int, **kwargs) -> Issue:
    issue = done(number, **kwargs)
    return Issue(issue.number, False, False, issue.labels, issue.merge_commits, issue.children, issue.milestone)


def by_number(*issues: Issue) -> dict[int, Issue]:
    return {issue.number: issue for issue in issues}


def plan(issues, history, *, succeeded=True, milestones=(), environment="dev"):
    return plan_deployment(by_number(*issues), list(milestones), set(history), environment, succeeded, RUN,
                           "abcdef1234", "2026-10-03")


class DeliveredItemsTests(unittest.TestCase):
    def test_an_item_is_delivered_when_its_merge_commit_is_deployed(self) -> None:
        issues = by_number(done(1, commits=["a"]), done(2, commits=["b"]))
        self.assertEqual({1}, delivered_items(issues, {"a"}))

    def test_every_closing_pull_request_must_be_deployed(self) -> None:
        self.assertEqual(set(), delivered_items(by_number(done(1, commits=["a", "b"])), {"a"}))

    def test_an_item_closed_without_a_pull_request_counts_once_closed(self) -> None:
        self.assertEqual({1}, delivered_items(by_number(done(1)), set()))

    def test_open_and_canceled_items_are_never_delivered(self) -> None:
        issues = by_number(open_issue(1, commits=["a"]), canceled(2, commits=["a"]))
        self.assertEqual(set(), delivered_items(issues, {"a"}))

    def test_a_parent_needs_all_completed_children_delivered(self) -> None:
        issues = by_number(done(10, children=[1, 2, 3]), done(1, commits=["a"]), done(2, commits=["b"]),
                           canceled(3))
        self.assertEqual({1}, delivered_items(issues, {"a"}) & {1, 10})
        self.assertEqual({1, 2, 10}, delivered_items(issues, {"a", "b"}))

    def test_a_parent_with_an_open_or_unknown_child_is_not_delivered(self) -> None:
        open_child = by_number(done(10, children=[1, 2]), done(1), open_issue(2))
        other_repository = by_number(done(10, children=[1, None]), done(1))
        self.assertNotIn(10, delivered_items(open_child, set()))
        self.assertNotIn(10, delivered_items(other_repository, set()))

    def test_a_parent_whose_children_are_all_canceled_is_not_delivered(self) -> None:
        self.assertEqual(set(), delivered_items(by_number(done(10, children=[1]), canceled(1)), set()))

    def test_a_cycle_does_not_loop(self) -> None:
        self.assertEqual(set(), delivered_items(by_number(done(1, children=[2]), done(2, children=[1])), set()))


class PlanTests(unittest.TestCase):
    def test_the_first_run_backfills_without_comments(self) -> None:
        result = plan([done(1, commits=["a"]), done(2, commits=["b"])], {"a", "b"})
        self.assertEqual({1: ["deployed:dev"], 2: ["deployed:dev"]}, result.add)
        self.assertEqual({}, result.comments)

    def test_later_runs_label_and_comment_only_new_items(self) -> None:
        result = plan([done(1, commits=["a"], labels=["deployed:dev"]), done(2, commits=["b"])], {"a", "b"})
        self.assertEqual({2: ["deployed:dev"]}, result.add)
        self.assertIn("Deployed to **dev**", result.comments[2])
        self.assertIn(RUN, result.comments[2])

    def test_a_failure_marks_undeployed_items_once(self) -> None:
        issues = [done(1, commits=["a"], labels=["deployed:dev"]), done(2, commits=["b"]),
                  done(3, commits=["c"], labels=["deploy-failed:dev"]), done(10, children=[2])]
        result = plan(issues, {"a", "b", "c"}, succeeded=False)
        self.assertEqual({2: ["deploy-failed:dev"]}, result.add)
        self.assertIn("failed", result.comments[2])
        self.assertEqual({}, result.milestones)

    def test_a_success_clears_the_failure_label(self) -> None:
        result = plan([done(1, commits=["a"], labels=["deployed:dev"]),
                       done(2, commits=["b"], labels=["deploy-failed:dev"])], {"a", "b"})
        self.assertEqual({2: ["deploy-failed:dev"]}, result.remove)
        self.assertEqual({2: ["deployed:dev"]}, result.add)

    def test_a_reopened_item_loses_its_label(self) -> None:
        result = plan([open_issue(1, labels=["deployed:dev"]), done(2, labels=["deployed:dev"])], set())
        self.assertEqual({1: ["deployed:dev"]}, result.remove)

    def test_environments_are_independent(self) -> None:
        result = plan([done(1, commits=["a"], labels=["deployed:dev"])], {"a"}, environment="production")
        self.assertEqual({1: ["deployed:production"]}, result.add)

    def test_a_release_is_noted_when_all_its_items_are_deployed(self) -> None:
        milestones = [Milestone(1, "v0.1", "Record: docs/planning/releases/v0.1.md"), Milestone(2, "v0.2", "")]
        issues = [done(1, commits=["a"], milestone=1), canceled(2, milestone=1), done(3, commits=["b"], milestone=2),
                  open_issue(4, milestone=2)]
        result = plan(issues, {"a", "b"}, milestones=milestones)
        self.assertEqual([1], list(result.milestones))
        self.assertTrue(result.milestones[1].startswith("Record: docs/planning/releases/v0.1.md\n\n" + MARKER_START))
        self.assertIn("- Deployed to dev: 2026-10-03, [this run](" + RUN + "), commit `abcdef1`", result.milestones[1])


class MilestoneDescriptionTests(unittest.TestCase):
    LINE = "- Deployed to test: 2026-10-05, run"

    def test_lines_keep_the_environment_order_and_the_first_date(self) -> None:
        start = f"Intro\n\n{MARKER_START}\n- Deployed to production: 2026-10-09, run\n" \
                f"- Deployed to dev: 2026-10-01, run\n{MARKER_END}"
        updated = milestone_description(start, "test", True, self.LINE)
        self.assertEqual(f"Intro\n\n{MARKER_START}\n- Deployed to dev: 2026-10-01, run\n{self.LINE}\n"
                         f"- Deployed to production: 2026-10-09, run\n{MARKER_END}", updated)
        self.assertEqual(updated, milestone_description(updated, "test", True, "- Deployed to test: later"))

    def test_a_release_no_longer_deployed_loses_its_line_and_empty_block(self) -> None:
        start = f"Intro\n\n{MARKER_START}\n{self.LINE}\n{MARKER_END}"
        self.assertEqual("Intro", milestone_description(start, "test", False, self.LINE))
        self.assertEqual("Intro", milestone_description("Intro", "test", False, self.LINE))


class ParseIssueTests(unittest.TestCase):
    def test_reads_state_children_and_merged_pull_requests(self) -> None:
        node = {
            "number": 7, "state": "CLOSED", "stateReason": "COMPLETED",
            "labels": {"nodes": [{"name": "type:task"}]}, "milestone": {"number": 4},
            "subIssues": {"nodes": [{"number": 8, "repository": {"nameWithOwner": "o/r"}},
                                    {"number": 9, "repository": {"nameWithOwner": "o/other"}}]},
            "closedByPullRequestsReferences": {"nodes": [{"merged": True, "mergeCommit": {"oid": "a"}},
                                                         {"merged": False, "mergeCommit": None}]},
        }
        self.assertEqual(Issue(7, True, True, frozenset({"type:task"}), ("a",), (8, None), 4), parse_issue(node, "o/r"))


class ArgumentTests(unittest.TestCase):
    VALID = ["--environment", "dev", "--outcome", "success", "--commit", "a" * 40, "--run-url", RUN,
             "--date", "2026-10-03"]

    def test_options_cannot_pass_as_values(self) -> None:
        for name, value in (("--commit", "--output=x"), ("--commit", "main"), ("--commit", "abcdef1"),
                            ("--environment", "staging"),
                            ("--run-url", "http://x"), ("--date", "today")):
            arguments = list(self.VALID)
            arguments[arguments.index(name) + 1] = value
            with self.subTest(name=name, value=value), self.assertRaises(SystemExit):
                main(arguments)


if __name__ == "__main__":
    unittest.main()
