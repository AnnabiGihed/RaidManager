"""Tests for the task each Dependabot pull request gets."""

from __future__ import annotations

import os
import sys
import unittest
import unittest.mock
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
import dependency_task  # noqa: E402
from closing_work_items import closing_numbers  # noqa: E402
from dependency_task import (  # noqa: E402
    BOT, MARKER, UPDATES, PullRequest, find_task, pull_request_body, task_body, task_title,
)
from validate_pr import validate  # noqa: E402
from work_contracts import TASK, missing_sections, unknown_sections  # noqa: E402

UPDATE = PullRequest(470, "ci: bump lycheeverse/lychee-action from 2.9.0 to 2.10.0", "Bumps lychee-action.", BOT,
                     "https://github.com/AnnabiGihed/RaidManager/pull/470", False)


class FakeGitHub:
    """Answers the REST calls the script makes, and records the ones that change something."""

    def __init__(self, pull: dict, sub_issues: list[dict], issues: dict[int, dict] | None = None) -> None:
        self.pull = pull
        self.sub_issues = sub_issues
        self.issues = issues or {}
        self.changes: list[tuple[str, str, dict]] = []

    def __call__(self, method: str, path: str, payload: dict | None = None) -> dict | list:
        if method == "GET":
            return self.read(path)
        self.changes.append((method, path, payload or {}))
        return {"number": 480, "id": 9480} if path == "issues" else {}

    def read(self, path: str) -> dict | list:
        if path.startswith("pulls/"):
            return self.pull
        if path.startswith(f"issues/{UPDATES}/sub_issues"):
            return self.sub_issues
        if path == f"issues/{UPDATES}":
            return {"number": UPDATES, "milestone": {"number": 4}}
        return self.issues[int(path.split("/")[1])]


def pull(author: str = BOT, body: str = "Bumps lychee-action.", merged: bool = False) -> dict:
    return {"title": UPDATE.title, "body": body, "user": {"login": author}, "html_url": UPDATE.url, "merged": merged}


def run(fake: FakeGitHub, action: str) -> None:
    environment = {"PR_NUMBER": "470", "PR_ACTION": action}
    with unittest.mock.patch.object(dependency_task, "api", fake), \
            unittest.mock.patch.dict(os.environ, environment), unittest.mock.patch("builtins.print"):
        dependency_task.main()


class ContentTests(unittest.TestCase):
    def test_the_task_meets_the_task_contract(self) -> None:
        body = task_body(UPDATE)
        self.assertEqual(missing_sections(TASK, body), [])
        self.assertEqual(unknown_sections(TASK, body), [])
        self.assertIn(f"### Parent\n#{UPDATES}\n", body)

    def test_the_task_is_named_after_the_update(self) -> None:
        self.assertEqual(task_title(UPDATE), "Task: ci: bump lycheeverse/lychee-action from 2.9.0 to 2.10.0 (#470)")

    def test_the_description_closes_the_task_and_passes_once_the_owner_ticks_it(self) -> None:
        body = pull_request_body(UPDATE, 480)
        self.assertEqual(closing_numbers(body), [480])
        unticked = validate(body)
        self.assertEqual(len(unticked), 5)
        self.assertTrue(all(error.startswith("Author self-review item is not checked") for error in unticked))
        ticked = body.replace("- [ ] I ", "- [x] I ")
        self.assertEqual(validate(ticked), [])

    def test_the_description_fits_tags_and_pins(self) -> None:
        body = pull_request_body(UPDATE, 480)
        self.assertIn("changes the version of the action", body)
        self.assertIn("a tag such as `@v4` or a commit id", body)
        self.assertNotIn("pinned commit id", body)
        self.assertNotIn("pinned versions", task_body(UPDATE))


class FindTaskTests(unittest.TestCase):
    def test_only_an_open_task_of_the_same_pull_request_is_found(self) -> None:
        mine = {"number": 480, "state": "open", "body": MARKER.format(number=470)}
        closed = {"number": 479, "state": "closed", "body": MARKER.format(number=470)}
        other = {"number": 481, "state": "open", "body": MARKER.format(number=4700)}
        self.assertEqual(find_task([closed, other, mine], 470), mine)
        self.assertIsNone(find_task([closed, other], 470))


class RunTests(unittest.TestCase):
    def test_an_opened_update_gets_a_linked_task_and_a_description(self) -> None:
        fake = FakeGitHub(pull(), [])
        run(fake, "opened")
        created, linked, described = fake.changes
        self.assertEqual(created[:2], ("POST", "issues"))
        self.assertEqual((created[2]["labels"], created[2]["assignees"], created[2]["milestone"]),
                         (["type:task"], ["AnnabiGihed"], 4))
        self.assertEqual(linked, ("POST", f"issues/{UPDATES}/sub_issues", {"sub_issue_id": 9480}))
        self.assertEqual(described[:2], ("PATCH", "pulls/470"))
        self.assertEqual(closing_numbers(described[2]["body"]), [480])

    def test_a_rerun_reuses_the_task(self) -> None:
        fake = FakeGitHub(pull(), [{"number": 480, "state": "open", "body": MARKER.format(number=470)}])
        run(fake, "opened")
        self.assertEqual([change[:2] for change in fake.changes], [("PATCH", "pulls/470")])

    def test_a_described_update_is_left_alone(self) -> None:
        fake = FakeGitHub(pull(body=pull_request_body(UPDATE, 480)), [])
        run(fake, "opened")
        self.assertEqual(fake.changes, [])

    def test_a_person_s_pull_request_is_left_alone(self) -> None:
        fake = FakeGitHub(pull(author="AnnabiGihed"), [])
        run(fake, "opened")
        self.assertEqual(fake.changes, [])

    def test_an_update_closed_without_merging_cancels_its_task(self) -> None:
        fake = FakeGitHub(pull(), [{"number": 480, "state": "open", "body": MARKER.format(number=470)}])
        run(fake, "closed")
        commented, closed = fake.changes
        self.assertEqual(commented[:2], ("POST", "issues/480/comments"))
        self.assertEqual(closed, ("PATCH", "issues/480", {"state": "closed", "state_reason": "not_planned"}))

    def test_a_merged_update_leaves_its_task_to_the_review_workflow(self) -> None:
        fake = FakeGitHub(pull(merged=True), [{"number": 480, "state": "open", "body": MARKER.format(number=470)}])
        run(fake, "closed")
        self.assertEqual(fake.changes, [])

    def test_a_reopened_update_reopens_its_canceled_task(self) -> None:
        fake = FakeGitHub(pull(body=pull_request_body(UPDATE, 480)), [],
                          {480: {"number": 480, "state": "closed", "state_reason": "not_planned"}})
        run(fake, "reopened")
        self.assertEqual([change[:2] for change in fake.changes], [("POST", "issues/480/comments"),
                                                                 ("PATCH", "issues/480")])


if __name__ == "__main__":
    unittest.main()
