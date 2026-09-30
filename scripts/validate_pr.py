"""Check the required pull-request description structure."""

from __future__ import annotations

import os
from closing_work_items import closing_numbers


REQUIRED_SECTIONS = (
    "What changed",
    "Why it changed",
    "How it was tested",
    "What to review carefully",
    "Migration or deployment notes",
)


# Only this item may stay unchecked when the pull request opens: CI and approval happen afterwards.
DEFERRED_ITEM = "Required CI checks pass"


def unchecked_self_review_items(body: str) -> list[str]:
    section = body.split("### Author self-review", 1)[1] if "### Author self-review" in body else ""
    items = [line.strip()[len("- [ ]"):].strip() for line in section.splitlines() if line.strip().startswith("- [ ]")]
    return [item for item in items if not item.startswith(DEFERRED_ITEM)]


def validate(body: str) -> list[str]:
    errors: list[str] = []
    headings = [line[3:].strip() for line in body.splitlines() if line.startswith("## ")]
    if headings[: len(REQUIRED_SECTIONS)] != list(REQUIRED_SECTIONS):
        errors.append("PR description must contain the five required sections in order")
    if not closing_numbers(body):
        errors.append("PR description must have a standalone Closes #number line before the first section")
    if "### Author self-review" not in body or "- [" not in body:
        errors.append("PR description must include the author self-review checklist")
    for item in unchecked_self_review_items(body):
        errors.append(f"Author self-review item is not checked: {item}")
    for placeholder in ("Describe the user or maintainer-visible outcome.", "List automated checks, negative cases"):
        if placeholder in body:
            errors.append("PR description still contains an unfilled template instruction")
            break
    return errors


def main() -> int:
    errors = validate(os.environ.get("PR_BODY", ""))
    for error in errors:
        print(f"ERROR: {error}")
    if errors:
        return 1
    print("Pull-request description passed")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
