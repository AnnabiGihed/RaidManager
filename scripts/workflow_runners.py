"""Check that every workflow job runs on the owner's self-hosted runner and that no fork pull request reaches it.

ADR-0034 moves every GitHub Actions job to the runner labelled self-hosted, linux and pc-personal on the owner's PC.
The repository is public, so each job that a pull_request event can start must skip pull requests from forks
(owner decision on #561). The docs check runs this through validate_docs.py; it reads the workflow files as text,
since the scripts carry no YAML library.
"""

from __future__ import annotations

import re
from pathlib import Path

RUNS_ON = "runs-on: [self-hosted, linux, pc-personal]"
FORK_GUARD = "github.event.pull_request.head.repo.full_name == github.repository"
WORKFLOWS = Path(".github") / "workflows"

TRIGGER = re.compile(r"^ {2}([a-z_]+):")
JOB = re.compile(r"^ {2}([A-Za-z0-9_-]+):\s*$")
JOB_KEY = re.compile(r"^ {4}([a-z-]+):")


def triggers(lines: list[str]) -> set[str]:
    """The events in the workflow's top-level `on:` block."""
    events: set[str] = set()
    inside = False
    for line in lines:
        if line.startswith("on:"):
            inside = True
            continue
        if inside and line and not line.startswith((" ", "#")):
            break
        match = TRIGGER.match(line) if inside else None
        if match:
            events.add(match.group(1))
    return events


def jobs(lines: list[str]) -> dict[str, list[str]]:
    """Each job's name with every line below it, up to the next job."""
    found: dict[str, list[str]] = {}
    inside = False
    current: list[str] | None = None
    for line in lines:
        if line.startswith("jobs:"):
            inside = True
            continue
        if not inside:
            continue
        if line and not line.startswith((" ", "#")):
            break
        match = JOB.match(line)
        if match:
            current = found.setdefault(match.group(1), [])
        elif current is not None:
            current.append(line)
    return found


def job_condition(keys: list[str]) -> str:
    """The job's `if:` text, joined across its continuation lines."""
    text: list[str] = []
    collecting = False
    for line in keys:
        if line.startswith("    if:"):
            collecting = True
            text.append(line[len("    if:"):].strip())
        elif collecting and line.startswith("      "):
            text.append(line.strip())
        elif collecting:
            break
    return " ".join(text)


def workflow_problems(name: str, text: str) -> list[str]:
    """The problems of one workflow file."""
    lines = text.splitlines()
    fork_reachable = "pull_request" in triggers(lines)
    problems: list[str] = []
    for job, keys in jobs(lines).items():
        runs_on = [line.strip() for line in keys if JOB_KEY.match(line) and line.startswith("    runs-on:")]
        if runs_on != [RUNS_ON]:
            problems.append(f"{name}: job {job} must use `{RUNS_ON}` (ADR-0034)")
        if fork_reachable and FORK_GUARD not in job_condition(keys):
            problems.append(f"{name}: job {job} runs on pull_request and must skip forks with `{FORK_GUARD}` (ADR-0034)")
    return problems


def runner_problems(root: Path) -> list[str]:
    """The problems of every workflow under .github/workflows."""
    folder = root / WORKFLOWS
    if not folder.is_dir():
        return []
    problems: list[str] = []
    for path in sorted([*folder.glob("*.yml"), *folder.glob("*.yaml")]):
        problems.extend(workflow_problems(path.name, path.read_text(encoding="utf-8")))
    return problems
