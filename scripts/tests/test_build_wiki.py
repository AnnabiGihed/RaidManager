"""Tests for the generated GitHub Wiki."""

from __future__ import annotations

import sys
import tempfile
import unittest
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from build_wiki import build, page_name, read_nav, write  # noqa: E402

WIKI = "https://github.com/owner/repo/wiki"
MKDOCS = """site_name: Test
nav:
  - Home: index.md
  - Guide: explanation/guide.md
  - Decisions:
      - First: adr/0001-first.md
theme:
  name: material
markdown_extensions:
  - pymdownx.superfences:
      custom_fences:
        - format: !!python/name:pymdownx.superfences.fence_code_format
"""


def built(root: Path):
    """Builds the wiki of the fixture repository in root."""
    return build(root, "owner/repo", "0123456789abcdef")


class WikiTestCase(unittest.TestCase):
    def setUp(self) -> None:
        self.scratch = tempfile.TemporaryDirectory()
        self.root = Path(self.scratch.name)
        self.addCleanup(self.scratch.cleanup)
        self.file("mkdocs.yml", MKDOCS)
        self.file("docs/index.md", "# Project docs\n\nRead the [guide](explanation/guide.md#setup).\n")
        self.file("docs/explanation/guide.md", "\n".join([
            "# Guide: getting started",
            "",
            "## Setup",
            "",
            "See [ADR-0001](../adr/0001-first.md), the [workflow](../../.github/workflows/ci.yml),",
            "and [GitHub](https://github.com). ![Context](../diagrams/context.svg)",
            "",
            "```markdown",
            "[kept](not-rewritten.md)",
            "```",
            "",
            "[home]: ../index.md",
        ]) + "\n")
        self.file("docs/adr/0001-first.md", "# ADR-0001: First decision\n\nBack to [the guide](../explanation/guide.md).\n")
        self.file("docs/diagrams/context.svg", "<svg/>")

    def file(self, path: str, content: str) -> None:
        target = self.root / path
        target.parent.mkdir(parents=True, exist_ok=True)
        target.write_text(content, encoding="utf-8")


class PageNameTests(unittest.TestCase):
    def test_index_is_home(self) -> None:
        self.assertEqual(page_name("index.md", "Anything"), "Home")

    def test_punctuation_becomes_single_hyphens(self) -> None:
        self.assertEqual(page_name("adr/0011-x.md", "ADR-0011: Website session and API trust"),
                         "ADR-0011-Website-session-and-API-trust")


class NavTests(unittest.TestCase):
    def test_reads_pages_and_sections_and_stops_at_the_next_key(self) -> None:
        nav = read_nav(MKDOCS)
        self.assertEqual([(item.depth, item.title, item.source) for item in nav], [
            (0, "Home", "index.md"), (0, "Guide", "explanation/guide.md"), (0, "Decisions", None),
            (1, "First", "adr/0001-first.md"),
        ])


class BuildTests(WikiTestCase):
    def test_valid_docs_build_without_errors(self) -> None:
        wiki = built(self.root)
        self.assertEqual(wiki.errors, [])
        self.assertEqual(set(wiki.pages), {"Home.md", "Guide-getting-started.md", "ADR-0001-First-decision.md",
                                           "_Sidebar.md", "_Footer.md"})

    def test_links_point_to_wiki_pages_with_anchors(self) -> None:
        home = built(self.root).pages["Home.md"]
        self.assertIn(f"[guide]({WIKI}/Guide-getting-started#setup)", home)

    def test_links_outside_docs_point_to_the_repository(self) -> None:
        guide = built(self.root).pages["Guide-getting-started.md"]
        self.assertIn("(https://github.com/owner/repo/blob/main/.github/workflows/ci.yml)", guide)
        self.assertIn("(https://github.com)", guide)

    def test_images_are_copied_and_linked_from_the_wiki(self) -> None:
        wiki = built(self.root)
        self.assertIn(f"![Context]({WIKI}/diagrams/context.svg)", wiki.pages["Guide-getting-started.md"])
        self.assertEqual(wiki.assets, {"diagrams/context.svg"})
        out = self.root / "out"
        write(wiki, self.root / "docs", out)
        self.assertTrue((out / "diagrams" / "context.svg").is_file())
        self.assertTrue((out / "_Sidebar.md").is_file())

    def test_reference_links_to_home_are_rewritten(self) -> None:
        self.assertIn(f"[home]: {WIKI}\n", built(self.root).pages["Guide-getting-started.md"])

    def test_code_blocks_are_left_alone(self) -> None:
        self.assertIn("[kept](not-rewritten.md)", built(self.root).pages["Guide-getting-started.md"])

    def test_title_is_dropped_and_the_source_is_named(self) -> None:
        guide = built(self.root).pages["Guide-getting-started.md"]
        self.assertTrue(guide.startswith("> Generated from [`docs/explanation/guide.md`]"))
        self.assertNotIn("# Guide: getting started", guide)
        self.assertIn("## Setup", guide)

    def test_sidebar_follows_the_navigation(self) -> None:
        sidebar = built(self.root).pages["_Sidebar.md"]
        self.assertIn("- [Guide](Guide-getting-started)\n- **Decisions**\n  - [First](ADR-0001-First-decision)", sidebar)

    def test_footer_names_the_commit(self) -> None:
        self.assertIn("[`0123456`](https://github.com/owner/repo/commit/0123456789abcdef)",
                      built(self.root).pages["_Footer.md"])


class BuildErrorTests(WikiTestCase):
    def test_link_to_a_missing_file_fails(self) -> None:
        self.file("docs/adr/0001-first.md", "# ADR-0001: First decision\n\nSee [gone](missing.md).\n")
        self.assertIn("docs/adr/0001-first.md: link to missing file missing.md", built(self.root).errors)

    def test_document_missing_from_the_navigation_fails(self) -> None:
        self.file("docs/adr/0002-second.md", "# ADR-0002: Second decision\n")
        self.assertIn("docs/adr/0002-second.md: missing from the mkdocs.yml nav", built(self.root).errors)

    def test_navigation_entry_without_a_file_fails(self) -> None:
        (self.root / "docs/adr/0001-first.md").unlink()
        self.assertIn("mkdocs.yml: nav lists missing file adr/0001-first.md", built(self.root).errors)

    def test_two_documents_with_the_same_page_name_fail(self) -> None:
        self.file("docs/adr/0001-first.md", "# Guide: getting started\n")
        errors = built(self.root).errors
        self.assertTrue(any("both become the wiki page Guide-getting-started" in error for error in errors), errors)

    def test_document_without_a_title_fails(self) -> None:
        self.file("docs/adr/0001-first.md", "No title here.\n")
        self.assertIn("docs/adr/0001-first.md: the first line must be the page's # title", built(self.root).errors)

    def test_failed_build_writes_no_sidebar(self) -> None:
        self.file("docs/adr/0001-first.md", "# ADR-0001: First decision\n\nSee [gone](missing.md).\n")
        self.assertNotIn("_Sidebar.md", built(self.root).pages)


if __name__ == "__main__":
    unittest.main()
