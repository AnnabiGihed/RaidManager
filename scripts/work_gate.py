"""The agent preflight and board report of the Work Management and Delivery Specification.

Author: Gihed Annabi
Date: 2026-10-02
Purpose: check the rules that need the GitHub Project's fields (Sprint, Status, Delivery Stage, Story Points), which
the built-in GITHUB_TOKEN can't read on a user-owned Project. It runs with the owner's local `gh` login, before every
execution and at the start of every agent session (specification sections 8, 17, 18 and 22, ADR-0026).

Usage: work_gate.py preflight <task>        the section 8 gate; exits 1 and names each failed condition
       work_gate.py report [--apply-labels]  board report; exits 1 on any violation; --apply-labels maintains the
                                             scheduling-violation label
"""

from __future__ import annotations

import argparse
import json
import re
import subprocess
import sys
from dataclasses import dataclass, field
from datetime import date, datetime, timedelta, timezone
from pathlib import Path
from zoneinfo import ZoneInfo

from project_hierarchy import closing_numbers
from work_contracts import (BUG, EPIC, FEATURE, IMPROVEMENT, SPIKE, STORY, TASK, missing_sections,
                            task_milestone_problem, unknown_sections)

REPOSITORY = "AnnabiGihed/RaidManager"
PROJECT = "PVT_kwHOAPL9-M4BlGGp"
TIMEZONE = ZoneInfo("Europe/Brussels")
SPRINTS = Path("docs/planning/sprints")
SCHEDULING_VIOLATION = "scheduling-violation"
OUTCOMES = frozenset({STORY, IMPROVEMENT, BUG, SPIKE})
EXECUTABLE = OUTCOMES | {TASK}
TYPES = frozenset({EPIC, FEATURE, STORY, IMPROVEMENT, BUG, SPIKE, TASK})
EXECUTING = frozenset({"In Progress", "In Review"})
STATE_LINE = re.compile(r"^\|\s*State\s*\|\s*([A-Za-z ]+?)\s*\|", re.MULTILINE)


@dataclass(frozen=True)
class Sprint:
    title: str
    start: date
    duration: int

    def window(self) -> tuple[datetime, datetime]:
        """Local midnight boundaries in Europe/Brussels, start inclusive and end exclusive (sections 7, 22)."""
        start = datetime.combine(self.start, datetime.min.time(), TIMEZONE)
        end = datetime.combine(self.start + timedelta(days=self.duration), datetime.min.time(), TIMEZONE)
        return start, end

    def active(self, now: datetime) -> bool:
        start, end = self.window()
        return start <= now < end


@dataclass
class Item:
    number: int
    title: str
    state: str
    reason: str | None
    labels: frozenset[str]
    body: str = ""
    milestone: str | None = None
    assignees: tuple[str, ...] = ()
    parent: int | None = None
    prerequisites: list[tuple[int, str, str | None]] = field(default_factory=list)
    status: str | None = None
    stage: str | None = None
    points: float | None = None
    sprint: Sprint | None = None

    @property
    def kind(self) -> str | None:
        kinds = sorted(self.labels & TYPES)
        return kinds[0] if len(kinds) == 1 else None

    @property
    def completed(self) -> bool:
        return self.state == "closed" and self.reason in (None, "completed")

    @property
    def canceled(self) -> bool:
        return self.state == "closed" and not self.completed


@dataclass(frozen=True)
class Finding:
    rule: str
    passed: bool
    detail: str


def sprint_state(sprint: Sprint, root: Path) -> str | None:
    """Reads the state from the sprint's record, docs/planning/sprints/sprint-NN.md, if it exists."""
    match = re.search(r"(\d+)", sprint.title)
    if not match:
        return None
    path = root / SPRINTS / f"sprint-{int(match.group(1)):02d}.md"
    if not path.is_file():
        return None
    found = STATE_LINE.search(path.read_text(encoding="utf-8"))
    return found.group(1) if found else None


def contract_gaps(item: Item) -> list[str]:
    kind = item.kind
    if kind is None:
        return ["exactly one type label"]
    return missing_sections(kind, item.body) + [f"{name} (Unknown)" for name in unknown_sections(kind, item.body)]


def preflight(task: Item, items: dict[int, Item], now: datetime, root: Path) -> list[Finding]:
    """The seven conditions of the active-sprint gate (section 8) for one task."""
    findings: list[Finding] = []
    parent = items.get(task.parent) if task.parent else None

    def check(rule: str, problem: str | None, fix: str) -> None:
        findings.append(Finding(rule, problem is None, f"{problem} {fix}" if problem else "ok"))

    hierarchy = None
    if task.kind != TASK:
        hierarchy = f"#{task.number} is not a task; only tasks are executed (outcome items run through their tasks)."
    elif parent is None or parent.kind not in OUTCOMES:
        hierarchy = f"#{task.number} has no story, improvement, bug or spike parent on the Project."
    gaps = contract_gaps(task) + (contract_gaps(parent) if parent else [])
    check("1. Hierarchy and contract", hierarchy or (f"Contract gaps: {', '.join(gaps)}." if gaps else None),
          "Fix them with work-classification-and-hierarchy and work-backlog-refinement.")

    sprint_problem = None
    if task.sprint is None:
        sprint_problem = "No sprint is assigned."
    else:
        start, end = task.sprint.window()
        state = sprint_state(task.sprint, root)
        if state is None:
            sprint_problem = f"{task.sprint.title} has no record in {SPRINTS}."
        elif state.lower() == "canceled":
            sprint_problem = f"{task.sprint.title} is canceled."
        elif not task.sprint.active(now):
            sprint_problem = (f"{task.sprint.title} runs from {start:%Y-%m-%d %H:%M} to {end:%Y-%m-%d %H:%M} "
                              f"Europe/Brussels (end exclusive); now is {now.astimezone(TIMEZONE):%Y-%m-%d %H:%M}.")
    check("2. Active sprint", sprint_problem,
          "Ask the owner to select the work into an active sprint; never move dates.")

    same = None
    if parent and (parent.sprint is None or task.sprint is None or parent.sprint.title != task.sprint.title):
        same = (f"#{task.number} is in {task.sprint.title if task.sprint else 'no sprint'} but its parent "
                f"#{parent.number} is in {parent.sprint.title if parent.sprint else 'no sprint'}.")
    check("3. Matching sprint", same, "Select the task and its parent into the same sprint.")

    release = task_milestone_problem(task.milestone, parent.number, parent.milestone) if parent else None
    check("4. Matching release", release, "Align the milestones (specification section 15).")

    missing = [name for name, value in (("assignee", task.assignees), ("Delivery Stage", task.stage)) if not value]
    check("5. Assignee and Delivery Stage", f"Missing: {', '.join(missing)}." if missing else None,
          "Set them on the task.")

    waiting = [f"#{number}" for number, state, reason in task.prerequisites
               if not (state == "closed" and reason in (None, "completed"))]
    check("6. Prerequisites", f"Not completed yet: {', '.join(waiting)}." if waiting else None,
          "Wait for their completion evidence.")

    closed = None
    if task.state == "closed":
        closed = f"#{task.number} is already {'completed' if task.completed else 'canceled'}."
    elif parent and parent.state == "closed":
        closed = f"Its parent #{parent.number} is closed."
    check("7. Open and not canceled", closed, "Reopen it deliberately (work-task-execution-and-completion) first.")
    return findings


def same_sprint(first: Item | None, second: Item) -> bool:
    return bool(first and first.sprint and second.sprint and first.sprint.title == second.sprint.title)


def status_problem(item: Item) -> str | None:
    """Closure reason against Project Status (specification section 13, A4)."""
    if item.completed and item.status != "Done":
        return f"#{item.number} is closed as completed but its Status is {item.status or 'empty'}, not Done."
    if item.canceled and item.status != "Canceled":
        return f"#{item.number} is closed as not planned but its Status is {item.status or 'empty'}, not Canceled."
    if item.state == "open" and item.status in ("Done", "Canceled"):
        return f"#{item.number} is open but its Status is {item.status}."
    return None


def report(items: dict[int, Item], open_pull_requests: list[tuple[int, str]], now: datetime,
           root: Path) -> dict[str, list[tuple[int, str]]]:
    """Every board finding the specification asks validators for (sections 17 and 18), grouped by category."""
    found: dict[str, list[tuple[int, str]]] = {
        "Scheduling violations": [], "Status and closure mismatches": [], "Unestimated selected stories": [],
        "Ready or selected items with contract gaps": [], "Unavailable prerequisites": [],
        "Blocked items without a recorded reason": [], "Open items with contract gaps (allowed in Backlog)": [],
    }
    for item in sorted(items.values(), key=lambda value: value.number):
        kind = item.kind
        parent = items.get(item.parent) if item.parent else None
        problem = status_problem(item)
        if problem:
            found["Status and closure mismatches"].append((item.number, problem))
        if item.state != "open" or kind is None:
            continue
        if kind in EXECUTABLE and item.status in EXECUTING and item.sprint is None:
            found["Scheduling violations"].append(
                (item.number, f"Status {item.status} but no sprint was ever assigned."))
        if kind == TASK and parent and parent.kind in OUTCOMES and item.sprint and \
                (parent.sprint is None or parent.sprint.title != item.sprint.title):
            found["Scheduling violations"].append(
                (item.number, f"In {item.sprint.title}, but its parent #{parent.number} is in "
                              f"{parent.sprint.title if parent.sprint else 'no sprint'}."))
        if kind == STORY and item.sprint and item.points is None:
            found["Unestimated selected stories"].append((item.number, f"Selected into {item.sprint.title}."))
        gaps = contract_gaps(item)
        if gaps:
            target = ("Ready or selected items with contract gaps" if item.status == "Ready" or item.sprint
                      else "Open items with contract gaps (allowed in Backlog)")
            found[target].append((item.number, ", ".join(gaps)))
        if item.sprint:
            outside = [f"#{number}" for number, state, reason in item.prerequisites
                       if not (state == "closed" and reason in (None, "completed"))
                       and not same_sprint(items.get(number), item)]
            if outside:
                found["Unavailable prerequisites"].append(
                    (item.number, f"Waits on {', '.join(outside)}, unfinished and outside {item.sprint.title}."))
        if item.status == "Blocked" and "unblock" not in item.body.lower():
            found["Blocked items without a recorded reason"].append(
                (item.number, "Record the reason, the person responsible and the unblock condition."))
    for number, body in open_pull_requests:
        for task_number in closing_numbers(body):
            task = items.get(task_number)
            if task and (task.sprint is None or not task.sprint.active(now)):
                found["Scheduling violations"].append(
                    (task_number, f"Pull request #{number} is open, but the task has no active sprint; it may "
                                  "not change or merge until the task is selected into one (A5)."))
    return found


VIOLATIONS = ("Scheduling violations", "Status and closure mismatches", "Unestimated selected stories",
              "Ready or selected items with contract gaps", "Unavailable prerequisites",
              "Blocked items without a recorded reason")


def gh(*arguments: str) -> str:
    result = subprocess.run(["gh", *arguments], check=True, capture_output=True, text=True, encoding="utf-8")
    return result.stdout


ITEMS_QUERY = """query($cursor: String) { node(id: "%s") { ... on ProjectV2 { items(first: 100, after: $cursor) {
  pageInfo { hasNextPage endCursor }
  nodes {
    status: fieldValueByName(name: "Status") { ... on ProjectV2ItemFieldSingleSelectValue { name } }
    stage: fieldValueByName(name: "Delivery Stage") { ... on ProjectV2ItemFieldSingleSelectValue { name } }
    points: fieldValueByName(name: "Story Points") { ... on ProjectV2ItemFieldNumberValue { number } }
    sprint: fieldValueByName(name: "Sprint") { ... on ProjectV2ItemFieldIterationValue { title startDate duration } }
    content { ... on Issue { number title state stateReason body milestone { title }
      labels(first: 20) { nodes { name } } assignees(first: 10) { nodes { login } } parent { number }
      blockedBy(first: 50) { nodes { number state stateReason } } } }
  } } } } }""" % PROJECT


def item_from_api(node: dict) -> Item | None:
    content = node.get("content") or {}
    if "number" not in content:
        return None
    sprint = node.get("sprint")
    return Item(
        number=content["number"], title=content["title"], state=content["state"].lower(),
        reason=(content.get("stateReason") or "").lower() or None,
        labels=frozenset(label["name"] for label in content["labels"]["nodes"]), body=content.get("body") or "",
        milestone=(content.get("milestone") or {}).get("title"),
        assignees=tuple(person["login"] for person in content["assignees"]["nodes"]),
        parent=(content.get("parent") or {}).get("number"),
        prerequisites=[(other["number"], other["state"].lower(), (other.get("stateReason") or "").lower() or None)
                       for other in content["blockedBy"]["nodes"]],
        status=(node.get("status") or {}).get("name"), stage=(node.get("stage") or {}).get("name"),
        points=(node.get("points") or {}).get("number"),
        sprint=Sprint(sprint["title"], date.fromisoformat(sprint["startDate"]), sprint["duration"]) if sprint else None,
    )


def fetch_items() -> dict[int, Item]:
    items: dict[int, Item] = {}
    cursor = ""
    while True:
        arguments = ["api", "graphql", "-f", f"query={ITEMS_QUERY}"] + (["-f", f"cursor={cursor}"] if cursor else [])
        page = json.loads(gh(*arguments))["data"]["node"]["items"]
        for node in page["nodes"]:
            item = item_from_api(node)
            if item:
                items[item.number] = item
        if not page["pageInfo"]["hasNextPage"]:
            return items
        cursor = page["pageInfo"]["endCursor"]


def fetch_open_pull_requests() -> list[tuple[int, str]]:
    found = json.loads(gh("pr", "list", "--repo", REPOSITORY, "--state", "open", "--json", "number,body",
                          "--limit", "100"))
    return [(value["number"], value["body"] or "") for value in found]


def apply_labels(items: dict[int, Item], violations: set[int]) -> None:
    """Keeps the scheduling-violation label on exactly the items the report flags (section 17)."""
    for item in items.values():
        has = SCHEDULING_VIOLATION in item.labels
        if item.number in violations and not has:
            gh("issue", "edit", str(item.number), "--repo", REPOSITORY, "--add-label", SCHEDULING_VIOLATION)
        elif item.number not in violations and has:
            gh("issue", "edit", str(item.number), "--repo", REPOSITORY, "--remove-label", SCHEDULING_VIOLATION)


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    commands = parser.add_subparsers(dest="command", required=True)
    gate = commands.add_parser("preflight", help="check the active-sprint gate for one task")
    gate.add_argument("task", type=int)
    board = commands.add_parser("report", help="report every board finding")
    board.add_argument("--apply-labels", action="store_true", help="maintain the scheduling-violation label")
    args = parser.parse_args()
    now, root = datetime.now(timezone.utc), Path.cwd()
    items = fetch_items()
    if args.command == "preflight":
        task = items.get(args.task)
        if task is None:
            print(f"FAIL #{args.task} is not on the Project; eligibility is unknown (section 18).")
            return 1
        findings = preflight(task, items, now, root)
        for finding in findings:
            print(f"{'PASS' if finding.passed else 'FAIL'} {finding.rule}: {finding.detail}")
        return 0 if all(finding.passed for finding in findings) else 1
    for item in items.values():
        if item.status == "Blocked":
            # The reason, the person responsible and the unblock condition are usually a comment (section 11).
            item.body += "\n" + gh("issue", "view", str(item.number), "--repo", REPOSITORY, "--comments")
    found = report(items, fetch_open_pull_requests(), now, root)
    for category, entries in found.items():
        print(f"## {category} ({len(entries)})")
        for number, detail in entries:
            print(f"- #{number}: {detail}")
        print()
    if args.apply_labels:
        apply_labels(items, {number for number, _ in found["Scheduling violations"]})
    return 1 if any(found[category] for category in VIOLATIONS) else 0


if __name__ == "__main__":
    sys.exit(main())
