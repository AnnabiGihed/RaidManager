"""Spell-check the documentation as strictly as an editor does: US English plus one project word list.

Every word outside code, links, URLs and HTML comments must be a US English dictionary word or appear in the project
word list, `.vale/styles/config/vocabularies/RaidManager/accept.txt`. Vale uses the same list, and `.editorconfig`
points Visual Studio's spell checker at it, so CI, Vale and the editor agree. British spellings fail, including the
doubled-consonant forms the dictionary would otherwise accept.

    python scripts/spell_check.py              # checks the published documentation
    python scripts/spell_check.py FILE...      # checks the given Markdown files
"""

from __future__ import annotations

import argparse
import re
import sys
from pathlib import Path

WORD_LIST = Path(".vale/styles/config/vocabularies/RaidManager/accept.txt")
ROOT_DOCUMENTS = ("README.md", "CONTRIBUTING.md", "CHANGELOG.md", "SECURITY.md", "AGENTS.md")
# British forms a US dictionary still lists; the documentation is written in US English.
BRITISH = {
    "labelled": "labeled", "labelling": "labeling", "cancelled": "canceled", "cancelling": "canceling",
    "modelled": "modeled", "modelling": "modeling", "travelled": "traveled", "licence": "license", "grey": "gray",
    "normalised": "normalized", "organised": "organized", "prioritise": "prioritize", "centre": "center",
    "colour": "color", "colours": "colors", "behaviour": "behavior", "favour": "favor", "analyse": "analyze",
}
FENCE = re.compile(r"^\s*(```|~~~)")
REMOVED = [
    re.compile(r"<!--.*?-->", re.DOTALL),              # HTML comments
    re.compile(r"`[^`\n]*`"),                           # inline code
    re.compile(r"\]\([^)]*\)"),                         # link and image targets
    re.compile(r"^\s*\[[^\]]+\]:\s*\S+.*$", re.MULTILINE),  # reference definitions
    re.compile(r"<[^>\n]+>"),                           # HTML tags and autolinks
    re.compile(r"\b(?:https?|mailto):\S+"),             # bare URLs
    re.compile(r"\S+@\S+\.\w+"),                        # email addresses
    re.compile(r"\{\{[^}]*\}\}|\$\{[^}]*\}"),           # template placeholders
]
WORD = re.compile(r"[A-Za-z]+(?:['’][A-Za-z]+)*")


def load_words(path: Path) -> set[str]:
    """Reads the project word list, lower-cased; blank lines and comments are ignored."""
    if not path.is_file():
        return set()
    return {line.strip().lower() for line in path.read_text(encoding="utf-8").splitlines()
            if line.strip() and not line.startswith("#")}


def prose_lines(document: str) -> list[tuple[int, str]]:
    """Returns the numbered lines outside fenced code blocks, with code, links and markup removed."""
    lines: list[tuple[int, str]] = []
    in_fence = False
    for number, line in enumerate(document.splitlines(), 1):
        if FENCE.match(line):
            in_fence = not in_fence
            continue
        if not in_fence:
            lines.append((number, line))
    text = "\n".join(line for _, line in lines)
    for pattern in REMOVED:
        text = pattern.sub(lambda match: re.sub(r"[^\n]", " ", match.group(0)), text)
    return [(number, cleaned) for (number, _), cleaned in zip(lines, text.split("\n"))]


def words_of(line: str) -> list[str]:
    """Splits a line into words: hyphenated words are checked part by part, and possessives lose their 's."""
    words = []
    for word in WORD.findall(line):
        word = word.replace("’", "'")
        if word.lower().endswith("'s"):
            word = word[:-2]
        words.append(word)
    return [word for word in words if len(word) > 1 or word.isupper()]


class Checker:
    def __init__(self, project_words: set[str]) -> None:
        from spellchecker import SpellChecker

        self.dictionary = SpellChecker()
        self.project_words = project_words

    def problem(self, word: str) -> str | None:
        lower = word.lower()
        if lower in BRITISH:
            return f"{word}: use the US spelling '{BRITISH[lower]}'"
        if lower in self.project_words or lower in self.dictionary:
            return None
        # Contractions (doesn't, isn't) and plurals of listed words are fine when their base is known.
        if "'" in lower and lower.split("'")[0] in self.dictionary:
            return None
        if lower.endswith("s") and (lower[:-1] in self.project_words or lower[:-2] in self.project_words):
            return None
        return f"{word}: not a US English word or a listed project term"

    def check(self, path: Path, root: Path) -> list[str]:
        shown = path.resolve().relative_to(root.resolve()).as_posix() if path.resolve().is_relative_to(root.resolve()) else path.as_posix()
        findings = []
        for number, line in prose_lines(path.read_text(encoding="utf-8")):
            for word in words_of(line):
                problem = self.problem(word)
                if problem:
                    findings.append(f"{shown}:{number}: {problem}")
        return findings


def documents(root: Path) -> list[Path]:
    found = [root / name for name in ROOT_DOCUMENTS if (root / name).is_file()]
    return found + sorted((root / "docs").rglob("*.md"))


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("files", type=Path, nargs="*", help="Markdown files to check (default: the documentation)")
    parser.add_argument("--root", type=Path, default=Path.cwd())
    args = parser.parse_args()
    checker = Checker(load_words(args.root / WORD_LIST))
    findings = [finding for path in (args.files or documents(args.root)) for finding in checker.check(path, args.root)]
    for finding in findings:
        print(f"ERROR: {finding}")
    if findings:
        print(f"{len(findings)} spelling problems. Fix the word, or add a genuine project term to {WORD_LIST.as_posix()}.")
        return 1
    print("Spelling passed")
    return 0


if __name__ == "__main__":
    sys.exit(main())
