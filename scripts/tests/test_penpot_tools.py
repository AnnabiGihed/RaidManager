"""Tests for the Penpot mockup generator and renderer (ADR-0018)."""

from __future__ import annotations

import json
import sys
import tempfile
import unittest
import zipfile
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from penpot_components import (  # noqa: E402
    AVATAR_TONES, BADGE_TONES, BOARD_H, BOARD_W, BUTTON_STYLES, NOTICE_TONES, OFFICER_PAGES, PLAYER, PLAYER_PAGES,
    PUBLIC_W, PUBLIC_X, WINDOW_H, WINDOW_W, app_screen, companion_window, badge, button, notice, page_header, public_screen,
)
from penpot_render import is_current, render  # noqa: E402
from penpot_scene import (  # noqa: E402
    FILE_VERSION, MIGRATIONS, ROOT_ID, Board, Circle, Click, Group, Rect, contrast_ratio, text, write_mockup,
    write_penpot,
)


# A light palette for the contrast tests, which need colours the house palette avoids.
LIGHT = {"Surface/white": "#FFFFFF", "Status/warning background": "#FFF0DB", "Status/warning": "#FF9800",
         "Status/info": "#2196F3"}


def scene() -> list[Board]:
    return [
        Board("1 · Review", 0, 0, 400, 300, "#0B111C", [
            Rect("Card", 20, 20, 360, 200, "#172333", 1, 4, "#212F41"),
            Group("Approve button", [
                Rect("Background", 40, 160, 96, 36, "#5DE0C1", 1, 4),
                text("Label", 40, 183, "APPROVE", 12, 700, "#102830", 96, "center", 1.5),
            ]),
            Circle("Status", 60, 60, 8, "#F3BE75"),
            text("Title", 40, 110, "Review & approve <characters>", 32, 700),
        ]),
        Board("2 · Empty", 480, 0, 400, 300, "#0B111C", [text("Message", 20, 40, "Nothing to review.")]),
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
        self.assertEqual((paragraph["fontFamily"], paragraph["fontSize"]), ("Open Sans", "32"))
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
        self.assertEqual(colours["Brand/accent"]["color"], "#5de0c1")
        self.assertNotIn("Status/danger", colours, "unused palette colours stay out of the library")
        self.assertEqual(typographies, {"Label/Eyebrow", "Heading/Page title", "Body/Default"})
        background = self.shapes()["Background:rect"]["fills"][0]
        self.assertEqual(background["fillColorRefId"], colours["Brand/accent"]["id"])
        leaf = self.shapes()["Title:text"]["content"]["children"][0]["children"][0]["children"][0]
        typography = next(entry for name, entry in entries.items() if "/typographies/" in name and entry["name"] == "Page title")
        self.assertEqual(leaf["typographyRefId"], typography["id"])

    def test_groups_link_to_boards_and_flows_start_the_prototype(self) -> None:
        boards = scene()
        approve_button(boards).on_click = Click("navigate", "2 · Empty")
        boards[1].children.append(Group("Back button", [Rect("Area", 0, 0, 40, 40, "#172333")], Click("prev-screen")))
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
            write_penpot(self.path, "bad", "Page", [board], palette=LIGHT)

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
        write_penpot(self.path, "icons", "Page", [board], palette=LIGHT)

    def test_contrast_ratio_matches_the_wcag_formula(self) -> None:
        self.assertAlmostEqual(contrast_ratio("#000000", "#FFFFFF"), 21, places=2)
        self.assertAlmostEqual(contrast_ratio("#4340D2", "#FFFFFF"), 7.23, places=2)

    def test_sibling_layers_need_distinct_names(self) -> None:
        board = Board("Board", 0, 0, 100, 100, "#0B111C", [Rect("Box", 0, 0, 1, 1, "#000000"), Rect("Box", 5, 5, 1, 1, "#000000")])
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
        self.assertIn('text-anchor="middle" letter-spacing="1.5">APPROVE</text>', svg)
        self.assertIn('<ellipse cx="60" cy="60" rx="8" ry="8" fill="#f3be75"', svg)
        self.assertIn('font-family="Open Sans, Segoe UI, Arial, sans-serif"', svg)

    def test_inner_strokes_are_drawn_inside_the_shape(self) -> None:
        self.assertIn('<rect x="20.5" y="20.5" width="359" height="199" rx="4" fill="#172333" stroke="#212f41"', render(self.path)[0])

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


def layer_names(items: list) -> list[str]:
    names: list[str] = []
    for item in items:
        names.append(item.name)
        if isinstance(item, Group):
            names += layer_names(item.children)
    return names


def find(items: list, name: str) -> Group:
    for item in items:
        if isinstance(item, Group):
            if item.name == name:
                return item
            try:
                return find(item.children, name)
            except LookupError:
                pass
    raise LookupError(name)


class ComponentTests(unittest.TestCase):
    """The app shell and shared components (ADR-0019)."""

    def setUp(self) -> None:
        scratch = tempfile.TemporaryDirectory()
        self.addCleanup(scratch.cleanup)
        self.path = Path(scratch.name) / "shell.penpot"

    def test_an_officer_sees_both_navigation_sections(self) -> None:
        board = app_screen("1 · Overview", 0, 0, "Overview", [])
        self.assertEqual((board.w, board.h), (BOARD_W, BOARD_H))
        names = layer_names(board.children)
        for group in ("Sidebar", "Logo", "Community card", "Player navigation", "Officer navigation", "User card",
                      "Top bar", "Breadcrumb", "Realm status"):
            self.assertIn(group, names)
        self.assertTrue(all(f"{page} link" in names for page in PLAYER_PAGES + OFFICER_PAGES))

    def test_a_player_sees_only_the_player_section(self) -> None:
        names = layer_names(app_screen("1 · Characters", 0, 0, "My characters", [], user=PLAYER).children)
        self.assertNotIn("Officer navigation", names)
        self.assertNotIn("Schedule link", names)
        self.assertIn("Bryn Valewood", [item.text for item in find(app_screen("b", 0, 0, "Raids", [], user=PLAYER).children,
                                                                    "User card").children if hasattr(item, "text")])

    def test_the_active_page_is_marked_and_navigation_links_to_boards(self) -> None:
        board = app_screen("1 · Overview", 0, 0, "Raids", [], links={"Overview": "2 · Other"})
        active, other = find(board.children, "Raids link"), find(board.children, "Overview link")
        self.assertIn("Active mark", layer_names(active.children))
        self.assertNotIn("Active mark", layer_names(other.children))
        self.assertEqual(other.on_click, Click("navigate", "2 · Other"))
        self.assertIsNone(active.on_click)
        breadcrumb = [item.text for item in find(board.children, "Breadcrumb").children if hasattr(item, "text")]
        self.assertEqual(breadcrumb, ["CITADEL VANGUARD", "/", "RAIDS"])

    def test_every_component_passes_contrast_in_a_generated_file(self) -> None:
        content: list = [page_header("Eyebrow", "Title", "Subtitle")]
        for index, tone in enumerate(BADGE_TONES):
            content.append(badge(f"{tone} badge", 300 + index * 100, 300, tone.title(), tone))
        for index, style in enumerate(BUTTON_STYLES):
            content.append(button(f"{style} button", 300 + index * 140, 360, style.title(), style))
        for index, tone in enumerate(NOTICE_TONES):
            content.append(notice(f"{tone} notice", 300, 420 + index * 88, 600, "Title", "Body", tone))
        boards = [app_screen("1 · Officer", 0, 0, "Overview", content),
                  app_screen("2 · Player", BOARD_W + 80, 0, "Readiness", [], user=PLAYER, links={"Overview": "1 · Officer"})]
        write_penpot(self.path, "shell", "Shell", boards)
        self.assertEqual(render(self.path)[1], [])

    def test_tone_colours_pass_wcag_aa(self) -> None:
        for fill, colour in list(BADGE_TONES.values()) + list(AVATAR_TONES.values()):
            self.assertGreaterEqual(contrast_ratio(colour, fill), 4.5, (colour, fill))
        for fill, _, colour in BUTTON_STYLES.values():
            self.assertGreaterEqual(contrast_ratio(colour, fill), 4.5, (colour, fill))
        for background, _, title, body, icon, mark, _ in NOTICE_TONES.values():
            self.assertGreaterEqual(min(contrast_ratio(title, background), contrast_ratio(body, background)), 4.5)
            self.assertGreaterEqual(contrast_ratio(mark, icon), 3)

    def test_sign_out_is_in_the_top_bar_and_links_where_asked(self) -> None:
        linked = find(app_screen("1 · A", 0, 0, "Raids", [], sign_out="2 · Signed out").children, "Sign out button")
        self.assertEqual(linked.on_click, Click("navigate", "2 · Signed out"))
        self.assertIsNone(find(app_screen("1 · A", 0, 0, "Raids", []).children, "Sign out button").on_click)

    def test_a_user_without_a_community_sees_an_empty_community_card(self) -> None:
        board = app_screen("1 · New", 0, 0, "Overview", [], community=None, user=PLAYER)
        card = find(board.children, "Community card")
        self.assertIn("No community yet", [item.text for item in card.children if hasattr(item, "text")])
        breadcrumb = [item.text for item in find(board.children, "Breadcrumb").children if hasattr(item, "text")]
        self.assertEqual(breadcrumb[0], "RAIDMANAGER")
        write_penpot(self.path, "new", "New", [board])

    def test_the_community_card_links_where_asked(self) -> None:
        board = app_screen("1 · A", 0, 0, "Raids", [], links={"Community": "2 · Settings"})
        self.assertEqual(find(board.children, "Community card").on_click, Click("navigate", "2 · Settings"))

    def test_companion_windows_have_a_title_bar_but_no_website_shell(self) -> None:
        board = companion_window("1 · Waiting", 0, 0, [text("Code", 32, 120, "K7M-4QX", 32, 700)])
        self.assertEqual((board.w, board.h), (WINDOW_W, WINDOW_H))
        names = layer_names(board.children)
        self.assertIn("Title bar", names)
        self.assertNotIn("Sidebar", names)
        write_penpot(self.path, "window", "Window", [board])

    def test_signed_out_pages_have_the_logo_but_no_shell(self) -> None:
        board = public_screen("1 · Signed out", 0, 0, [text("Title", PUBLIC_X, 240, "Sign in", 18, 700)])
        names = layer_names(board.children)
        self.assertIn("Logo", names)
        self.assertNotIn("Sidebar", names)
        self.assertNotIn("Top bar", names)
        self.assertEqual(PUBLIC_X * 2 + PUBLIC_W, BOARD_W)
        write_penpot(self.path, "public", "Public", [board])

    def test_buttons_fit_their_label_unless_given_a_width(self) -> None:
        def width(group: Group) -> float:
            background = group.children[0]
            assert isinstance(background, Rect)
            return background.w

        self.assertGreater(width(button("Wide", 0, 0, "Approve all characters")), width(button("Short", 0, 0, "OK")))
        self.assertEqual(width(button("Fixed", 0, 0, "OK", width=120)), 120)


if __name__ == "__main__":
    unittest.main()
