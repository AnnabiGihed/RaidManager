"""UI mockup rules (ADR-0017): every UI work item and every UI change shows its Penpot mockup.

- Issues: an item labelled `ui` links or shows its mockup. The hierarchy workflow flags it `needs-mockup` otherwise.
- Pull requests: a pull request that changes UI files shows its mockup, or states "No visual change:" with a reason.
- Folder: `docs/mockups/` holds each screen's `.penpot` source next to the SVG rendered from it (ADR-0018), and no
  `.penpot` or `.svg` file lives anywhere else, such as the repository root.
"""

from __future__ import annotations

import argparse
import os
import re
import subprocess
import sys
from pathlib import Path, PurePosixPath

from penpot_render import is_current


UI_LABEL = "ui"
NEEDS_MOCKUP = "needs-mockup"
MOCKUP_FOLDER = "docs/mockups"
REPOSITORY_MOCKUP = re.compile(r"docs/mockups/[\w./-]+?\.svg", re.IGNORECASE)
PENPOT_LINK = re.compile(r"https://(?:design\.)?penpot\.app/\S+", re.IGNORECASE)
NO_VISUAL_CHANGE = re.compile(r"^No visual change:[ \t]*\S.{9,}$", re.IGNORECASE | re.MULTILINE)
FORM_ANSWER = re.compile(r"^###\s*User interface\s*\n+\s*(?P<answer>\S[^\n]*)", re.IGNORECASE | re.MULTILINE)
UI_SUFFIXES = (".razor", ".razor.css", ".css", ".html", ".lua", ".toc")
UI_FOLDERS = ("src/Containers/UI/", "addon/")
MOCKUP_SUFFIXES = (".penpot", ".svg")
MAX_LISTED_FILES = 5
# SVGs belong to the documentation (diagrams and mockups) or a web project's static assets; nothing else.
SVG_FOLDERS = ("docs/",)
SVG_ASSET_SEGMENT = "/wwwroot/"
SKIPPED_FOLDERS = frozenset({".git", "bin", "obj", "node_modules", "TestResults", "site", ".venv", ".vs"})


def mockup_references(text: str) -> list[str]:
    """Returns the repository mockup paths and Penpot links a text contains."""
    return REPOSITORY_MOCKUP.findall(text or "") + PENPOT_LINK.findall(text or "")


def ui_requested(body: str) -> bool:
    """Tells whether an issue form answered "Yes" to the User interface question."""
    match = FORM_ANSWER.search(body or "")
    if match is None:
        return False
    return match["answer"].strip().lower().startswith("yes")


def mockup_problem(labels: frozenset[str], body: str) -> str | None:
    """Returns why a UI work item needs a mockup reference, or None when it has one or isn't UI work."""
    if UI_LABEL not in labels or mockup_references(body):
        return None
    return ("This item changes a user interface, so it must link or show its mockup: an exported "
            "`docs/mockups/<screen>.svg` (as a link or an image) or a Penpot share link.")


def is_ui_file(path: str) -> bool:
    """Tells whether a changed file is user-interface code: website, addon, or their markup and styles."""
    posix = PurePosixPath(path).as_posix()
    if posix.startswith(("test/", "docs/")):
        return False
    return posix.startswith(UI_FOLDERS) or posix.endswith(UI_SUFFIXES)


def pull_request_problems(body: str, changed: list[str], root: Path) -> list[str]:
    """Returns why a pull request that changes UI files may not merge without a mockup."""
    ui_files = [path for path in changed if is_ui_file(path)]
    if not ui_files:
        return []
    if NO_VISUAL_CHANGE.search(body or ""):
        return []
    references = mockup_references(body)
    listed = ", ".join(f"`{path}`" for path in ui_files[:MAX_LISTED_FILES])
    more = f" and {len(ui_files) - MAX_LISTED_FILES} more" if len(ui_files) > MAX_LISTED_FILES else ""
    if not references:
        return [f"This pull request changes user-interface files ({listed}{more}). Show the mockup it implements "
                "with a `docs/mockups/<screen>.svg` image or link, or a Penpot link, or explain on its own line: "
                "`No visual change: <reason>`."]
    missing = [path for path in REPOSITORY_MOCKUP.findall(body) if not (root / path).is_file()]
    return [f"The mockup `{path}` isn't in the repository; commit it with its `.penpot` source." for path in missing]


def repository_files(root: Path, suffixes: tuple[str, ...]) -> list[Path]:
    """Lists the working-tree files with the suffixes, skipping build output and tool folders."""
    found: list[Path] = []
    if not root.is_dir():
        return found
    for path in root.iterdir():
        if path.name in SKIPPED_FOLDERS:
            continue
        if path.is_dir():
            found += [item for item in path.rglob("*") if item.is_file() and item.name.endswith(suffixes)
                      and not SKIPPED_FOLDERS.intersection(item.relative_to(root).parts)]
        elif path.name.endswith(suffixes):
            found.append(path)
    return found


def placement_problems(root: Path) -> list[str]:
    """Rejects mockup files outside docs/mockups, and SVG files outside the documentation or web assets."""
    problems: list[str] = []
    for path in repository_files(root, MOCKUP_SUFFIXES):
        relative = path.relative_to(root).as_posix()
        if path.suffix == ".penpot" and not relative.startswith(f"{MOCKUP_FOLDER}/"):
            problems.append(f"{relative}: Penpot files live only in {MOCKUP_FOLDER}/")
        elif path.suffix == ".svg" and not relative.startswith(SVG_FOLDERS) and SVG_ASSET_SEGMENT not in f"/{relative}":
            problems.append(f"{relative}: SVG files live only in docs/ (mockups and diagrams) or a web project's wwwroot/")
    return problems


def folder_problems(root: Path) -> list[str]:
    """Checks the mockup folder: each `.penpot` has its SVG, each SVG is the current rendering of its `.penpot`."""
    problems = placement_problems(root)
    folder = root / MOCKUP_FOLDER
    if not folder.is_dir():
        return problems
    for path in sorted(item for item in folder.rglob("*") if item.is_file()):
        relative = path.relative_to(root).as_posix()
        if not path.name.endswith(MOCKUP_SUFFIXES):
            problems.append(f"{relative}: {MOCKUP_FOLDER} holds only .penpot sources and the SVGs rendered from them")
        elif path.suffix == ".svg" and not path.with_suffix(".penpot").is_file():
            problems.append(f"{relative}: missing its Penpot source {path.with_suffix('.penpot').name}")
        elif path.suffix == ".penpot" and not path.with_suffix(".svg").is_file():
            problems.append(f"{relative}: missing its SVG; run python scripts/penpot_render.py {relative}")
        elif path.suffix == ".penpot" and not is_current(path):
            problems.append(f"{relative}: its SVG isn't the current rendering; run python scripts/penpot_render.py {relative}")
    return problems


def changed_files(repository: str, number: str) -> list[str]:
    result = subprocess.run(["gh", "api", "--paginate", f"repos/{repository}/pulls/{number}/files", "--jq", ".[].filename"],
                            check=True, capture_output=True, text=True, encoding="utf-8")
    return [line for line in result.stdout.splitlines() if line]


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--root", type=Path, default=Path.cwd())
    parser.add_argument("--pull-request", action="store_true",
                        help="check the pull request in PR_NUMBER and PR_BODY; otherwise check the mockup folder")
    args = parser.parse_args()
    if args.pull_request:
        changed = changed_files(os.environ["GITHUB_REPOSITORY"], os.environ["PR_NUMBER"])
        problems = pull_request_problems(os.environ.get("PR_BODY", ""), changed, args.root)
        success = "No user-interface change needs a mockup, or the pull request shows it."
    else:
        problems = folder_problems(args.root)
        success = f"Every mockup in {MOCKUP_FOLDER} has its Penpot source and SVG export."
    for problem in problems:
        print(f"ERROR: {problem}")
    if not problems:
        print(success)
    return 1 if problems else 0


if __name__ == "__main__":
    sys.exit(main())
