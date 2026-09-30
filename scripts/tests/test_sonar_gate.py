"""Tests for the SonarCloud pull-request check (ADR-0020)."""

from __future__ import annotations

import sys
import unittest
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from sonar_gate import Finding, analysed_commit, component_path, gate  # noqa: E402

PROJECT = "AnnabiGihed_RaidManager"
ISSUES, HOTSPOTS = "/api/issues/search", "/api/hotspots/search"
HEAD = "0f8774e36beedf8c16ac9b06e86f4f9d7db6bdbd"
ISSUE = {"component": f"{PROJECT}:scripts/mockups/character_review.py", "line": 71, "rule": "python:S1192",
         "severity": "CRITICAL", "type": "CODE_SMELL",
         "message": 'Define a constant instead of duplicating this literal "Text/muted" 4 times.'}
HOTSPOT = {"component": f"{PROJECT}:scripts/sonar_gate.py", "line": 12, "ruleKey": "python:S5332",
           "vulnerabilityProbability": "LOW", "message": "Make sure that using clear-text protocols is safe here."}


class FakeSonar:
    """Answers the SonarCloud web API from canned data, analysing the head commit after `analysed_after` polls."""

    def __init__(self, issues: list[dict] | None = None, hotspots: list[dict] | None = None,
                 analysed_after: int = 0, page_size: int | None = None) -> None:
        self.issues = issues or []
        self.hotspots = hotspots or []
        self.analysed_after = analysed_after
        self.page_size = page_size
        self.polls = 0
        self.requests: list[tuple[str, dict[str, str]]] = []

    def __call__(self, path: str, parameters: dict[str, str]) -> dict:
        self.requests.append((path, parameters))
        if path == "/api/project_pull_requests/list":
            self.polls += 1
            sha = HEAD if self.polls > self.analysed_after else "0" * 40
            return {"pullRequests": [{"key": "204", "commit": {"sha": HEAD}}, {"key": "205", "commit": {"sha": sha}}]}
        key, items = ("issues", self.issues) if path == ISSUES else ("hotspots", self.hotspots)
        size = self.page_size or int(parameters["ps"])
        start = (int(parameters["p"]) - 1) * size
        return {key: items[start:start + size], "paging": {"total": len(items)}}


class Clock:
    def __init__(self) -> None:
        self.now = 0.0

    def __call__(self) -> float:
        return self.now

    def sleep(self, seconds: float) -> None:
        self.now += seconds


def run(sonar: FakeSonar, timeout: float = 60) -> tuple[int, list[str], Clock]:
    clock = Clock()
    code, lines = gate(sonar, PROJECT, "205", HEAD, timeout, 20, clock.sleep, clock)
    return code, lines, clock


class SonarGateTests(unittest.TestCase):
    def test_a_clean_analysis_passes(self) -> None:
        code, lines, _ = run(FakeSonar())
        self.assertEqual(code, 0)
        self.assertIn("no open issue and no security hotspot", lines[0])

    def test_an_open_issue_fails_with_an_annotation_on_its_line(self) -> None:
        code, lines, _ = run(FakeSonar(issues=[ISSUE]))
        self.assertEqual(code, 1)
        self.assertEqual(lines[0], "::error file=scripts/mockups/character_review.py,line=71,title=SonarCloud python:S1192"
                                   "::code smell (CRITICAL): Define a constant instead of duplicating this literal "
                                   '"Text/muted" 4 times.')
        self.assertIn("1 finding(s)", lines[-1])

    def test_a_hotspot_to_review_fails(self) -> None:
        code, lines, _ = run(FakeSonar(hotspots=[HOTSPOT]))
        self.assertEqual(code, 1)
        self.assertIn("title=SonarCloud python:S5332::security hotspot (LOW)", lines[0])

    def test_only_unresolved_issues_and_hotspots_to_review_are_asked_for(self) -> None:
        sonar = FakeSonar()
        run(sonar)
        parameters = {path: query for path, query in sonar.requests}
        self.assertEqual(parameters[ISSUES]["resolved"], "false")
        self.assertEqual(parameters[ISSUES]["pullRequest"], "205")
        self.assertEqual(parameters[HOTSPOTS]["status"], "TO_REVIEW")

    def test_it_waits_for_the_head_commit_to_be_analysed(self) -> None:
        code, _, clock = run(FakeSonar(issues=[ISSUE], analysed_after=2))
        self.assertEqual(code, 1)
        self.assertEqual(clock.now, 40)

    def test_it_fails_when_the_head_commit_is_never_analysed(self) -> None:
        code, lines, _ = run(FakeSonar(analysed_after=100), timeout=60)
        self.assertEqual(code, 1)
        self.assertIn("didn't analyse 0f8774e within 1 minutes", lines[0])

    def test_every_page_is_read(self) -> None:
        issues = [{**ISSUE, "line": line} for line in range(1, 6)]
        code, lines, _ = run(FakeSonar(issues=issues, page_size=2))
        self.assertEqual(code, 1)
        self.assertEqual(len(lines), 6)

    def test_findings_are_sorted_by_file_and_line(self) -> None:
        issues = [{**ISSUE, "line": 90}, {**ISSUE, "component": f"{PROJECT}:a.py", "line": 5}, {**ISSUE, "line": 3}]
        _, lines, _ = run(FakeSonar(issues=issues))
        self.assertEqual([line.split(",title")[0] for line in lines[:3]],
                         ["::error file=a.py,line=5", "::error file=scripts/mockups/character_review.py,line=3",
                          "::error file=scripts/mockups/character_review.py,line=90"])

    def test_helpers(self) -> None:
        self.assertEqual(analysed_commit([{"key": "7", "commit": {"sha": "abc"}}], "7"), "abc")
        self.assertIsNone(analysed_commit([{"key": "7"}], "8"))
        self.assertEqual(component_path(f"{PROJECT}:src/A.cs"), "src/A.cs")
        self.assertEqual(component_path(PROJECT), PROJECT)
        finding = Finding("bug", "a.py", None, "r", "MAJOR", "50% wrong\nline")
        self.assertEqual(finding.annotation(), "::error file=a.py,title=SonarCloud r::bug (MAJOR): 50%25 wrong%0Aline")


if __name__ == "__main__":
    unittest.main()
