"""Keep completed RaidManager issues consistent with their native sub-issues."""

from __future__ import annotations

import argparse
import json
import subprocess
from dataclasses import dataclass


WORK_ITEM_LABELS = {"type:task", "type:bug", "type:spike"}


@dataclass(frozen=True)
class Issue:
    number: int
    state: str
    labels: frozenset[str]

    @classmethod
    def from_api(cls, value: dict) -> Issue:
        return cls(value["number"], value["state"].lower(), frozenset(label["name"] for label in value["labels"]))


def completion_problem(issue: Issue, children: list[Issue]) -> str | None:
    if "type:epic" in issue.labels:
        expected = "type:story"
        name = "story"
    elif "type:story" in issue.labels:
        expected = WORK_ITEM_LABELS
        name = "work item"
    else:
        return None

    relevant = [child for child in children if expected in child.labels] if isinstance(expected, str) else [
        child for child in children if child.labels & expected
    ]
    if not relevant:
        return f"At least one native child {name} is required before completion."
    unexpected = [child.number for child in children if child not in relevant]
    if unexpected:
        return f"Native children must be {name}s: {', '.join(f'#{number}' for number in unexpected)}."
    open_children = [child.number for child in children if child.state != "closed"]
    if open_children:
        return f"Close every child {name} first: {', '.join(f'#{number}' for number in open_children)}."
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


def inspect_issue(repository: str, issue: Issue) -> bool:
    if issue.state != "closed" or not issue.labels & {"type:epic", "type:story"}:
        return False
    problem = completion_problem(issue, children_for(repository, issue.number))
    if problem is None:
        return False
    gh("issue", "reopen", str(issue.number), "--repo", repository, "--comment", f"Completion rule: {problem}")
    print(f"Reopened #{issue.number}: {problem}")
    return True


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--repository", required=True)
    parser.add_argument("--issue", type=int)
    args = parser.parse_args()
    if args.issue:
        issue = Issue.from_api(json.loads(gh("api", f"repos/{args.repository}/issues/{args.issue}")))
        inspect_issue(args.repository, issue)
        return
    issues = [Issue.from_api(value) for value in api_pages(f"repos/{args.repository}/issues?state=closed")]
    for issue in issues:
        inspect_issue(args.repository, issue)


if __name__ == "__main__":
    main()
