"""Give each Dependabot pull request its own task, so dependency updates follow the work rules (#460).

Dependabot opens a pull request for each new release of a GitHub Action the workflows pin. Every pull request must
close a task in the hierarchy (ADR-0025), so this script, run by the `dependency-task` workflow:

- when Dependabot opens or reopens a pull request, creates a task under the standing improvement for updates, with
  its contract, type label, the improvement's milestone and the owner as assignee, links it as a sub-issue, and
  rewrites the pull request description with `Closes #<task>` and the five required sections. The self-review
  checklist stays unticked: the owner ticks it while reviewing, which also re-runs the `validate` check;
- when Dependabot closes a pull request without merging it (a newer release supersedes it), closes its task as not
  planned with a comment.

The task can't be selected into a sprint here: the built-in token can't write Project fields (specification §22). The
board report lists its open pull request until the agent selects the task, with the owner's standing approval
recorded on #460. Everything goes through `gh` with the workflow's built-in token; a rerun changes nothing.

Usage, with GH_TOKEN, PR_NUMBER and PR_ACTION set by the workflow:
    dependency_task.py
"""

from __future__ import annotations

import json
import os
import subprocess
import sys
from dataclasses import dataclass

from closing_work_items import closing_numbers

REPOSITORY = "AnnabiGihed/RaidManager"
OWNER = "AnnabiGihed"
BOT = "dependabot[bot]"
# The standing improvement that holds every update task (owner decision on #460).
UPDATES = 461
MARKER = "<!-- dependency-pull-request: {number} -->"


@dataclass(frozen=True)
class PullRequest:
    number: int
    title: str
    body: str
    author: str
    url: str
    merged: bool


def task_title(pull_request: PullRequest) -> str:
    """Names the task after the update, such as `Task: ci: bump x from 1 to 2 (#470)`."""
    return f"Task: {pull_request.title} (#{pull_request.number})"


def task_body(pull_request: PullRequest) -> str:
    """The task's contract, with the marker that finds it again for the same pull request."""
    return f"""{MARKER.format(number=pull_request.number)}
### Parent
#{UPDATES}

### User interface
No, nothing users see changes

### Mockup
None: no screen changes.

### Purpose
Review and merge Dependabot's update {pull_request.url}: {pull_request.title}.

### Bounded deliverable
The action versions that pull request changes in `.github/workflows/`.

### Execution scope
Only the files Dependabot changed; any adaptation the new release needs is a separate task.

### Delivery Stage
Development

### Completion conditions
The pull request's checks pass, the owner reviewed it and a peer approved it, and it merged.

### Dependencies
None.

### Verification method
The required checks of the pull request, which run the updated workflows.
"""


def pull_request_body(pull_request: PullRequest, task: int) -> str:
    """The description the `validate` check requires, written for an update the owner reviews as the operator."""
    return f"""Closes #{task}

Refs #{UPDATES}

No visual change: a dependency update of the GitHub Actions workflows.

## What changed

{pull_request.title}: Dependabot changes the version of the action in the workflows this pull request lists,
whether a tag such as `@v4` or a commit id with its version comment. The release notes are linked in its commit
message.

## Why it changed

The workflows' actions stay current without anyone watching for releases (#460). The task #{task} was created for this
update by the `dependency-task` workflow.

## How it was tested

The required checks of this pull request run on the updated workflows.

## What to review carefully

The release notes of the new version: a changed input, output or permission can need an adaptation, which is then a
separate task.

## Migration or deployment notes

None.

### Author self-review

The owner reviews this update as the operator and ticks each item that holds.

- [ ] I reviewed the final diff and mapped the task's acceptance criteria to evidence.
- [ ] I updated documentation, API contracts, diagrams, and ADRs affected by this change.
- [ ] I added a `[Unreleased]` changelog entry for observable changes, or explained why none applies.
- [ ] I checked the documentation for technical accuracy, terminology, examples, and broken links.
- [ ] I confirmed that no credentials or personal information are committed.
- [ ] Required CI checks pass on the latest commit, and an independent reviewer approved the PR.
"""


def issue_number(issue: dict) -> int:
    return int(issue["number"])


def find_task(sub_issues: list[dict], number: int) -> dict | None:
    """Finds the open task created earlier for the same pull request, so a rerun creates nothing twice."""
    marker = MARKER.format(number=number)
    return next((issue for issue in sub_issues if issue.get("state") == "open" and marker in (issue.get("body") or "")),
                None)


def gh(*arguments: str, stdin: str | None = None) -> str:
    return subprocess.run(["gh", *arguments], input=stdin, check=True, capture_output=True, text=True,
                          encoding="utf-8").stdout


def api(method: str, path: str, payload: dict | None = None) -> dict | list:
    """Calls the REST API with a JSON body read from standard input."""
    arguments = ["api", "-X", method, f"repos/{REPOSITORY}/{path}"]
    output = gh(*arguments, "--input", "-", stdin=json.dumps(payload)) if payload is not None else gh(*arguments)
    return json.loads(output) if output.strip() else {}


def api_object(method: str, path: str, payload: dict | None = None) -> dict:
    value = api(method, path, payload)
    if not isinstance(value, dict):
        raise TypeError(f"GitHub returned a list for {path}")
    return value


def fetch_pull_request(number: int) -> PullRequest:
    pull = api_object("GET", f"pulls/{number}")
    return PullRequest(number, pull["title"], pull.get("body") or "", pull["user"]["login"], pull["html_url"],
                       bool(pull.get("merged")))


def sub_issues() -> list[dict]:
    issues = api("GET", f"issues/{UPDATES}/sub_issues?per_page=100")
    return issues if isinstance(issues, list) else []


def open_task(pull_request: PullRequest) -> int:
    """Creates and links the task, or finds the one created earlier, then writes the pull request description."""
    existing = find_task(sub_issues(), pull_request.number)
    if existing:
        task = issue_number(existing)
    else:
        milestone = api_object("GET", f"issues/{UPDATES}").get("milestone")
        created = api_object("POST", "issues", {"title": task_title(pull_request), "body": task_body(pull_request),
                                                "labels": ["type:task"], "assignees": [OWNER],
                                                **({"milestone": issue_number(milestone)} if milestone else {})})
        task = issue_number(created)
        api("POST", f"issues/{UPDATES}/sub_issues", {"sub_issue_id": int(created["id"])})
    api("PATCH", f"pulls/{pull_request.number}", {"body": pull_request_body(pull_request, task)})
    return task


def cancel_task(pull_request: PullRequest) -> int | None:
    """Closes the task of a pull request Dependabot closed without merging, such as one a newer release replaced."""
    existing = find_task(sub_issues(), pull_request.number)
    if not existing:
        return None
    task = issue_number(existing)
    api("POST", f"issues/{task}/comments", {
        "body": f"Canceled: Dependabot closed {pull_request.url} without merging it, usually because a newer release "
                f"replaced it; that release gets its own task. Nothing of this task was delivered, and #{UPDATES} "
                "keeps its scope. The agent sets the Project Status to Canceled (specification §13, A3)."})
    api("PATCH", f"issues/{task}", {"state": "closed", "state_reason": "not_planned"})
    return task


def reopen_canceled(tasks: list[int]) -> list[int]:
    """Reopens the canceled tasks of a pull request Dependabot reopened, so its task is open again."""
    reopened = []
    for task in tasks:
        issue = api_object("GET", f"issues/{task}")
        if issue.get("state") == "closed" and issue.get("state_reason") == "not_planned":
            api("POST", f"issues/{task}/comments", {
                "body": "Reopened: Dependabot reopened the pull request this task belongs to. The agent sets the "
                        "Project Status by the evidence (specification §13, A4)."})
            api("PATCH", f"issues/{task}", {"state": "open"})
            reopened.append(task)
    return reopened


def main() -> None:
    number = int(os.environ["PR_NUMBER"])
    pull_request = fetch_pull_request(number)
    if pull_request.author != BOT:
        print(f"#{number} was opened by @{pull_request.author}, not Dependabot; nothing to do.")
        return
    if os.environ.get("PR_ACTION") == "closed":
        if pull_request.merged:
            print(f"#{number} merged; the review workflow closes its task.")
            return
        task = cancel_task(pull_request)
        print(f"#{number} closed without merging; " + (f"task #{task} canceled." if task else "it had no open task."))
        return
    linked = closing_numbers(pull_request.body)
    if linked:
        reopened = reopen_canceled(linked) if os.environ.get("PR_ACTION") == "reopened" else []
        print(f"#{number} already closes {', '.join(f'#{task}' for task in linked)}"
              + (f"; reopened {', '.join(f'#{task}' for task in reopened)}." if reopened else "."))
        return
    print(f"#{number} closes task #{open_task(pull_request)} under #{UPDATES}.")


if __name__ == "__main__":
    main()
