"""Tests for the strict documentation spell check."""

from __future__ import annotations

import sys
import tempfile
import unittest
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from spell_check import WORD_LIST, Checker, load_words, prose_lines, words_of  # noqa: E402


class ProseTests(unittest.TestCase):
    def test_code_links_urls_and_comments_are_not_prose(self) -> None:
        document = "\n".join([
            "Use `dotnet-ef migrations` and [the guide](https://example.test/xyzzy) or <https://qwzx.test>.",
            "<!-- qwzxcomment --> See https://example.test/plugh and mail owner@example.test.",
            "```bash",
            "frobnicate --qwzx",
            "```",
            "[ref]: https://example.test/zork",
        ])
        words = [word for _, line in prose_lines(document) for word in words_of(line)]
        self.assertEqual(words, ["Use", "and", "the", "guide", "or", "See", "and", "mail"])

    def test_line_numbers_survive_removed_blocks(self) -> None:
        lines = prose_lines("First\n```\ncode\n```\nFifth line")
        self.assertEqual([number for number, _ in lines], [1, 5])

    def test_hyphenated_words_and_possessives_split(self) -> None:
        self.assertEqual(words_of("Penpot's read-only file isn't here"), ["Penpot", "read", "only", "file", "isn't", "here"])


class CheckerTests(unittest.TestCase):
    def setUp(self) -> None:
        self.checker = Checker({"penpot", "workflow", "adr"})

    def test_dictionary_and_project_words_pass_in_any_case(self) -> None:
        for word in ("color", "Penpot", "penpot", "Workflow", "ADR"):
            with self.subTest(word=word):
                self.assertIsNone(self.checker.problem(word))

    def test_plurals_of_project_words_pass(self) -> None:
        self.assertIsNone(self.checker.problem("workflows"))
        self.assertIsNone(self.checker.problem("ADRs"))

    def test_contractions_pass(self) -> None:
        self.assertIsNone(self.checker.problem("doesn't"))

    def test_british_spellings_fail_with_the_us_form(self) -> None:
        self.assertEqual(self.checker.problem("colours"), "colours: use the US spelling 'colors'")
        self.assertEqual(self.checker.problem("labelled"), "labelled: use the US spelling 'labeled'")
        self.assertEqual(self.checker.problem("Behaviour"), "Behaviour: use the US spelling 'behavior'")

    def test_unknown_words_fail(self) -> None:
        self.assertEqual(self.checker.problem("recieve"), "recieve: not a US English word or a listed project term")

    def test_findings_name_the_file_and_line(self) -> None:
        with tempfile.TemporaryDirectory() as scratch:
            root = Path(scratch)
            (root / "docs").mkdir()
            (root / "docs/page.md").write_text("# Page\n\nThe colours recieve `qwzx` values.\n", encoding="utf-8")
            findings = self.checker.check(root / "docs/page.md", root)
        self.assertEqual(findings, [
            "docs/page.md:3: colours: use the US spelling 'colors'",
            "docs/page.md:3: recieve: not a US English word or a listed project term",
        ])


class WordListTests(unittest.TestCase):
    def test_reads_words_and_skips_comments_and_blanks(self) -> None:
        with tempfile.TemporaryDirectory() as scratch:
            path = Path(scratch) / "accept.txt"
            path.write_text("# comment\nPenpot\n\nGitHub\n", encoding="utf-8")
            self.assertEqual(load_words(path), {"penpot", "github"})

    def test_the_repository_word_list_is_sorted_and_unique(self) -> None:
        path = Path(__file__).resolve().parents[2] / WORD_LIST
        words = [line.strip() for line in path.read_text(encoding="utf-8").splitlines() if line.strip()]
        self.assertEqual(words, sorted(words, key=str.lower))
        self.assertEqual(len({word.lower() for word in words}), len(words))


if __name__ == "__main__":
    unittest.main()
