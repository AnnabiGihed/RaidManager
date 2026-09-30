"""Merge the Cobertura coverage reports of a test run and enforce the coverage gate (ADR-0015).

A pull request passes when at least MIN_CHANGED percent of its changed, coverable source lines are covered and the
total line coverage is at least MIN_TOTAL percent. The script writes a Markdown summary for the run page and the
pull-request comment.
"""

from __future__ import annotations

import argparse
import re
import subprocess
import sys
import xml.etree.ElementTree as ElementTree
from dataclasses import dataclass, field
from pathlib import Path, PurePosixPath


MIN_CHANGED = 80.0
MIN_TOTAL = 60.0
MARKER = "<!-- coverage-report -->"
HUNK = re.compile(r"^@@ -\d+(?:,\d+)? \+(?P<start>\d+)(?:,(?P<count>\d+))? @@")
MAX_LISTED_FILES = 15


@dataclass
class Coverage:
    """Line hits per repository-relative source file, merged across every report."""

    lines: dict[str, dict[int, bool]] = field(default_factory=dict)
    projects: dict[str, str] = field(default_factory=dict)

    def add(self, path: str, project: str, number: int, covered: bool) -> None:
        file_lines = self.lines.setdefault(path, {})
        file_lines[number] = file_lines.get(number, False) or covered
        self.projects.setdefault(path, project)


@dataclass(frozen=True)
class Ratio:
    covered: int
    total: int

    @property
    def percent(self) -> float | None:
        return 100.0 * self.covered / self.total if self.total else None


def relative_path(filename: str, sources: list[str], root: Path) -> str | None:
    """Returns a report file name as a repository-relative POSIX path, or None when it is outside the repository."""
    candidates = [Path(filename)] if Path(filename).is_absolute() else [Path(source) / filename for source in sources]
    for candidate in candidates:
        try:
            return PurePosixPath(candidate.resolve().relative_to(root.resolve()).as_posix()).as_posix()
        except ValueError:
            continue
    return None


def read_reports(paths: list[Path], root: Path) -> Coverage:
    coverage = Coverage()
    for path in paths:
        report = ElementTree.parse(path).getroot()
        sources = [source.text or "" for source in report.iter("source")]
        for package in report.iter("package"):
            project = package.get("name", "")
            for cls in package.iter("class"):
                relative = relative_path(cls.get("filename", ""), sources, root)
                if relative is None or not relative.startswith("src/"):
                    continue
                for line in cls.iter("line"):
                    coverage.add(relative, project, int(line.get("number", "0")), int(line.get("hits", "0")) > 0)
    return coverage


def changed_lines(diff: str) -> dict[str, set[int]]:
    """Reads the added or modified line numbers of each file from a `git diff -U0` output."""
    changes: dict[str, set[int]] = {}
    current: str | None = None
    for line in diff.splitlines():
        if line.startswith("+++ "):
            target = line[4:].strip()
            current = target[2:] if target.startswith("b/") else None
        elif current and (match := HUNK.match(line)):
            start, count = int(match["start"]), int(match["count"] or "1")
            changes.setdefault(current, set()).update(range(start, start + count))
    return changes


def total_ratio(coverage: Coverage, files: list[str] | None = None) -> Ratio:
    selected = coverage.lines if files is None else {name: coverage.lines[name] for name in files}
    return Ratio(sum(sum(lines.values()) for lines in selected.values()), sum(len(lines) for lines in selected.values()))


def changed_ratio(coverage: Coverage, changes: dict[str, set[int]]) -> tuple[Ratio, dict[str, list[int]]]:
    """Returns the coverage of changed coverable lines, and the uncovered changed lines per file."""
    covered = total = 0
    uncovered: dict[str, list[int]] = {}
    for path, numbers in sorted(changes.items()):
        file_lines = coverage.lines.get(path, {})
        for number in sorted(numbers & file_lines.keys()):
            total += 1
            if file_lines[number]:
                covered += 1
            else:
                uncovered.setdefault(path, []).append(number)
    return Ratio(covered, total), uncovered


def unmeasured_projects(root: Path, coverage: Coverage) -> list[str]:
    """Returns the src/ projects no test loaded: they have no coverage data, so the total can't include them."""
    measured = set(coverage.projects.values())
    return sorted(path.stem for path in (root / "src").rglob("*.csproj") if path.stem not in measured)


def gate_errors(total: Ratio, changed: Ratio | None, min_total: float, min_changed: float) -> list[str]:
    errors = []
    if total.percent is None:
        errors.append("No coverage data was found: did the tests run with coverage.runsettings?")
    elif total.percent < min_total:
        errors.append(f"Total line coverage is {total.percent:.1f}%, under the {min_total:.0f}% minimum.")
    if changed is not None and changed.percent is not None and changed.percent < min_changed:
        errors.append(f"Changed lines are {changed.percent:.1f}% covered, under the {min_changed:.0f}% minimum.")
    return errors


def line_ranges(numbers: list[int]) -> str:
    ranges: list[str] = []
    start = previous = numbers[0]
    for number in [*numbers[1:], None]:
        if number is not None and number == previous + 1:
            previous = number
            continue
        ranges.append(str(start) if start == previous else f"{start}-{previous}")
        if number is not None:
            start = previous = number
    return ", ".join(ranges)


def cell(ratio: Ratio) -> str:
    return "n/a" if ratio.percent is None else f"{ratio.percent:.1f}%"


def summary(coverage: Coverage, total: Ratio, changed: Ratio | None, uncovered: dict[str, list[int]],
            errors: list[str], min_total: float, min_changed: float, unmeasured: list[str] | None = None) -> str:
    def status(ratio: Ratio, minimum: float) -> str:
        return "➖" if ratio.percent is None else ("✅" if ratio.percent >= minimum else "❌")

    lines = [MARKER, "## Test coverage", ""]
    lines.append("❌ **The coverage gate failed.**" if errors else "✅ **The coverage gate passed.**")
    lines += ["", "| Scope | Covered lines | Coverage | Minimum | |", "| --- | ---: | ---: | ---: | --- |"]
    if changed is not None:
        lines.append(f"| Changed lines | {changed.covered} / {changed.total} | {cell(changed)} | {min_changed:.0f}% "
                     f"| {status(changed, min_changed)} |")
    lines.append(f"| Total | {total.covered} / {total.total} | {cell(total)} | {min_total:.0f}% "
                 f"| {status(total, min_total)} |")
    if changed is not None and changed.total == 0:
        lines += ["", "This change has no coverable source lines, so only the total applies."]
    for error in errors:
        lines += ["", f"- {error}"]
    if unmeasured:
        lines += ["", "No test loads these projects, so they have no coverage data and the total leaves them out: "
                  + ", ".join(f"`{name}`" for name in unmeasured) + "."]
    if uncovered:
        lines += ["", "<details><summary>Uncovered changed lines</summary>", ""]
        for path, numbers in list(uncovered.items())[:MAX_LISTED_FILES]:
            lines.append(f"- `{path}`: {line_ranges(numbers)}")
        if len(uncovered) > MAX_LISTED_FILES:
            lines.append(f"- … and {len(uncovered) - MAX_LISTED_FILES} more files")
        lines += ["", "</details>"]
    by_project: dict[str, list[str]] = {}
    for path, project in coverage.projects.items():
        by_project.setdefault(project, []).append(path)
    lines += ["", "<details><summary>By project</summary>", "", "| Project | Covered lines | Coverage |",
              "| --- | ---: | ---: |"]
    for project in sorted(by_project):
        ratio = total_ratio(coverage, by_project[project])
        lines.append(f"| {project} | {ratio.covered} / {ratio.total} | {cell(ratio)} |")
    lines += ["", "</details>", ""]
    return "\n".join(lines)


def git_diff(base: str, root: Path) -> str:
    result = subprocess.run(["git", "diff", "-U0", "--no-color", "--no-renames", base, "HEAD", "--", "src"],
                            cwd=root, check=True, capture_output=True, text=True, encoding="utf-8")
    return result.stdout


def main() -> int:
    # The summary uses status symbols that some Windows consoles can't encode by default.
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--reports", type=Path, required=True, help="folder searched for coverage.cobertura.xml")
    parser.add_argument("--root", type=Path, default=Path.cwd())
    parser.add_argument("--base", default="", help="base commit of a pull request; empty checks the total only")
    parser.add_argument("--summary", type=Path, help="file the Markdown summary is written to")
    parser.add_argument("--min-total", type=float, default=MIN_TOTAL)
    parser.add_argument("--min-changed", type=float, default=MIN_CHANGED)
    args = parser.parse_args()

    coverage = read_reports(sorted(args.reports.rglob("coverage.cobertura.xml")), args.root)
    total = total_ratio(coverage)
    changed: Ratio | None = None
    uncovered: dict[str, list[int]] = {}
    if args.base:
        changed, uncovered = changed_ratio(coverage, changed_lines(git_diff(args.base, args.root)))
    errors = gate_errors(total, changed, args.min_total, args.min_changed)
    report = summary(coverage, total, changed, uncovered, errors, args.min_total, args.min_changed,
                     unmeasured_projects(args.root, coverage))
    if args.summary:
        args.summary.write_text(report, encoding="utf-8")
    print(report)
    return 1 if errors else 0


if __name__ == "__main__":
    sys.exit(main())
