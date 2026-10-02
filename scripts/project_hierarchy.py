"""Keep RaidManager's work items in one Epic -> Feature -> Story/Improvement/Bug/Spike -> Task hierarchy (ADR-0025).

Four rules, all checked with the built-in GITHUB_TOKEN:

- Parent: every item has exactly one type label and, except an epic, a parent of the level above on the same
  milestone; an epic has none.
  Nothing is standalone. A violation adds the needs-parent label and one explanatory comment; fixing the item removes
  the label.
- Completion: an epic, feature, story, improvement, bug or spike closed as completed is reopened unless at least one
  child of the level below is completed and every child is closed.
- Pull requests: each "Closes #N" names a task whose chain reaches an epic.
- Mockups (ADR-0017): an item labelled `ui`, or whose issue form says it changes a user interface, links or shows its
  mockup; otherwise it gets the needs-mockup label and one comment.
"""

from __future__ import annotations

import argparse
import json
import os
import re
import subprocess
import sys
from dataclasses import dataclass, field
from datetime import datetime, timedelta, timezone
from pathlib import Path
from typing import Callable

from ui_mockups import NEEDS_MOCKUP, UI_LABEL, mockup_problem, ui_requested


NEEDS_PARENT = "needs-parent"
# A new issue usually gets its parent a moment after it is created, so the parent rule waits before flagging it.
GRACE = timedelta(minutes=10)
EPIC, FEATURE, STORY, IMPROVEMENT, BUG, SPIKE, TASK = (
    "type:epic", "type:feature", "type:story", "type:improvement", "type:bug", "type:spike", "type:task")
# A spike is a peer of stories, improvements and bugs under a feature, and has tasks like them (ADR-0025).
WORK_ITEMS = frozenset({TASK})
BACKLOG_ITEMS = frozenset({STORY, IMPROVEMENT, BUG, SPIKE})
# Allowed parent types and child types for each type label.
PARENTS: dict[str, frozenset[str]] = {
    EPIC: frozenset(),
    FEATURE: frozenset({EPIC}),
    **dict.fromkeys(BACKLOG_ITEMS, frozenset({FEATURE})),
    **dict.fromkeys(WORK_ITEMS, BACKLOG_ITEMS),
}
CHILDREN: dict[str, frozenset[str]] = {
    EPIC: frozenset({FEATURE}),
    FEATURE: BACKLOG_ITEMS,
    **dict.fromkeys(BACKLOG_ITEMS, WORK_ITEMS),
}
# Type names in hierarchy order.
NAMES = {kind: kind.removeprefix("type:") for kind in (EPIC, FEATURE, STORY, IMPROVEMENT, BUG, SPIKE, TASK)}
# Leaves first, so a parent is judged after the children the same run reopened.
DEPTH = {TASK: 0, **dict.fromkeys(BACKLOG_ITEMS, 1), FEATURE: 2, EPIC: 3}
ANCESTOR_LEVELS = 3
CLOSING_LINE = re.compile(r"^Closes #(\d+)[ \t]*$", re.IGNORECASE)
FIELDS = "number state stateReason createdAt milestone { title } labels(first: 20) { nodes { name } }"
NODE = f"{FIELDS} body parent {{ {FIELDS} }} subIssues(first: 100) {{ nodes {{ {FIELDS} }} }}"


@dataclass(frozen=True)
class Issue:
    number: int
    state: str
    labels: frozenset[str]
    state_reason: str | None = None
    created_at: datetime | None = None
    body: str = ""
    milestone: str | None = None

    @classmethod
    def from_api(cls, value: dict) -> Issue:
        """Reads an issue from the REST (lists of label objects) or GraphQL (labels.nodes) shape."""
        labels = value["labels"]
        names = labels["nodes"] if isinstance(labels, dict) else labels
        reason = value.get("state_reason") or value.get("stateReason") or ""
        created = value.get("created_at") or value.get("createdAt")
        milestone = value.get("milestone") or {}
        return cls(value["number"], value["state"].lower(), frozenset(label["name"] for label in names),
                   reason.lower() or None,
                   datetime.fromisoformat(created.replace("Z", "+00:00")) if created else None,
                   value.get("body") or "", milestone.get("title"))

    @property
    def kinds(self) -> list[str]:
        return sorted(label for label in self.labels if label in PARENTS)

    @property
    def kind(self) -> str | None:
        return self.kinds[0] if len(self.kinds) == 1 else None

    @property
    def completed(self) -> bool:
        # Issues closed before GitHub recorded a reason count as completed.
        return self.state == "closed" and self.state_reason in (None, "completed")

    @property
    def abandoned(self) -> bool:
        return self.state == "closed" and not self.completed


@dataclass
class Node:
    """An issue with its parent and children, as one GraphQL query returns them."""

    issue: Issue
    parent: Issue | None = None
    children: list[Issue] = field(default_factory=list)

    @classmethod
    def from_api(cls, value: dict) -> Node:
        parent = Issue.from_api(value["parent"]) if value.get("parent") else None
        children = [Issue.from_api(child) for child in value["subIssues"]["nodes"]]
        return cls(Issue.from_api(value), parent, children)


ORDER = list(NAMES)


def names(kinds: frozenset[str]) -> str:
    """Lists type names in hierarchy order: "story, improvement or bug"."""
    ordered = [NAMES[kind] for kind in sorted(kinds, key=ORDER.index)]
    return ordered[0] if len(ordered) == 1 else ", ".join(ordered[:-1]) + " or " + ordered[-1]


def a(text: str) -> str:
    """Prefixes the indefinite article: "an epic", "a feature"."""
    return f"{'an' if text[:1] in 'aeiou' else 'a'} {text}"


def numbers(issues: list[Issue]) -> str:
    return ", ".join(f"#{issue.number}" for issue in issues)


def parent_problem(issue: Issue, parent: Issue | None) -> str | None:
    """Returns why an item is misplaced in the hierarchy, or None when it is placed correctly."""
    if len(issue.kinds) > 1:
        return f"Use exactly one type label, not {', '.join(issue.kinds)}."
    kind = issue.kind
    if issue.abandoned or (kind is None and issue.state != "open"):
        return None
    if kind is None:
        return (f"Give this issue exactly one type label ({', '.join(ORDER)}) and link it to its parent; "
                "nothing is standalone.")
    allowed = PARENTS[kind]
    if not allowed:
        return None if parent is None else f"An epic has no parent: remove it from #{parent.number}."
    if parent is None:
        return f"Add this {NAMES[kind]} as a sub-issue of {a(names(allowed))}."
    if parent.kind not in allowed:
        found = NAMES.get(parent.kind or "", "item without one type label")
        return f"Its parent #{parent.number} is {a(found)}; {a(NAMES[kind])} belongs under {a(names(allowed))}."
    if issue.milestone != parent.milestone:
        # A milestone view filters out a parent on another milestone and shows its children as if standalone.
        return (f"Its milestone is {milestone(issue)} but its parent #{parent.number}'s is {milestone(parent)}; "
                "give both the same milestone, so a milestone view shows it under its parent.")
    return None


def milestone(issue: Issue) -> str:
    return f"`{issue.milestone}`" if issue.milestone else "none"


def completion_problem(issue: Issue, children: list[Issue]) -> str | None:
    """Returns why a completed parent may not stay closed, or None when it may."""
    kind = issue.kind
    if not issue.completed or kind not in CHILDREN:
        return None
    expected = CHILDREN[kind]
    unexpected = [child for child in children if child.kind not in expected]
    if unexpected:
        return f"Children of {a(NAMES[kind])} must be {a(names(expected))}: {numbers(unexpected)}."
    open_children = [child for child in children if child.state != "closed"]
    if open_children:
        return f"Close every child first: {numbers(open_children)}."
    if not any(child.completed for child in children):
        return f"At least one child {names(expected)} must be completed before this {NAMES[kind]} can close."
    return None


def chain_problem(number: int, fetch: Callable[[int], Node]) -> str | None:
    """Returns why a pull request may not close #number, or None when it names a correctly placed work item."""
    node = fetch(number)
    if node.issue.kind not in WORK_ITEMS:
        found = NAMES.get(node.issue.kind or "", "item without one type label")
        return f"#{number} is {a(found)}. A pull request closes a task only; use Refs for other items."
    current, expected = node, [BACKLOG_ITEMS, frozenset({FEATURE}), frozenset({EPIC})]
    for level in expected:
        if current.parent is None or current.parent.kind not in level:
            where = f"#{current.issue.number}"
            return (f"{where} needs a parent {names(level)}, so that #{number} reaches an epic through a story, "
                    "improvement, bug or spike and a feature.")
        current = fetch(current.parent.number)
    if current.parent is not None:
        return f"Epic #{current.issue.number} must not have a parent."
    return None


def closing_numbers(body: str) -> list[int]:
    """Reads the standalone "Closes #N" lines before the first section heading, as the review workflow does."""
    found: list[int] = []
    for line in body.replace("\r\n", "\n").split("\n"):
        if line.startswith("## "):
            break
        match = CLOSING_LINE.match(line.strip())
        if match:
            found.append(int(match.group(1)))
    return found


def gh(*arguments: str) -> str:
    result = subprocess.run(["gh", *arguments], check=True, capture_output=True, text=True, encoding="utf-8")
    return result.stdout


def graphql(query: str, **variables: str | int) -> dict:
    arguments = ["api", "graphql", "-f", f"query={query}"]
    for name, value in variables.items():
        arguments += ["-F" if isinstance(value, int) else "-f", f"{name}={value}"]
    return json.loads(gh(*arguments))["data"]


def fetch_node(repository: str, number: int) -> Node:
    owner, name = repository.split("/")
    query = (f"query($owner: String!, $name: String!, $number: Int!) {{ repository(owner: $owner, name: $name) "
             f"{{ issue(number: $number) {{ {NODE} }} }} }}")
    return Node.from_api(graphql(query, owner=owner, name=name, number=number)["repository"]["issue"])


def fetch_all(repository: str) -> list[Node]:
    owner, name = repository.split("/")
    query = (f"query($owner: String!, $name: String!, $cursor: String) {{ repository(owner: $owner, name: $name) "
             f"{{ issues(first: 50, after: $cursor) {{ pageInfo {{ hasNextPage endCursor }} nodes {{ {NODE} }} }} }} }}")
    nodes: list[Node] = []
    cursor = ""
    while True:
        variables = {"owner": owner, "name": name, **({"cursor": cursor} if cursor else {})}
        page = graphql(query, **variables)["repository"]["issues"]
        nodes += [Node.from_api(value) for value in page["nodes"]]
        if not page["pageInfo"]["hasNextPage"]:
            return nodes
        cursor = page["pageInfo"]["endCursor"]


class Guard:
    """Applies the rules to issues and records what it changed."""

    def __init__(self, repository: str, now: datetime | None = None, root: Path | None = None) -> None:
        self.repository = repository
        self.now = now or datetime.now(timezone.utc)
        # The checkout the workflow runs on (main), where a named mockup must exist to count.
        self.root = root or Path.cwd()
        self.reopened: set[int] = set()

    def current(self, issue: Issue) -> Issue:
        if issue.number not in self.reopened:
            return issue
        return Issue(issue.number, "open", issue.labels, None, issue.created_at, issue.body, issue.milestone)

    def flag(self, issue: Issue, label: str, problem: str | None, rule: str) -> None:
        """Adds the label with one explanatory comment while the problem lasts, and removes it once it is fixed."""
        if problem and issue.created_at and self.now - issue.created_at < GRACE:
            return
        if problem and label not in issue.labels:
            gh("issue", "edit", str(issue.number), "--repo", self.repository, "--add-label", label)
            gh("issue", "comment", str(issue.number), "--repo", self.repository, "--body",
               f"{rule}: {problem} The `{label}` label goes away once it is fixed.")
            print(f"Flagged #{issue.number} {label}: {problem}")
        elif not problem and label in issue.labels:
            gh("issue", "edit", str(issue.number), "--repo", self.repository, "--remove-label", label)
            print(f"Cleared #{issue.number} {label}")

    def check_parent(self, node: Node) -> None:
        self.flag(node.issue, NEEDS_PARENT, parent_problem(node.issue, node.parent), "Hierarchy rule (ADR-0025)")

    def check_mockup(self, node: Node) -> None:
        issue = node.issue
        labels = issue.labels
        if UI_LABEL not in labels and ui_requested(issue.body):
            gh("issue", "edit", str(issue.number), "--repo", self.repository, "--add-label", UI_LABEL)
            labels = labels | {UI_LABEL}
            print(f"Labelled #{issue.number} {UI_LABEL}: its issue form says it changes a user interface")
        problem = None if issue.abandoned else mockup_problem(labels, issue.body, self.root)
        self.flag(issue, NEEDS_MOCKUP, problem, "Mockup rule (ADR-0017)")

    def check_completion(self, node: Node) -> None:
        issue = self.current(node.issue)
        problem = completion_problem(issue, [self.current(child) for child in node.children])
        if problem:
            gh("issue", "reopen", str(issue.number), "--repo", self.repository, "--comment",
               f"Completion rule: {problem}")
            self.reopened.add(issue.number)
            print(f"Reopened #{issue.number}: {problem}")

    def check(self, node: Node) -> None:
        self.check_parent(node)
        self.check_mockup(node)
        self.check_completion(node)


def check_pull_request(repository: str, body: str) -> int:
    problems = [problem for number in closing_numbers(body)
                if (problem := chain_problem(number, lambda n: fetch_node(repository, n)))]
    for problem in problems:
        print(f"ERROR: {problem}")
    if not problems:
        print("Every closed task sits in the Epic, Feature, Story/Improvement/Bug/Spike and Task hierarchy.")
    return 1 if problems else 0


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repository", required=True)
    parser.add_argument("--issue", type=int, help="check one issue and its ancestors")
    parser.add_argument("--pull-request", action="store_true", help="check the work items PR_BODY closes")
    args = parser.parse_args()
    if args.pull_request:
        return check_pull_request(args.repository, os.environ.get("PR_BODY", ""))
    guard = Guard(args.repository)
    if args.issue:
        number: int = args.issue
        for _ in range(ANCESTOR_LEVELS + 1):
            node = fetch_node(args.repository, number)
            guard.check(node)
            if node.parent is None:
                break
            number = node.parent.number
        return 0
    nodes = fetch_all(args.repository)
    for node in sorted(nodes, key=lambda node: DEPTH.get(node.issue.kind or "", -1)):
        guard.check(node)
    return 0


if __name__ == "__main__":
    sys.exit(main())
