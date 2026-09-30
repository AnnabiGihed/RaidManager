"""Keep completed RaidManager issues consistent with their native sub-issues."""

from __future__ import annotations

import argparse
import json
import subprocess
from dataclasses import dataclass


WORK_ITEM_LABELS = frozenset({"type:task", "type:bug", "type:spike"})
PARENT_LABELS = frozenset({"type:epic", "type:story"})
# An epic's parent chain is at most story -> epic, so two levels cover every ancestor.
ANCESTOR_LEVELS = 2


@dataclass(frozen=True)
class Issue:
    number: int
    state: str
    labels: frozenset[str]
    state_reason: str | None = None

    @classmethod
    def from_api(cls, value: dict) -> Issue:
        return cls(
            value["number"],
            value["state"].lower(),
            frozenset(label["name"] for label in value["labels"]),
            (value.get("state_reason") or "").lower() or None,
        )

    @property
    def completed(self) -> bool:
        # Issues closed before GitHub recorded a reason count as completed.
        return self.state == "closed" and self.state_reason in (None, "completed")


def completion_problem(issue: Issue, children: list[Issue]) -> str | None:
    """Returns why a completed epic or story may not stay closed, or None when it may."""
    if not issue.completed:
        return None
    if "type:epic" in issue.labels:
        expected = frozenset({"type:story"})
        name = "story"
    elif "type:story" in issue.labels:
        expected = WORK_ITEM_LABELS
        name = "work item"
    else:
        return None

    unexpected = [child.number for child in children if not child.labels & expected]
    if unexpected:
        return f"Native children must be {name}s: {', '.join(f'#{number}' for number in unexpected)}."
    open_children = [child.number for child in children if child.state != "closed"]
    if open_children:
        return f"Close every child {name} first: {', '.join(f'#{number}' for number in open_children)}."
    if not any(child.completed for child in children):
        return f"At least one native child {name} must be completed before completion."
    return None


def gh(*arguments: str) -> str:
    result = subprocess.run(["gh", *arguments], check=True, capture_output=True, text=True)
    return result.stdout


def api_pages(path: str) -> list[dict]:
    values = []
    page = 1
    while True:
        separator = "&" if "?" in path else "?"
        batch = json.loads(gh("api", f"{path}{separator}per_page=100&page={page}"))
        values.extend(batch)
        if len(batch) < 100:
            return values
        page += 1


def children_for(repository: str, number: int) -> list[Issue]:
    return [Issue.from_api(value) for value in api_pages(f"repos/{repository}/issues/{number}/sub_issues")]


def parent_of(repository: str, number: int) -> Issue | None:
    try:
        return Issue.from_api(json.loads(gh("api", f"repos/{repository}/issues/{number}/parent")))
    except subprocess.CalledProcessError:
        # GitHub answers 404 when the issue has no parent.
        return None


def inspect_issue(repository: str, issue: Issue) -> bool:
    if not issue.completed or not issue.labels & PARENT_LABELS:
        return False
    problem = completion_problem(issue, children_for(repository, issue.number))
    if problem is None:
        return False
    gh("issue", "reopen", str(issue.number), "--repo", repository, "--comment", f"Completion rule: {problem}")
    print(f"Reopened #{issue.number}: {problem}")
    return True


def inspect_with_ancestors(repository: str, issue: Issue) -> None:
    """Checks an issue, then its parent and grandparent, whose completion may depend on it."""
    inspect_issue(repository, issue)
    current = issue
    for _ in range(ANCESTOR_LEVELS):
        parent = parent_of(repository, current.number)
        if parent is None:
            return
        inspect_issue(repository, parent)
        current = parent


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--repository", required=True)
    parser.add_argument("--issue", type=int)
    args = parser.parse_args()
    if args.issue:
        issue = Issue.from_api(json.loads(gh("api", f"repos/{args.repository}/issues/{args.issue}")))
        inspect_with_ancestors(args.repository, issue)
        return
    issues = [Issue.from_api(value) for value in api_pages(f"repos/{args.repository}/issues?state=closed")]
    for issue in issues:
        inspect_issue(args.repository, issue)


if __name__ == "__main__":
    main()
