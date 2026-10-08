"""Decide which checks a pull request needs, from the files it changes (#579, #582).

The `checks` workflow runs its `changes` job first, then starts each check only when the files it reads changed. A
skipped job counts as passed for a required check, so a check is skipped only when nothing it reads can have changed;
whenever that isn't certain, it runs (owner decisions on #579 and #582):

- `build-test` (format, build, tests, coverage) is skipped when every changed file is documentation, a skill, a
  script other than `coverage_gate.py`, the addon, an issue form, or a workflow other than `checks.yml`.
- `companion` (the Windows executable of the companion) runs when the companion's own projects or a build file
  changed.
- `docs` (documentation checks, script tests, the wiki and site builds) is skipped when every changed file is code
  under `src/` or `test/`, added or modified, and none is a Markdown, SVG or Penpot file: a deleted or renamed file can
  break a link, and those file types have documentation rules.
- `sonar` isn't decided here: SonarCloud reads every language, so it runs on every pull request.

With `--published`, it decides instead whether a merged pull request changed what `docs-publish` builds (the site
and the wiki come from `docs/`, `mkdocs.yml` and `scripts/build_wiki.py`), and prints `publish=` (#586).

A change to `checks.yml` or to this script runs everything, and so does any event other than a pull request (a run
on `main`).

It prints `docs=`, `build-test=` and `companion=` lines for `$GITHUB_OUTPUT` on standard output, and the reasons on
standard error. The pull request's number comes from `PR_NUMBER`, checked to be a number; the repository is a
constant, so no command-line text reaches `gh`.

Usage, in the `changes` job, with GH_TOKEN, EVENT_NAME and PR_NUMBER set by the workflow:
    check_scope.py >> "$GITHUB_OUTPUT"
and in the review workflow after a merge, with GH_TOKEN and PR_NUMBER:
    check_scope.py --published
"""

from __future__ import annotations

import argparse
import json
import os
import subprocess
import sys
from dataclasses import dataclass

REPOSITORY = "AnnabiGihed/RaidManager"
PULL_REQUEST_EVENT = "pull_request"
CHECKS = ("docs", "build-test", "companion")
# Changing how the checks are chosen runs every check.
SCOPE_FILES = frozenset({".github/workflows/checks.yml", "scripts/check_scope.py"})
# Files no part of the .NET build or its tests reads.
NOT_DOTNET_FOLDERS = ("docs/", ".agents/", ".claude/", ".vale/", "scripts/", "src/Addon/", "test/Addon/",
                      ".github/ISSUE_TEMPLATE/", ".github/workflows/")
NOT_DOTNET_FILES = frozenset({"mkdocs.yml", ".luacheckrc", ".stylua.toml", ".markdownlint-cli2.yaml", ".vale.ini",
                              "mypy.ini", "pyrightconfig.json", "LICENSE"})
# The .NET job runs this script, so it isn't excluded with the other scripts.
DOTNET_SCRIPTS = frozenset({"scripts/coverage_gate.py"})
COMPANION_FOLDERS = ("src/Containers/UI/Core/RaidManager.Companion.Client/",
                     "src/Containers/UI/Hosting/RaidManager.Companion/")
BUILD_FILES = frozenset({"Directory.Build.props", "Directory.Packages.props", "nuget.config", "global.json",
                         "dotnet-tools.json", "RaidManager.sln", ".editorconfig", "stylecop.json"})
CODE_FOLDERS = ("src/", "test/")
DOCUMENTED_SUFFIXES = (".md", ".svg", ".penpot")
UNCHANGED_PATHS = frozenset({"added", "modified"})
PUBLISHED_FOLDERS = ("docs/",)
PUBLISHED_FILES = frozenset({"mkdocs.yml", "scripts/build_wiki.py", ".github/workflows/docs-publish.yml"})


@dataclass(frozen=True)
class ChangedFile:
    """A file a pull request changes, as GitHub lists it."""

    path: str
    status: str = "modified"
    previous_path: str | None = None

    @property
    def paths(self) -> tuple[str, ...]:
        """The file's path, and its previous one for a rename."""
        return (self.path,) if self.previous_path is None else (self.path, self.previous_path)


def affects_dotnet(path: str) -> bool:
    """Tells whether a changed file can change the .NET build or its tests."""
    if path in DOTNET_SCRIPTS:
        return True
    if path.startswith(NOT_DOTNET_FOLDERS) or path in NOT_DOTNET_FILES:
        return False
    return not ("/" not in path and path.endswith(".md"))


def affects_companion(path: str) -> bool:
    """Tells whether a changed file can change the companion's executable."""
    return path.startswith(COMPANION_FOLDERS) or path in BUILD_FILES


def affects_published_docs(path: str) -> bool:
    """Tells whether a changed file can change the published site or wiki."""
    return path.startswith(PUBLISHED_FOLDERS) or path in PUBLISHED_FILES


def publishes(changed: list[ChangedFile]) -> bool:
    """Tells whether a merged pull request needs the site and the wiki published again."""
    return any(affects_published_docs(path) for item in changed for path in item.paths)


def is_plain_code(changed: ChangedFile) -> bool:
    """Tells whether a change is code the documentation checks never read."""
    return (changed.status in UNCHANGED_PATHS and changed.path.startswith(CODE_FOLDERS)
            and not changed.path.endswith(DOCUMENTED_SUFFIXES))


def scopes(changed: list[ChangedFile]) -> dict[str, bool]:
    """Chooses the checks a pull request's changes need."""
    if any(path in SCOPE_FILES for item in changed for path in item.paths):
        return dict.fromkeys(CHECKS, True)
    return {
        "docs": not all(is_plain_code(item) for item in changed),
        "build-test": any(affects_dotnet(path) for item in changed for path in item.paths),
        "companion": any(affects_companion(path) for item in changed for path in item.paths),
    }


def changed_files(number: int) -> list[ChangedFile]:
    """Lists the files of a pull request, with their status and a renamed file's previous path."""
    output = subprocess.run(
        ["gh", "api", "--paginate", f"repos/{REPOSITORY}/pulls/{number}/files",
         "--jq", ".[] | {path: .filename, status: .status, previous_path: .previous_filename}"],
        capture_output=True, text=True, check=True).stdout
    return [ChangedFile(**json.loads(line)) for line in output.splitlines() if line.strip()]


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--published", action="store_true",
                        help="decide whether the merged pull request in PR_NUMBER needs docs-publish")
    args = parser.parse_args()
    if args.published:
        number = int(os.environ["PR_NUMBER"])
        publish = publishes(changed_files(number))
        print(f"publish={'true' if publish else 'false'}")
        print(f"docs-publish: {'runs' if publish else 'skipped, no published file changed'}", file=sys.stderr)
        return
    if os.environ.get("EVENT_NAME") != PULL_REQUEST_EVENT:
        chosen = dict.fromkeys(CHECKS, True)
        print("Not a pull request: every check runs.", file=sys.stderr)
    else:
        number = int(os.environ["PR_NUMBER"])
        changed = changed_files(number)
        chosen = scopes(changed)
        print(f"{len(changed)} changed file(s) in #{number}.", file=sys.stderr)
    for check in CHECKS:
        print(f"{check}={'true' if chosen[check] else 'false'}")
        print(f"{check}: {'runs' if chosen[check] else 'skipped, none of its files changed'}", file=sys.stderr)


if __name__ == "__main__":
    main()
