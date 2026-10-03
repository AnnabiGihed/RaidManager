"""Validate repository documentation invariants that Markdown lint cannot check."""

from __future__ import annotations

import argparse
import datetime as dt
import re
from pathlib import Path

from ui_mockups import folder_problems


REQUIRED_FILES = ("README.md", "CHANGELOG.md", "CONTRIBUTING.md", "LICENSE", "SECURITY.md")
CHANGELOG_SECTIONS = {"Added", "Changed", "Deprecated", "Removed", "Fixed", "Security", "Breaking changes"}
RELEASE_HEADING = re.compile(r"\[(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)\] - (\d{4}-\d{2}-\d{2})")


def headings(document: str, level: int) -> list[str]:
    prefix = "#" * level + " "
    return [line[len(prefix) :].strip() for line in document.splitlines() if line.startswith(prefix)]


def validate_changelog(document: str) -> list[str]:
    errors: list[str] = []
    sections = headings(document, 2)
    if not sections or sections[0] != "[Unreleased]":
        errors.append("CHANGELOG.md must start its versions with ## [Unreleased]")
    if sections.count("[Unreleased]") != 1:
        errors.append("CHANGELOG.md must contain exactly one [Unreleased] section")
    for section in sections[1:]:
        match = RELEASE_HEADING.fullmatch(section)
        if match is None:
            errors.append(f"CHANGELOG.md has an invalid release heading: {section}")
            continue
        try:
            dt.date.fromisoformat(match.group(4))
        except ValueError:
            errors.append(f"CHANGELOG.md has an invalid release date: {section}")
    for section in headings(document, 3):
        if section not in CHANGELOG_SECTIONS:
            errors.append(f"CHANGELOG.md has an unknown change category: {section}")
    if "keepachangelog.com" not in document.lower() or "semver.org" not in document.lower():
        errors.append("CHANGELOG.md must link Keep a Changelog and Semantic Versioning")
    return errors


def validate_contributing(document: str) -> list[str]:
    errors: list[str] = []
    sections = set(headings(document, 2))
    required = {"Work items", "Branching", "Commit messages", "Pull requests", "Local setup", "Releases", "Security"}
    for section in sorted(required - sections):
        errors.append(f"CONTRIBUTING.md is missing ## {section}")
    for term in ("Conventional Commits", "main", "dotnet restore", "dotnet build", "dotnet test", "SECURITY.md"):
        if term not in document:
            errors.append(f"CONTRIBUTING.md is missing {term}")
    return errors


def validate_diagrams(root: Path) -> list[str]:
    diagram_dir = root / "docs" / "diagrams"
    errors: list[str] = []
    for source in (*diagram_dir.glob("*.mmd"), *diagram_dir.glob("*.puml")):
        if not source.with_suffix(".svg").is_file():
            errors.append(f"Missing SVG export for {source.relative_to(root).as_posix()}")
    for export in diagram_dir.glob("*.svg"):
        if not export.with_suffix(".mmd").is_file() and not export.with_suffix(".puml").is_file():
            errors.append(f"Missing diagram source for {export.relative_to(root).as_posix()}")
    return errors


def validate_readme(content: str) -> list[str]:
    errors: list[str] = []
    if not content.startswith("# RaidManager\n"):
        errors.append("README.md must start with the RaidManager title")
    for section in ("Status", "Build locally", "Documentation"):
        if section not in headings(content, 2):
            errors.append(f"README.md is missing ## {section}")
    if not re.search(r"\*\*(Production|Beta|Experimental|Deprecated)\.\*\*", content):
        errors.append("README.md must state one recognized project status")
    if "Gihed Annabi" not in content or "github.com/AnnabiGihed/RaidManager/issues" not in content:
        errors.append("README.md must name the owner and issue contact channel")
    return errors


def validate_license(content: str) -> list[str]:
    if not re.search(r"Copyright \(c\) \d{4} Gihed Annabi", content) or "proprietary" not in content.lower():
        return ["LICENSE must contain Gihed Annabi's proprietary license text"]
    return []


# Each root file that has rules, with the check that reads its text.
ROOT_FILE_CHECKS = (
    ("README.md", validate_readme),
    ("CHANGELOG.md", validate_changelog),
    ("CONTRIBUTING.md", validate_contributing),
    ("LICENSE", validate_license),
)


def validate(root: Path) -> list[str]:
    errors = [f"Missing required root file: {name}" for name in REQUIRED_FILES if not (root / name).is_file()]
    for name, check in ROOT_FILE_CHECKS:
        if (root / name).is_file():
            errors.extend(check((root / name).read_text(encoding="utf-8")))
    errors.extend(validate_diagrams(root))
    errors.extend(folder_problems(root))
    return errors


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--root", type=Path, default=Path.cwd())
    args = parser.parse_args()
    errors = validate(args.root)
    for error in errors:
        print(f"ERROR: {error}")
    if errors:
        return 1
    print("Documentation invariants passed")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
