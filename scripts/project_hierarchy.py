"""Keep RaidManager's work items in one Epic -> Feature -> Story/Improvement/Bug -> Task/Spike hierarchy (ADR-0016).

Three rules, all checked with the built-in GITHUB_TOKEN:

- Parent: every item except an epic has a parent of the level above; an epic has none. A violation adds the
  needs-parent label and one explanatory comment; fixing the item removes the label.
- Completion: an epic, feature, story, improvement or bug closed as completed is reopened unless at least one child
  of the level below is completed and every child is closed.
- Pull requests: each "Closes #N" names a task or spike whose chain reaches an epic.
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
from typing import Callable


NEEDS_PARENT = "needs-parent"
# A new issue usually gets its parent a moment after it is created, so the parent rule waits before flagging it.
GRACE = timedelta(minutes=10)
WORK_ITEMS = frozenset({"type:task", "type:spike"})
BACKLOG_ITEMS = frozenset({"type:story", "type:improvement", "type:bug"})
# Allowed parent types and child types for each type label.
PARENTS: dict[str, frozenset[str]] = {
    "type:epic": frozenset(),
    "type:feature": frozenset({"type:epic"}),
    **{kind: frozenset({"type:feature"}) for kind in BACKLOG_ITEMS},
    **{kind: BACKLOG_ITEMS for kind in WORK_ITEMS},
}
CHILDREN: dict[str, frozenset[str]] = {
    "type:epic": frozenset({"type:feature"}),
    "type:feature": BACKLOG_ITEMS,
    **{kind: WORK_ITEMS for kind in BACKLOG_ITEMS},
}
NAMES = {
    "type:epic": "epic", "type:feature": "feature", "type:story": "story", "type:improvement": "improvement",
    "type:bug": "bug", "type:task": "task", "type:spike": "spike",
}
# Leaves first, so a parent is judged after the children the same run reopened.
DEPTH = {"type:task": 0, "type:spike": 0, "type:story": 1, "type:improvement": 1, "type:bug": 1,
         "type:feature": 2, "type:epic": 3}
ANCESTOR_LEVELS = 3
CLOSING_LINE = re.compile(r"^Closes #(\d+)[ \t]*$", re.IGNORECASE)
FIELDS = "number state stateReason createdAt labels(first: 20) { nodes { name } }"
NODE = f"{FIELDS} parent {{ {FIELDS} }} subIssues(first: 100) {{ nodes {{ {FIELDS} }} }}"


@dataclass(frozen=True)
class Issue:
    number: int
    state: str
    labels: frozenset[str]
    state_reason: str | None = None
    created_at: datetime | None = None

    @classmethod
    def from_api(cls, value: dict) -> Issue:
        """Reads an issue from the REST (lists of label objects) or GraphQL (labels.nodes) shape."""
        labels = value["labels"]
        names = labels["nodes"] if isinstance(labels, dict) else labels
        reason = value.get("state_reason") or value.get("stateReason") or ""
        created = value.get("created_at") or value.get("createdAt")
        return cls(value["number"], value["state"].lower(), frozenset(label["name"] for label in names),
                   reason.lower() or None,
                   datetime.fromisoformat(created.replace("Z", "+00:00")) if created else None)

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
    if kind is None or issue.abandoned:
        return None
    allowed = PARENTS[kind]
    if not allowed:
        return None if parent is None else f"An epic has no parent: remove it from #{parent.number}."
    if parent is None:
        return f"Add this {NAMES[kind]} as a sub-issue of {a(names(allowed))}."
    if parent.kind not in allowed:
        found = NAMES.get(parent.kind or "", "item without one type label")
        return f"Its parent #{parent.number} is {a(found)}; {a(NAMES[kind])} belongs under {a(names(allowed))}."
    return None


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
        return f"#{number} is {a(found)}. A pull request closes a task or spike only; use Refs for other items."
    current, expected = node, [BACKLOG_ITEMS, frozenset({"type:feature"}), frozenset({"type:epic"})]
    for level in expected:
        if current.parent is None or current.parent.kind not in level:
            where = f"#{current.issue.number}"
            return (f"{where} needs a parent {names(level)}, so that #{number} reaches an epic through a story, "
                    "improvement or bug and a feature.")
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

    def __init__(self, repository: str, now: datetime | None = None) -> None:
        self.repository = repository
        self.now = now or datetime.now(timezone.utc)
        self.reopened: set[int] = set()

    def current(self, issue: Issue) -> Issue:
        return Issue(issue.number, "open", issue.labels, None, issue.created_at) if issue.number in self.reopened else issue

    def check_parent(self, node: Node) -> None:
        issue = node.issue
        problem = parent_problem(issue, node.parent)
        if problem and issue.created_at and self.now - issue.created_at < GRACE:
            return
        if problem and NEEDS_PARENT not in issue.labels:
            gh("issue", "edit", str(issue.number), "--repo", self.repository, "--add-label", NEEDS_PARENT)
            gh("issue", "comment", str(issue.number), "--repo", self.repository, "--body",
               f"Hierarchy rule: {problem} The `{NEEDS_PARENT}` label goes away once it is fixed (ADR-0016).")
            print(f"Flagged #{issue.number}: {problem}")
        elif not problem and NEEDS_PARENT in issue.labels:
            gh("issue", "edit", str(issue.number), "--repo", self.repository, "--remove-label", NEEDS_PARENT)
            print(f"Cleared #{issue.number}")

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
        self.check_completion(node)


def check_pull_request(repository: str, body: str) -> int:
    problems = [problem for number in closing_numbers(body)
                if (problem := chain_problem(number, lambda n: fetch_node(repository, n)))]
    for problem in problems:
        print(f"ERROR: {problem}")
    if not problems:
        print("Every closed work item sits in the Epic, Feature, Story and Task hierarchy.")
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
        number: int | None = args.issue
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
