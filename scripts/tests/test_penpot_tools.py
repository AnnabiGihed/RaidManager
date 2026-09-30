"""Tests for the Penpot mockup generator and renderer (ADR-0018)."""

from __future__ import annotations

import json
import sys
import tempfile
import unittest
import zipfile
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from penpot_render import is_current, render  # noqa: E402
from penpot_scene import (  # noqa: E402
    FILE_VERSION, MIGRATIONS, ROOT_ID, Board, Circle, Click, Group, Rect, contrast_ratio, text, write_mockup,
    write_penpot,
)


def scene() -> list[Board]:
    return [
        Board("1 · Review", 0, 0, 400, 300, "#F5F5F5", [
            Rect("Card", 20, 20, 360, 200, "#FFFFFF", 1, 4, "#E0E0E0"),
            Group("Approve button", [
                Rect("Background", 40, 160, 96, 36, "#4340D2", 1, 4),
                text("Label", 40, 183, "APPROVE", 13, 500, "#FFFFFF", 96, "center", 0.5),
            ]),
            Circle("Status", 60, 60, 8, "#FF9800"),
            text("Title", 40, 110, "Review & approve <characters>", 24),
        ]),
        Board("2 · Empty", 480, 0, 400, 300, "#F5F5F5", [text("Message", 20, 40, "Nothing to review.")]),
    ]


def approve_button(boards: list[Board]) -> Group:
    button = boards[0].children[1]
    assert isinstance(button, Group)
    return button


class PenpotFileTests(unittest.TestCase):
    def setUp(self) -> None:
        scratch = tempfile.TemporaryDirectory()
        self.addCleanup(scratch.cleanup)
        self.folder = Path(scratch.name)
        self.path = self.folder / "review.penpot"
        write_penpot(self.path, "review", "Review", scene())

    def entries(self) -> dict[str, dict]:
        with zipfile.ZipFile(self.path) as archive:
            return {name: json.loads(archive.read(name)) for name in archive.namelist()}

    def shapes(self) -> dict[str, dict]:
        return {shape["name"] + ":" + shape["type"]: shape for name, shape in self.entries().items() if name.count("/") == 4}

    def test_zip_follows_the_binfile_v3_layout(self) -> None:
        entries = self.entries()
        manifest = entries["manifest.json"]
        file_id = manifest["files"][0]["id"]
        self.assertEqual((manifest["type"], manifest["version"]), ("penpot/export-files", 1))
        file = entries[f"files/{file_id}.json"]
        self.assertEqual((file["version"], len(file["migrations"])), (FILE_VERSION, len(MIGRATIONS)))
        pages = [name for name in entries if name.count("/") == 3 and "/pages/" in name]
        self.assertEqual(len(pages), 1)

    def test_texts_are_editable_text_layers_with_their_content(self) -> None:
        title = self.shapes()["Title:text"]
        paragraph = title["content"]["children"][0]["children"][0]
        self.assertEqual(paragraph["children"][0]["text"], "Review & approve <characters>")
        self.assertEqual((paragraph["fontFamily"], paragraph["fontSize"]), ("Roboto", "24"))
        self.assertEqual(title["growType"], "auto-width")

    def test_layers_are_linked_to_their_parents_and_boards(self) -> None:
        shapes = self.shapes()
        board, group, label = shapes["1 · Review:frame"], shapes["Approve button:group"], shapes["Label:text"]
        self.assertEqual(shapes["Root Frame:frame"]["id"], ROOT_ID)
        self.assertIn(board["id"], shapes["Root Frame:frame"]["shapes"])
        self.assertEqual((group["parentId"], group["frameId"]), (board["id"], board["id"]))
        self.assertEqual((label["parentId"], label["frameId"]), (group["id"], board["id"]))
        self.assertEqual((group["x"], group["y"], group["width"], group["height"]), (40, 160, 96, 36))

    def test_regenerating_an_unchanged_screen_gives_the_same_file(self) -> None:
        again = self.folder / "again.penpot"
        write_penpot(again, "review", "Review", scene())
        self.assertEqual(self.path.read_bytes(), again.read_bytes())

    def test_write_mockup_puts_the_file_and_its_rendering_in_docs_mockups(self) -> None:
        penpot = write_mockup(self.folder, "character-review", "Character review", scene())
        self.assertEqual(penpot, self.folder / "docs/mockups/character-review.penpot")
        self.assertTrue(is_current(penpot))

    def test_palette_colours_and_text_styles_become_a_shared_library(self) -> None:
        entries = self.entries()
        colours = {entry["path"] + "/" + entry["name"]: entry for name, entry in entries.items() if "/colors/" in name}
        typographies = {entry["path"] + "/" + entry["name"] for name, entry in entries.items() if "/typographies/" in name}
        self.assertEqual(colours["Brand/primary"]["color"], "#4340d2")
        self.assertNotIn("Status/danger", colours, "unused palette colours stay out of the library")
        self.assertEqual(typographies, {"Label/Button", "Heading/Page title", "Body/Default"})
        background = self.shapes()["Background:rect"]["fills"][0]
        self.assertEqual(background["fillColorRefId"], colours["Brand/primary"]["id"])
        leaf = self.shapes()["Title:text"]["content"]["children"][0]["children"][0]["children"][0]
        typography = next(entry for name, entry in entries.items() if "/typographies/" in name and entry["name"] == "Page title")
        self.assertEqual(leaf["typographyRefId"], typography["id"])

    def test_groups_link_to_boards_and_flows_start_the_prototype(self) -> None:
        boards = scene()
        approve_button(boards).on_click = Click("navigate", "2 · Empty")
        boards[1].children.append(Group("Back button", [Rect("Area", 0, 0, 40, 40, "#FFFFFF")], Click("prev-screen")))
        write_penpot(self.path, "review", "Review", boards, flows={"Review a character": "1 · Review"})
        shapes = self.shapes()
        empty_board = shapes["2 · Empty:frame"]["id"]
        self.assertEqual(shapes["Approve button:group"]["interactions"],
                         [{"eventType": "click", "actionType": "navigate", "destination": empty_board, "preserveScroll": False}])
        self.assertEqual(shapes["Back button:group"]["interactions"], [{"eventType": "click", "actionType": "prev-screen"}])
        page = next(entry for name, entry in self.entries().items() if name.count("/") == 3 and "/pages/" in name)
        flow = next(iter(page["flows"].values()))
        self.assertEqual((flow["name"], flow["startingFrame"]), ("Review a character", shapes["1 · Review:frame"]["id"]))

    def test_links_to_missing_boards_fail(self) -> None:
        boards = scene()
        approve_button(boards).on_click = Click("open-overlay", "3 · Missing")
        with self.assertRaisesRegex(ValueError, "no board named '3 · Missing'"):
            write_penpot(self.path, "review", "Review", boards)
        with self.assertRaisesRegex(ValueError, "Flow 'Start' starts on a board that doesn't exist"):
            write_penpot(self.path, "review", "Review", scene(), flows={"Start": "Nowhere"})

    def test_low_contrast_text_stops_the_generation(self) -> None:
        board = Board("Board", 0, 0, 300, 100, "#FFFFFF", [
            Rect("Badge", 10, 10, 120, 30, "#FFF0DB"),
            text("Label", 14, 30, "Pending", 12, 500, "#FF9800"),
        ])
        with self.assertRaisesRegex(ValueError, r"'Label' \('Pending'\) has contrast 1\.92:1"):
            write_penpot(self.path, "bad", "Page", [board])

    def test_unnamed_colours_stop_the_generation(self) -> None:
        board = Board("Board", 0, 0, 300, 100, "#FFFFFF", [Rect("Badge", 10, 10, 120, 30, "#123456")])
        with self.assertRaisesRegex(ValueError, "'Badge' uses #123456, which the palette doesn't name"):
            write_penpot(self.path, "bad", "Page", [board])
        write_penpot(self.path, "ok", "Page", [board], palette={"Surface/card": "#FFFFFF", "Custom/badge": "#123456"})

    def test_a_colour_has_one_name_in_the_palette(self) -> None:
        with self.assertRaisesRegex(ValueError, "these have several: #ffffff"):
            write_penpot(self.path, "dup", "Page", scene(), palette={"A/white": "#FFFFFF", "B/white": "#ffffff"})

    def test_icons_need_graphic_contrast_only(self) -> None:
        board = Board("Board", 0, 0, 300, 100, "#FFFFFF", [
            Circle("Icon", 30, 30, 10, "#2196F3"),
            text("Icon mark", 24, 35, "i", 13, 700, "#FFFFFF", 12, "center", icon=True),
        ])
        write_penpot(self.path, "icons", "Page", [board])

    def test_contrast_ratio_matches_the_wcag_formula(self) -> None:
        self.assertAlmostEqual(contrast_ratio("#000000", "#FFFFFF"), 21, places=2)
        self.assertAlmostEqual(contrast_ratio("#4340D2", "#FFFFFF"), 7.23, places=2)

    def test_sibling_layers_need_distinct_names(self) -> None:
        board = Board("Board", 0, 0, 100, 100, "#FFFFFF", [Rect("Box", 0, 0, 1, 1, "#000000"), Rect("Box", 5, 5, 1, 1, "#000000")])
        with self.assertRaisesRegex(ValueError, "Two layers are named 'Box'"):
            write_penpot(self.folder / "bad.penpot", "bad", "Page", [board])


class RenderTests(unittest.TestCase):
    def setUp(self) -> None:
        scratch = tempfile.TemporaryDirectory()
        self.addCleanup(scratch.cleanup)
        self.path = Path(scratch.name) / "review.penpot"
        write_penpot(self.path, "review", "Review", scene())

    def rewrite(self, change) -> None:
        """Applies a change to every shape, the way an edit in Penpot would."""
        with zipfile.ZipFile(self.path) as archive:
            entries = {name: archive.read(name) for name in archive.namelist()}
        with zipfile.ZipFile(self.path, "w") as archive:
            for name, data in entries.items():
                if name.count("/") == 4:
                    data = json.dumps(change(json.loads(data))).encode()
                archive.writestr(name, data)

    def test_renders_boards_texts_and_labels(self) -> None:
        svg, warnings = render(self.path)
        self.assertEqual(warnings, [])
        self.assertIn(">1 · Review</text>", svg)
        self.assertIn(">Review &amp; approve &lt;characters&gt;</text>", svg)
        self.assertIn('text-anchor="middle" letter-spacing="0.5">APPROVE</text>', svg)
        self.assertIn('<ellipse cx="60" cy="60" rx="8" ry="8" fill="#ff9800"', svg)

    def test_inner_strokes_are_drawn_inside_the_shape(self) -> None:
        self.assertIn('<rect x="20.5" y="20.5" width="359" height="199" rx="4" fill="#ffffff" stroke="#e0e0e0"', render(self.path)[0])

    def test_hidden_layers_are_not_drawn(self) -> None:
        self.rewrite(lambda shape: {**shape, "hidden": True} if shape["name"] == "Title" else shape)
        self.assertNotIn("Review &amp; approve", render(self.path)[0])

    def test_unsupported_shapes_become_placeholders_with_a_warning(self) -> None:
        self.rewrite(lambda shape: {**shape, "type": "path"} if shape["name"] == "Status" else shape)
        svg, warnings = render(self.path)
        self.assertEqual(warnings, ["Status (path) is drawn as a placeholder"])
        self.assertIn('stroke-dasharray="4 4"', svg)

    def test_rotated_shapes_keep_their_centre(self) -> None:
        def rotate(shape: dict) -> dict:
            if shape["name"] != "Card":
                return shape
            return {**shape, "transform": {"a": 0, "b": 1, "c": -1, "d": 0, "e": 0, "f": 0}}
        self.rewrite(rotate)
        self.assertIn('transform="matrix(0 1 -1 0 320 -80)"', render(self.path)[0])

    def test_is_current_compares_the_svg_with_a_fresh_rendering(self) -> None:
        svg_path = self.path.with_suffix(".svg")
        self.assertFalse(is_current(self.path))
        svg_path.write_text(render(self.path)[0], encoding="utf-8", newline="\n")
        self.assertTrue(is_current(self.path))
        self.rewrite(lambda shape: {**shape, "name": shape["name"] + " (edited)"} if shape["type"] == "frame" else shape)
        self.assertFalse(is_current(self.path))


if __name__ == "__main__":
    unittest.main()
