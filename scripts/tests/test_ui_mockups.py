"""Tests for the UI mockup rules (ADR-0017)."""

from __future__ import annotations

import sys
import tempfile
import unittest
from pathlib import Path
from unittest import mock

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
import project_hierarchy  # noqa: E402
from penpot_render import render  # noqa: E402
from penpot_scene import Board, Rect, write_penpot  # noqa: E402
from project_hierarchy import Guard, Issue, Node  # noqa: E402
from ui_mockups import (  # noqa: E402
    NEEDS_MOCKUP, UI_LABEL, folder_problems, is_ui_file, mockup_problem, mockup_references, placement_problems,
    pull_request_problems, ui_requested,
)

PENPOT = "https://design.penpot.app/#/view/0f1e?page-id=2a&section=interactions"


class ReferenceTests(unittest.TestCase):
    def test_finds_repository_mockups_as_links_or_images(self) -> None:
        text = "See ![Review](docs/mockups/character-review.svg) and [list](../docs/mockups/raids/list.svg)."
        self.assertEqual(mockup_references(text), ["docs/mockups/character-review.svg", "docs/mockups/raids/list.svg"])

    def test_finds_penpot_share_links(self) -> None:
        self.assertEqual(mockup_references(f"Design: {PENPOT}"), [PENPOT])

    def test_other_images_are_not_mockups(self) -> None:
        self.assertEqual(mockup_references("![diagram](docs/diagrams/context.svg) https://figma.com/file/x"), [])

    def test_reads_the_issue_form_answer(self) -> None:
        self.assertTrue(ui_requested("### User interface\n\nYes, it changes what users see\n\n### Mockup\n"))
        self.assertFalse(ui_requested("### User interface\n\nNo, nothing users see changes\n"))
        self.assertFalse(ui_requested("No form here."))


class IssueRuleTests(unittest.TestCase):
    """An issue shows a mockup when it names a committed SVG or links a Penpot share link."""

    def setUp(self) -> None:
        scratch = tempfile.TemporaryDirectory()
        self.addCleanup(scratch.cleanup)
        self.root = Path(scratch.name)
        (self.root / "docs/mockups").mkdir(parents=True)
        (self.root / "docs/mockups/character-review.svg").write_text("<svg/>", encoding="utf-8")

    def problem(self, body: str, labels: frozenset[str] = frozenset({UI_LABEL})) -> str | None:
        return mockup_problem(labels, body, self.root)

    def test_ui_item_without_a_mockup_fails(self) -> None:
        self.assertIn("must link or show its mockup", self.problem("No design yet.") or "")

    def test_committed_mockup_passes_as_image_link_or_blob_url(self) -> None:
        for body in ("![Review](docs/mockups/character-review.svg)",
                     "[Review](../docs/mockups/character-review.svg)",
                     "https://github.com/owner/repo/blob/main/docs/mockups/character-review.svg"):
            with self.subTest(body=body):
                self.assertIsNone(self.problem(body))

    def test_penpot_link_passes_while_the_design_is_in_progress(self) -> None:
        self.assertIsNone(self.problem(PENPOT))

    def test_naming_a_mockup_that_does_not_exist_fails(self) -> None:
        problem = self.problem("Will produce docs/mockups/raid-list.penpot and docs/mockups/raid-list.svg.") or ""
        self.assertIn("names `docs/mockups/raid-list.svg`, but that mockup isn't in the repository yet", problem)

    def test_one_existing_mockup_among_missing_ones_passes(self) -> None:
        self.assertIsNone(self.problem("docs/mockups/raid-list.svg and docs/mockups/character-review.svg"))

    def test_items_without_the_ui_label_are_not_checked(self) -> None:
        self.assertIsNone(self.problem("", frozenset({"type:task"})))


class PullRequestRuleTests(unittest.TestCase):
    def setUp(self) -> None:
        scratch = tempfile.TemporaryDirectory()
        self.addCleanup(scratch.cleanup)
        self.root = Path(scratch.name)
        (self.root / "docs/mockups").mkdir(parents=True)
        (self.root / "docs/mockups/character-review.svg").write_text("<svg/>", encoding="utf-8")

    def test_ui_files_are_website_addon_markup_and_styles(self) -> None:
        for path in ("src/Containers/UI/Hosting/RaidManager.Web/Features/Home/Pages/Home.razor.cs",
                     "src/Containers/UI/RaidManager.ViewModels/Features/Claims/ReviewViewModel.cs",
                     "addon/RaidManager/Core.lua", "src/Other/Page.razor"):
            with self.subTest(path=path):
                self.assertTrue(is_ui_file(path))
        for path in ("src/Core/RaidManager.Domain/Raid.cs", "test/Containers/UI/Web.Tests/HomeTests.cs",
                     "docs/mockups/character-review.svg"):
            with self.subTest(path=path):
                self.assertFalse(is_ui_file(path))

    def test_change_without_ui_files_passes(self) -> None:
        self.assertEqual(pull_request_problems("", ["src/Core/RaidManager.Domain/Raid.cs"], self.root), [])

    def test_ui_change_without_a_mockup_fails(self) -> None:
        problems = pull_request_problems("Closes #1", ["src/Containers/UI/Web/Pages/Review.razor"], self.root)
        self.assertIn("changes user-interface files (`src/Containers/UI/Web/Pages/Review.razor`)", problems[0])

    def test_ui_change_showing_a_committed_mockup_passes(self) -> None:
        body = "![Review](docs/mockups/character-review.svg)"
        self.assertEqual(pull_request_problems(body, ["src/Containers/UI/Web/Pages/Review.razor"], self.root), [])

    def test_mockup_the_pull_request_adds_counts(self) -> None:
        changed = ["src/Containers/UI/a.razor", "docs/mockups/raid-list.svg", "docs/mockups/raid-list.penpot"]
        self.assertEqual(pull_request_problems("![x](docs/mockups/raid-list.svg)", changed, self.root), [])

    def test_mockup_missing_from_the_repository_fails(self) -> None:
        problems = pull_request_problems("![x](docs/mockups/raid-list.svg)", ["src/Containers/UI/a.razor"], self.root)
        self.assertEqual(problems, ["The mockup `docs/mockups/raid-list.svg` isn't in the repository; commit it with its "
                                    "`.penpot` source."])

    def test_penpot_link_passes(self) -> None:
        self.assertEqual(pull_request_problems(PENPOT, ["src/Containers/UI/a.razor"], self.root), [])

    def test_no_visual_change_needs_a_reason(self) -> None:
        change = ["src/Containers/UI/Web/Pages/Review.razor.cs"]
        self.assertEqual(pull_request_problems("No visual change: renames a view-model field only.", change, self.root), [])
        self.assertTrue(pull_request_problems("No visual change: n/a", change, self.root))

    def test_long_file_lists_are_shortened(self) -> None:
        changed = [f"src/Containers/UI/page{number}.razor" for number in range(8)]
        self.assertIn("and 3 more", pull_request_problems("", changed, self.root)[0])


class FolderTests(unittest.TestCase):
    def setUp(self) -> None:
        scratch = tempfile.TemporaryDirectory()
        self.addCleanup(scratch.cleanup)
        self.root = Path(scratch.name)
        self.folder = self.root / "docs/mockups"
        self.folder.mkdir(parents=True)

    def pair(self, name: str = "review") -> Path:
        penpot = self.folder / f"{name}.penpot"
        write_penpot(penpot, name, "Page", [Board("Board", 0, 0, 100, 60, "#0B111C", [Rect("Box", 10, 10, 20, 20, "#5DE0C1")])])
        penpot.with_suffix(".svg").write_text(render(penpot)[0], encoding="utf-8", newline=chr(10))
        return penpot

    def test_missing_folder_passes(self) -> None:
        self.assertEqual(folder_problems(Path(tempfile.gettempdir()) / "no-such-repository-root"), [])

    def test_source_with_its_current_rendering_passes(self) -> None:
        self.pair()
        self.assertEqual(folder_problems(self.root), [])

    def test_export_without_source_and_source_without_export_fail(self) -> None:
        (self.folder / "raids.svg").write_text("<svg/>", encoding="utf-8")
        self.pair().with_suffix(".svg").unlink()
        self.assertEqual(sorted(folder_problems(self.root)), [
            "docs/mockups/raids.svg: missing its Penpot source raids.penpot",
            "docs/mockups/review.penpot: missing its SVG; run python scripts/penpot_render.py docs/mockups/review.penpot",
        ])

    def test_stale_rendering_fails(self) -> None:
        self.pair().with_suffix(".svg").write_text("<svg>old</svg>", encoding="utf-8")
        self.assertIn("isn't the current rendering", folder_problems(self.root)[0])

    def test_other_files_fail(self) -> None:
        (self.folder / "notes.md").write_text("# Notes", encoding="utf-8")
        self.assertIn("only .penpot sources", folder_problems(self.root)[0])

    def test_mockups_outside_docs_mockups_fail(self) -> None:
        (self.root / "character-review.penpot").write_bytes(b"PK")
        (self.root / "character-review.svg").write_text("<svg/>", encoding="utf-8")
        self.assertEqual(sorted(placement_problems(self.root)), [
            "character-review.penpot: Penpot files live only in docs/mockups/",
            "character-review.svg: SVG files live only in docs/ (mockups and diagrams) or a web project's wwwroot/",
        ])

    def test_diagrams_web_assets_and_build_output_are_allowed(self) -> None:
        for path in ("docs/diagrams/context.svg", "src/Web/wwwroot/logo.svg", "src/Web/bin/Release/icon.svg"):
            (self.root / path).parent.mkdir(parents=True, exist_ok=True)
            (self.root / path).write_text("<svg/>", encoding="utf-8")
        self.assertEqual(placement_problems(self.root), [])


class GuardMockupTests(unittest.TestCase):
    def setUp(self) -> None:
        patcher = mock.patch.object(project_hierarchy, "gh", return_value="")
        self.gh = patcher.start()
        self.addCleanup(patcher.stop)
        scratch = tempfile.TemporaryDirectory()
        self.addCleanup(scratch.cleanup)
        self.root = Path(scratch.name)
        (self.root / "docs/mockups").mkdir(parents=True)
        (self.root / "docs/mockups/character-review.svg").write_text("<svg/>", encoding="utf-8")
        self.guard = Guard("owner/repo", root=self.root)

    def calls(self) -> list[tuple[str, ...]]:
        return [call.args for call in self.gh.call_args_list]

    def test_ui_item_without_a_mockup_is_flagged_once(self) -> None:
        self.guard.check_mockup(Node(Issue(18, "open", frozenset({"type:story", UI_LABEL}))))
        self.assertEqual(self.calls()[0], ("issue", "edit", "18", "--repo", "owner/repo", "--add-label", NEEDS_MOCKUP))
        self.assertIn("Mockup rule (ADR-0017)", self.calls()[1][-1])

    def test_form_answer_adds_the_ui_label_then_flags(self) -> None:
        body = "### User interface\n\nYes, it changes what users see\n"
        self.guard.check_mockup(Node(Issue(170, "open", frozenset({"type:story"}), body=body)))
        self.assertEqual(self.calls()[0][-1], UI_LABEL)
        self.assertEqual(self.calls()[1][-1], NEEDS_MOCKUP)

    def test_mockup_added_later_clears_the_label(self) -> None:
        issue = Issue(18, "open", frozenset({"type:story", UI_LABEL, NEEDS_MOCKUP}),
                      body="![Review](docs/mockups/character-review.svg)")
        self.guard.check_mockup(Node(issue))
        self.assertEqual(self.calls(), [("issue", "edit", "18", "--repo", "owner/repo", "--remove-label", NEEDS_MOCKUP)])

    def test_named_but_missing_mockup_keeps_the_label(self) -> None:
        issue = Issue(167, "open", frozenset({"type:task", UI_LABEL, NEEDS_MOCKUP}),
                      body="Commits docs/mockups/raid-list.penpot and docs/mockups/raid-list.svg.")
        self.guard.check_mockup(Node(issue))
        self.assertEqual(self.calls(), [])

    def test_abandoned_ui_item_is_not_flagged(self) -> None:
        self.guard.check_mockup(Node(Issue(18, "closed", frozenset({"type:story", UI_LABEL}), "not_planned")))
        self.assertEqual(self.calls(), [])


if __name__ == "__main__":
    unittest.main()
