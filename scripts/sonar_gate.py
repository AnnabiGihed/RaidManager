"""Fail a pull request on SonarCloud findings (ADR-0020).

SonarCloud analyses every pull request and reports its findings as a check, but that check follows the project's
quality gate, which only looks at ratings, coverage and duplication: a pull request with new code smells, or even a
vulnerability that keeps the rating, still passes. This check waits for SonarCloud's analysis of the head commit, then
fails when the pull request has any open issue or any security hotspot to review, listing each as an annotation.

The project is public, so the SonarCloud web API needs no token.

    python scripts/sonar_gate.py --project AnnabiGihed_RaidManager --pull-request 205 --commit <head sha>
"""

from __future__ import annotations

import argparse
import json
import sys
import time
import urllib.parse
import urllib.request
from collections.abc import Callable
from dataclasses import dataclass

SONAR = "https://sonarcloud.io"
PAGE_SIZE = 500
Fetch = Callable[[str, dict[str, str]], dict]


@dataclass(frozen=True)
class Finding:
    kind: str
    path: str
    line: int | None
    rule: str
    severity: str
    message: str

    def annotation(self) -> str:
        """A GitHub Actions error annotation, shown on the file and line in the pull request."""
        location = f"file={self.path}" + (f",line={self.line}" if self.line else "")
        message = self.message.replace("%", "%25").replace("\r", "%0D").replace("\n", "%0A")
        return f"::error {location},title=SonarCloud {self.rule}::{self.kind} ({self.severity}): {message}"


def fetch_json(path: str, parameters: dict[str, str]) -> dict:
    url = f"{SONAR}{path}?{urllib.parse.urlencode(parameters)}"
    with urllib.request.urlopen(url, timeout=30) as response:  # noqa: S310 - fixed https host
        return json.load(response)


def analysed_commit(pull_requests: list[dict], number: str) -> str | None:
    """Returns the commit SonarCloud last analysed for the pull request, or None before its first analysis."""
    for pull_request in pull_requests:
        if str(pull_request.get("key")) == number:
            return (pull_request.get("commit") or {}).get("sha")
    return None


def component_path(component: str) -> str:
    """Turns a component key (`<project>:<path>`) into a repository path."""
    return component.split(":", 1)[1] if ":" in component else component


def pages(fetch: Fetch, path: str, parameters: dict[str, str], key: str) -> list[dict]:
    """Reads every page of a SonarCloud search."""
    items: list[dict] = []
    page = 1
    while True:
        result = fetch(path, {**parameters, "ps": str(PAGE_SIZE), "p": str(page)})
        items += result.get(key, [])
        total = (result.get("paging") or {}).get("total", result.get("total", 0))
        if not result.get(key) or len(items) >= total:
            return items
        page += 1


def findings(fetch: Fetch, project: str, number: str) -> list[Finding]:
    """Lists the pull request's open issues and the security hotspots still to review."""
    issues = pages(fetch, "/api/issues/search",
                   {"componentKeys": project, "pullRequest": number, "resolved": "false"}, "issues")
    hotspots = pages(fetch, "/api/hotspots/search",
                     {"projectKey": project, "pullRequest": number, "status": "TO_REVIEW"}, "hotspots")
    found = [Finding(issue.get("type", "ISSUE").replace("_", " ").lower(), component_path(issue["component"]),
                     issue.get("line"), issue["rule"], issue.get("severity", ""), issue["message"])
             for issue in issues]
    found += [Finding("security hotspot", component_path(hotspot["component"]), hotspot.get("line"),
                      hotspot.get("ruleKey", ""), hotspot.get("vulnerabilityProbability", ""), hotspot["message"])
              for hotspot in hotspots]
    return sorted(found, key=lambda finding: (finding.path, finding.line or 0, finding.rule))


def wait_for_analysis(fetch: Fetch, project: str, number: str, commit: str, timeout: float, interval: float,
                      sleep: Callable[[float], None] = time.sleep, clock: Callable[[], float] = time.monotonic) -> bool:
    """Waits until SonarCloud has analysed the head commit; False when it hasn't within the timeout."""
    deadline = clock() + timeout
    while True:
        pull_requests = fetch("/api/project_pull_requests/list", {"project": project}).get("pullRequests", [])
        if analysed_commit(pull_requests, number) == commit:
            return True
        if clock() >= deadline:
            return False
        sleep(interval)


def gate(fetch: Fetch, project: str, number: str, commit: str, timeout: float, interval: float,
         sleep: Callable[[float], None] = time.sleep, clock: Callable[[], float] = time.monotonic) -> tuple[int, list[str]]:
    """Returns the exit code and the lines to print."""
    if not wait_for_analysis(fetch, project, number, commit, timeout, interval, sleep, clock):
        return 1, [f"::error title=SonarCloud::SonarCloud didn't analyse {commit[:7]} within {timeout / 60:g} minutes. "
                   "Rerun this job once the SonarCloud Code Analysis check has completed."]
    found = findings(fetch, project, number)
    if not found:
        return 0, [f"SonarCloud reports no open issue and no security hotspot to review for {commit[:7]}."]
    lines = [finding.annotation() for finding in found]
    lines.append(f"SonarCloud reports {len(found)} finding(s) on this pull request. Fix each one; a finding that is "
                 "genuinely wrong is marked as accepted or false positive in SonarCloud with a reason (ADR-0020).")
    return 1, lines


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--project", required=True, help="the SonarCloud project key")
    parser.add_argument("--pull-request", required=True, help="the pull request number")
    parser.add_argument("--commit", required=True, help="the pull request's head commit")
    parser.add_argument("--timeout", type=float, default=900, help="seconds to wait for the analysis")
    parser.add_argument("--interval", type=float, default=20, help="seconds between checks")
    args = parser.parse_args()
    code, lines = gate(fetch_json, args.project, args.pull_request, args.commit, args.timeout, args.interval)
    print("\n".join(lines))
    return code


if __name__ == "__main__":
    sys.exit(main())
