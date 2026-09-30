"""Write native Penpot mockups (`.penpot` files) from a small scene model (ADR-0018).

A screen script under `scripts/mockups/` describes boards made of rectangles, circles, text and named groups;
`write_penpot` turns them into a Penpot binfile that imports with every text as an editable text layer. The file also
carries a shared library: every colour of the palette becomes a named library colour and every text style a named
typography, and layers reference them, so a designer restyles the whole screen from the Assets panel. Groups can link
to other boards on click, and named flows make the boards a clickable prototype in Penpot's View mode. Text that fails
WCAG AA contrast against the shape behind it stops the generation.

The format follows penpot/penpot 2.18.0 (`backend/src/app/binfile/v3.clj`): a zip holding `manifest.json`,
`files/<file>.json`, `files/<file>/pages/<page>.json` and one `files/<file>/pages/<page>/<shape>.json` per shape.
Keys are camelCase and geometry uses absolute page coordinates. Penpot lays the text out when the file opens.
"""

from __future__ import annotations

import json
import uuid
import zipfile
from dataclasses import dataclass, field
from pathlib import Path
from typing import Union

PENPOT_VERSION = "2.18.0"
ROOT_ID = "00000000-0000-0000-0000-000000000000"
NAMESPACE = uuid.UUID("5b2f9c6e-6a53-4c3c-9d59-0f6f0f4a9e17")
FILE_VERSION = 67
# Features a new Penpot 2.18.0 file persists: the default features without the frontend-only ones.
FEATURES = ["fdata/shape-data-type", "fdata/path-data", "layout/grid", "components/v2", "design-tokens/v1", "variants/v1"]
# Every data migration of Penpot 2.18.0 (common/src/app/common/files/migrations.cljc), declared as applied so the
# importer doesn't rewrite the generated data. Update it with PENPOT_VERSION.
MIGRATIONS = [
    "legacy-2", "legacy-3", "legacy-5", "legacy-6", "legacy-7", "legacy-8", "legacy-9", "legacy-10", "legacy-11",
    "legacy-12", "legacy-13", "legacy-14", "legacy-16", "legacy-17", "legacy-18", "legacy-19", "legacy-25",
    "legacy-26", "legacy-27", "legacy-28", "legacy-29", "legacy-31", "legacy-32", "legacy-33", "legacy-34",
    "legacy-36", "legacy-37", "legacy-38", "legacy-39", "legacy-40", "legacy-41", "legacy-42", "legacy-43",
    "legacy-44", "legacy-45", "legacy-46", "legacy-47", "legacy-48", "legacy-49", "legacy-50", "legacy-51",
    "legacy-52", "legacy-53", "legacy-54", "legacy-55", "legacy-56", "legacy-57", "legacy-59", "legacy-62",
    "legacy-65", "legacy-66", "legacy-67",
    "0001-remove-tokens-from-groups", "0002-normalize-bool-content-v2", "0002-clean-shape-interactions",
    "0003-fix-root-shape", "0003-convert-path-content-v2", "0005-deprecate-image-type", "0006-fix-old-texts-fills",
    "0008-fix-library-colors-v4", "0009-clean-library-colors", "0009-add-partial-text-touched-flags",
    "0010-fix-swap-slots-pointing-non-existent-shapes", "0011-fix-invalid-text-touched-flags",
    "0012-fix-position-data", "0013-fix-component-path", "0013-clear-invalid-strokes-and-fills",
    "0014-fix-tokens-lib-duplicate-ids", "0014-clear-components-nil-objects", "0015-fix-text-attrs-blank-strings",
    "0015-clean-shadow-color", "0016-copy-fills-from-position-data-to-text-node", "0017-fix-layout-flex-dir",
    "0018-remove-unneeded-objects-from-components", "0019-fix-missing-swap-slots",
    "0020-sync-component-id-with-near-main", "0021-fix-shape-svg-attrs", "0022-normalize-component-root-and-resync",
    "0023-repair-token-themes-with-inexistent-sets", "0024b-fix-stroke-cap-placement",
    "0025-repair-empty-text-content", "0026-fix-svg-raw-shapes-uuids",
]
# Open Sans is the free Google font closest to the reference design's Segoe UI (ADR-0019); Penpot ships it.
FONT = {"fontId": "gfont-open-sans", "fontFamily": "Open Sans"}
VARIANTS = {400: "regular", 500: "500", 600: "600", 700: "700", 800: "800"}
IDENTITY = {"a": 1, "b": 0, "c": 0, "d": 1, "e": 0, "f": 0}
# A fixed date keeps the output identical when an unchanged screen is regenerated.
TIMESTAMP = "2026-01-01T00:00:00.000Z"

# RaidManager's design system (ADR-0019): the dark "Command Center" theme with a teal accent, as named library
# colours, one name per colour. "Group/name" puts a colour in a group of the Assets panel. Every text colour passes
# WCAG AA on the surfaces it is used on; `Text/muted` is the darkest text colour that passes on `Surface/card`.
HOUSE_PALETTE: dict[str, str] = {
    "Neutral/black": "#000000",
    "Surface/page": "#0B111C",
    "Surface/sidebar": "#0F1825",
    "Surface/top bar": "#111C2A",
    "Surface/card": "#172333",
    "Surface/raised": "#1C2B3E",
    "Surface/hero": "#1B3341",
    "Surface/selected": "#1B4548",
    "Surface/track": "#314155",
    "Line/divider": "#2B3B50",
    "Line/card border": "#212F41",
    "Brand/accent": "#5DE0C1",
    "Brand/on accent": "#102830",
    "Brand/logo tile": "#0C2931",
    "Text/primary": "#EFF5FA",
    "Text/secondary": "#9EADC1",
    "Text/muted": "#8394AA",
    "Text/hero": "#C6D7E2",
    "Accent/blue": "#8FB3F6",
    "Accent/amber": "#F3BE75",
    "Accent/purple": "#C4A3EF",
    "Status/warning background": "#4B382A",
    "Status/warning border": "#5E4631",
    "Status/warning title": "#F9D7A7",
    "Status/warning text": "#E2BA87",
    "Status/danger": "#F2878C",
    "Status/on danger": "#2B1115",
    "Status/danger background": "#3F2329",
    "Status/danger border": "#6B3740",
    "Status/danger text": "#F4A7AB",
    "Status/success background": "#173A36",
    "Status/success border": "#22524A",
    "Status/info background": "#1B2E4A",
    "Status/info border": "#2A4468",
    "Avatar/blue": "#293D58",
    "Avatar/red": "#56343C",
    "Avatar/red text": "#F08B8F",
    "Avatar/purple": "#604C80",
    "Avatar/purple text": "#F2E8FF",
    "Community/icon": "#9077B3",
}
# Names for the text styles a screen uses, keyed by (size, weight, letter spacing); others get a generated name.
# Uppercase labels are typed in capitals; the letter spacing is part of the style.
TYPE_SCALE: dict[tuple[float, int, float], str] = {
    (32, 700, 0): "Heading/Page title",
    (22, 700, 0): "Heading/Hero title",
    (18, 700, 0): "Heading/Section title",
    (16, 600, 0): "Heading/Card title",
    (28, 700, 0): "Display/Stat",
    (32, 700, 4): "Display/Code",
    (15, 400, 0): "Body/Large",
    (14, 400, 0): "Body/Default",
    (14, 600, 0): "Body/Strong",
    (13, 400, 0): "Body/Small",
    (13, 700, 0): "Label/Button",
    (12, 400, 0): "Caption/Default",
    (12, 600, 0): "Caption/Strong",
    (11, 700, 1.2): "Label/Section",
    (12, 700, 1.5): "Label/Eyebrow",
    (16, 800, 0.5): "Brand/Logo",
    (12, 800, 0): "Icon/Small",
    (16, 800, 0): "Icon/Default",
    (40, 800, 0): "Icon/Large",
}
WEIGHT_NAMES = {400: "Regular", 500: "Medium", 600: "Semibold", 700: "Bold", 800: "Extra bold"}
# WCAG 2.1 AA: 4.5:1 for normal text, 3:1 for large text (at least 24 px, or 18.66 px bold) and for icons, which
# count as graphical objects (success criterion 1.4.11).
NORMAL_TEXT_CONTRAST = 4.5
LARGE_TEXT_CONTRAST = 3.0
ICON_CONTRAST = 3.0


def relative_luminance(colour: str) -> float:
    channels = [int(colour.lstrip("#")[index:index + 2], 16) / 255 for index in (0, 2, 4)]
    linear = [value / 12.92 if value <= 0.03928 else ((value + 0.055) / 1.055) ** 2.4 for value in channels]
    return 0.2126 * linear[0] + 0.7152 * linear[1] + 0.0722 * linear[2]


def contrast_ratio(foreground: str, background: str) -> float:
    lighter, darker = sorted((relative_luminance(foreground), relative_luminance(background)), reverse=True)
    return (lighter + 0.05) / (darker + 0.05)


def stable_id(*parts: object) -> str:
    """Derives a UUID from names, so a layer keeps its id when the screen is regenerated."""
    return str(uuid.uuid5(NAMESPACE, "/".join(str(part) for part in parts)))


@dataclass
class Rect:
    name: str
    x: float
    y: float
    w: float
    h: float
    fill: str | None
    opacity: float = 1
    radius: float = 0
    stroke: str | None = None


@dataclass
class Circle:
    name: str
    cx: float
    cy: float
    r: float
    fill: str


@dataclass
class Text:
    """A one-line text layer; `top` is the top of its box, and `width` fixes the box instead of growing with the text.

    `icon` marks a glyph used as an icon ("i", "!", a check mark), which WCAG checks as a graphic at 3:1.
    """

    name: str
    x: float
    top: float
    text: str
    size: float = 14
    weight: int = 400
    color: str = "#EFF5FA"
    width: float | None = None
    align: str = "left"
    spacing: float = 0
    icon: bool = False


@dataclass
class Click:
    """What happens in Penpot's View mode when a group is clicked.

    `navigate` shows the board named `target`; `open-overlay` shows it centred over the current board;
    `close-overlay` closes the overlay the group belongs to; `prev-screen` goes back.
    """

    action: str
    target: str | None = None


@dataclass
class Group:
    name: str
    children: list[Item] = field(default_factory=list)
    on_click: Click | None = None


Item = Union[Rect, Circle, Text, Group]


@dataclass
class Board:
    """A Penpot board (frame). Its children use coordinates relative to the board's top-left corner."""

    name: str
    x: float
    y: float
    w: float
    h: float
    fill: str
    children: list[Item] = field(default_factory=list)


def text(name: str, x: float, baseline: float, value: str, size: float = 14, weight: int = 400,
         color: str = "#EFF5FA", width: float | None = None, align: str = "left", spacing: float = 0,
         icon: bool = False) -> Text:
    """Places a text layer by its baseline, the way a designer lines text up."""
    return Text(name, x, round(baseline - size * 1.03, 2), value, size, weight, color, width, align, spacing, icon)


def text_width(item: Text) -> float:
    """Estimates the width of a growing text box; Penpot measures the real width when the file opens."""
    if item.width:
        return item.width
    factor = 0.62 if item.text.isupper() else 0.53
    return round(len(item.text) * item.size * factor + len(item.text) * item.spacing + 2, 1)


def bounds(item: Item) -> tuple[float, float, float, float]:
    if isinstance(item, Rect):
        return item.x, item.y, item.w, item.h
    if isinstance(item, Circle):
        return item.cx - item.r, item.cy - item.r, 2 * item.r, 2 * item.r
    if isinstance(item, Text):
        return item.x, item.top, text_width(item), round(item.size * 1.2, 2)
    boxes = [bounds(child) for child in item.children]
    x1, y1 = min(box[0] for box in boxes), min(box[1] for box in boxes)
    x2, y2 = max(box[0] + box[2] for box in boxes), max(box[1] + box[3] for box in boxes)
    return x1, y1, x2 - x1, y2 - y1


def geometry(x: float, y: float, w: float, h: float) -> dict:
    return {
        "x": x, "y": y, "width": w, "height": h,
        "selrect": {"x": x, "y": y, "width": w, "height": h, "x1": x, "y1": y, "x2": x + w, "y2": y + h},
        "points": [{"x": x, "y": y}, {"x": x + w, "y": y}, {"x": x + w, "y": y + h}, {"x": x, "y": y + h}],
        "transform": dict(IDENTITY), "transformInverse": dict(IDENTITY),
    }


def offset(item: Item, dx: float, dy: float) -> Item:
    """Moves a board-relative item to absolute page coordinates."""
    if isinstance(item, Rect):
        return Rect(item.name, item.x + dx, item.y + dy, item.w, item.h, item.fill, item.opacity, item.radius, item.stroke)
    if isinstance(item, Circle):
        return Circle(item.name, item.cx + dx, item.cy + dy, item.r, item.fill)
    if isinstance(item, Text):
        return Text(item.name, item.x + dx, item.top + dy, item.text, item.size, item.weight, item.color, item.width,
                    item.align, item.spacing, item.icon)
    return Group(item.name, [offset(child, dx, dy) for child in item.children], item.on_click)


def type_style_name(size: float, weight: int, spacing: float) -> str:
    named = TYPE_SCALE.get((size, weight, spacing))
    if named:
        return named
    suffix = f" spaced {spacing:g}" if spacing else ""
    return f"Other/{FONT['fontFamily']} {size:g} {WEIGHT_NAMES.get(weight, str(weight))}{suffix}"


def split_name(full_name: str) -> tuple[str, str]:
    """Splits "Group/name" into Penpot's path and name."""
    path, _, name = full_name.rpartition("/")
    return path, name


class Library:
    """The file's shared colours and typographies, and the references layers make to them."""

    def __init__(self, file_id: str, file_name: str, palette: dict[str, str]) -> None:
        self.file_id = file_id
        self.file_name = file_name
        self.colours: dict[str, dict] = {}
        self.colour_ids: dict[str, str] = {}
        duplicates = sorted({colour.lower() for colour in palette.values()
                             if [value.lower() for value in palette.values()].count(colour.lower()) > 1})
        if duplicates:
            raise ValueError(f"Give each palette colour one name; these have several: {', '.join(duplicates)}")
        for full_name, hex_colour in palette.items():
            path, name = split_name(full_name)
            colour_id = stable_id("colour", file_name, full_name)
            self.colours[colour_id] = {"id": colour_id, "name": name, "path": path, "color": hex_colour.lower(),
                                       "opacity": 1}
            self.colour_ids.setdefault(hex_colour.lower(), colour_id)
        self.typographies: dict[str, dict] = {}

    def fill(self, colour: str | None, opacity: float = 1) -> list[dict]:
        if colour is None:
            return []
        fill: dict = {"fillColor": colour.lower(), "fillOpacity": opacity}
        colour_id = self.colour_ids.get(colour.lower())
        if colour_id:
            fill.update({"fillColorRefId": colour_id, "fillColorRefFile": self.file_id})
        return [fill]

    def stroke(self, colour: str) -> dict:
        stroke: dict = {"strokeColor": colour.lower(), "strokeOpacity": 1, "strokeWidth": 1, "strokeStyle": "solid",
                        "strokeAlignment": "inner"}
        colour_id = self.colour_ids.get(colour.lower())
        if colour_id:
            stroke.update({"strokeColorRefId": colour_id, "strokeColorRefFile": self.file_id})
        return stroke

    def typography(self, item: Text) -> str:
        full_name = type_style_name(item.size, item.weight, item.spacing)
        typography_id = stable_id("typography", self.file_name, full_name)
        if typography_id not in self.typographies:
            path, name = split_name(full_name)
            self.typographies[typography_id] = {
                "id": typography_id, "name": name, "path": path, **FONT, "fontVariantId": VARIANTS[item.weight],
                "fontSize": f"{item.size:g}", "fontWeight": str(item.weight), "fontStyle": "normal",
                "lineHeight": "1.2", "letterSpacing": f"{item.spacing:g}", "textTransform": "none"}
        return typography_id

    def used_colours(self, objects: dict[str, dict]) -> dict[str, dict]:
        """Keeps the palette colours the screen uses, so the Assets panel lists only real colours."""
        used = {paint.get(key) for shape in objects.values()
                for paint in (shape.get("fills") or []) + (shape.get("strokes") or [])
                for key in ("fillColorRefId", "strokeColorRefId")}
        used |= {fill.get("fillColorRefId") for shape in objects.values() if shape.get("type") == "text"
                 for paragraph_set in shape["content"]["children"] for paragraph in paragraph_set["children"]
                 for fill in paragraph.get("fills", [])}
        return {colour_id: colour for colour_id, colour in self.colours.items() if colour_id in used}


class PenpotWriter:
    """Turns boards into Penpot shape objects, keyed by id."""

    def __init__(self, page_id: str, library: Library, board_ids: dict[str, str]) -> None:
        self.page_id = page_id
        self.library = library
        self.board_ids = board_ids
        self.objects: dict[str, dict] = {}

    def base(self, shape_id: str, name: str, parent_id: str, frame_id: str) -> dict:
        return {"id": shape_id, "name": name, "parentId": parent_id, "frameId": frame_id, "pageId": self.page_id,
                "proportion": 1, "proportionLock": False, "rotation": 0}

    def interaction(self, click: Click, where: str) -> dict:
        if click.action not in ("navigate", "open-overlay", "close-overlay", "prev-screen"):
            raise ValueError(f"{where}: unsupported click action {click.action!r}")
        interaction: dict = {"eventType": "click", "actionType": click.action}
        if click.action in ("navigate", "open-overlay"):
            if click.target not in self.board_ids:
                raise ValueError(f"{where}: no board named {click.target!r} to {click.action} to")
            interaction["destination"] = self.board_ids[click.target]
        if click.action == "navigate":
            interaction["preserveScroll"] = False
        if click.action == "open-overlay":
            interaction.update({"overlayPosType": "center", "closeClickOutside": True, "backgroundOverlay": True})
        return interaction

    def add(self, item: Item, parent_id: str, frame_id: str, path: str) -> str:
        shape_id = stable_id(path, item.name)
        if shape_id in self.objects:
            raise ValueError(f"Two layers are named {item.name!r} in {path}; give sibling layers distinct names")
        base = self.base(shape_id, item.name, parent_id, frame_id)
        if isinstance(item, Rect):
            shape = {**base, "type": "rect", **geometry(item.x, item.y, item.w, item.h),
                     "fills": self.library.fill(item.fill, item.opacity)}
            if item.radius:
                shape.update({"r1": item.radius, "r2": item.radius, "r3": item.radius, "r4": item.radius})
            if item.stroke:
                shape["strokes"] = [self.library.stroke(item.stroke)]
        elif isinstance(item, Circle):
            shape = {**base, "type": "circle", **geometry(*bounds(item)), "fills": self.library.fill(item.fill)}
        elif isinstance(item, Text):
            typography_id = self.library.typography(item)
            style = {**FONT, "fontVariantId": VARIANTS[item.weight], "fontSize": f"{item.size:g}",
                     "fontWeight": str(item.weight), "fontStyle": "normal", "lineHeight": "1.2",
                     "letterSpacing": f"{item.spacing:g}", "textTransform": "none", "textDecoration": "none",
                     "textDirection": "ltr", "textAlign": item.align, "fills": self.library.fill(item.color),
                     "typographyRefId": typography_id, "typographyRefFile": self.library.file_id}
            paragraph = {"type": "paragraph", **style, "children": [{"text": item.text, **style}]}
            shape = {**base, "type": "text", **geometry(*bounds(item)),
                     "growType": "fixed" if item.width else "auto-width",
                     "content": {"type": "root", "verticalAlign": "top",
                                 "children": [{"type": "paragraph-set", "children": [paragraph]}]}}
        else:
            shape = {**base, "type": "group", **geometry(*bounds(item)), "fills": [], "shapes": []}
            if item.on_click:
                shape["interactions"] = [self.interaction(item.on_click, f"{path}/{item.name}")]
            self.objects[shape_id] = shape
            shape["shapes"] = [self.add(child, shape_id, frame_id, f"{path}/{item.name}") for child in item.children]
            return shape_id
        self.objects[shape_id] = shape
        return shape_id

    def board(self, board: Board) -> str:
        board_id = self.board_ids[board.name]
        shape = {**self.base(board_id, board.name, ROOT_ID, ROOT_ID), "type": "frame",
                 **geometry(board.x, board.y, board.w, board.h), "fills": self.library.fill(board.fill),
                 "strokes": [], "hideFillOnExport": False, "showContent": False, "shapes": []}
        self.objects[board_id] = shape
        shape["shapes"] = [self.add(offset(child, board.x, board.y), board_id, board_id, board.name)
                           for child in board.children]
        return board_id


def flatten(items: list[Item]) -> list[Rect | Circle | Text]:
    """Lists the drawn layers in paint order, bottom first."""
    drawn: list[Rect | Circle | Text] = []
    for item in items:
        drawn += flatten(item.children) if isinstance(item, Group) else [item]
    return drawn


def contrast_problems(boards: list[Board]) -> list[str]:
    """Checks each text against the topmost opaque shape behind it, or the board, for WCAG AA contrast."""
    problems: list[str] = []
    for board in boards:
        layers = flatten(board.children)
        for index, layer in enumerate(layers):
            if not isinstance(layer, Text):
                continue
            x, y, w, h = bounds(layer)
            background = board.fill
            for behind in layers[:index]:
                if isinstance(behind, Text):
                    continue
                bx, by, bw, bh = bounds(behind)
                opaque = isinstance(behind, Circle) or (behind.fill is not None and behind.opacity == 1)
                if opaque and behind.fill and bx <= x and by <= y and bx + bw >= x + w and by + bh >= y + h:
                    background = behind.fill
            large = layer.size >= 24 or (layer.size >= 18.66 and layer.weight >= 700)
            minimum = ICON_CONTRAST if layer.icon else LARGE_TEXT_CONTRAST if large else NORMAL_TEXT_CONTRAST
            ratio = contrast_ratio(layer.color, background)
            if ratio < minimum:
                problems.append(f"{board.name}: text {layer.name!r} ({layer.text!r}) has contrast {ratio:.2f}:1 on "
                                f"{background}; WCAG AA needs {minimum:g}:1")
    return problems


def unnamed_colours(boards: list[Board], palette: dict[str, str]) -> list[str]:
    """Lists colours used on the boards that the palette doesn't name; screens never invent colours."""
    named = {colour.lower() for colour in palette.values()}
    problems: list[str] = []
    for board in boards:
        colours: list[tuple[str, str | None]] = [(board.name, board.fill)]
        for layer in flatten(board.children):
            if isinstance(layer, Text):
                colours.append((layer.name, layer.color))
            else:
                colours.append((layer.name, layer.fill))
                if isinstance(layer, Rect):
                    colours.append((layer.name, layer.stroke))
        problems += [f"{board.name}: {name!r} uses {colour}, which the palette doesn't name"
                     for name, colour in colours if colour and colour.lower() not in named]
    return problems


def write_penpot(path: Path, file_name: str, page_name: str, boards: list[Board],
                 palette: dict[str, str] | None = None, flows: dict[str, str] | None = None) -> None:
    """Writes the boards as one Penpot file with one page, its shared library, and its prototype flows.

    `palette` maps "Group/name" to a hex colour (default: the house palette); `flows` maps a flow name to the board
    it starts on.
    """
    palette = HOUSE_PALETTE if palette is None else palette
    file_id = stable_id("file", file_name)
    page_id = stable_id("page", file_name, page_name)
    library = Library(file_id, file_name, palette)
    problems = unnamed_colours(boards, palette)
    if problems:
        raise ValueError("Add these colours to the palette with a name:\n" + "\n".join(problems))
    problems = contrast_problems(boards)
    if problems:
        raise ValueError("Text fails WCAG AA contrast:\n" + "\n".join(problems))
    board_ids = {board.name: stable_id("board", board.name) for board in boards}
    writer = PenpotWriter(page_id, library, board_ids)
    root_children = [writer.board(board) for board in boards]
    writer.objects[ROOT_ID] = {**writer.base(ROOT_ID, "Root Frame", ROOT_ID, ROOT_ID), "type": "frame",
                               **geometry(0, 0, 0.01, 0.01), "fills": [{"fillColor": "#ffffff", "fillOpacity": 1}],
                               "strokes": [], "shapes": root_children}
    page: dict = {"id": page_id, "name": page_name, "index": 0}
    if flows:
        page["flows"] = {}
        for flow_name, board_name in flows.items():
            if board_name not in board_ids:
                raise ValueError(f"Flow {flow_name!r} starts on a board that doesn't exist: {board_name!r}")
            flow_id = stable_id("flow", file_name, flow_name)
            page["flows"][flow_id] = {"id": flow_id, "name": flow_name, "startingFrame": board_ids[board_name]}
    manifest = {"type": "penpot/export-files", "version": 1, "generatedBy": "raidmanager/penpot_scene",
                "referer": "penpot", "files": [{"id": file_id, "name": file_name, "features": FEATURES}],
                "relations": []}
    file = {"id": file_id, "name": file_name, "revn": 1, "createdAt": TIMESTAMP, "modifiedAt": TIMESTAMP,
            "isShared": False, "version": FILE_VERSION, "features": FEATURES, "migrations": MIGRATIONS}
    path.parent.mkdir(parents=True, exist_ok=True)
    with zipfile.ZipFile(path, "w", zipfile.ZIP_DEFLATED) as archive:
        def put(name: str, data: dict) -> None:
            info = zipfile.ZipInfo(name, date_time=(2026, 1, 1, 0, 0, 0))
            info.compress_type = zipfile.ZIP_DEFLATED
            archive.writestr(info, json.dumps(data, indent=2, ensure_ascii=False))

        put("manifest.json", manifest)
        put(f"files/{file_id}.json", file)
        for colour_id, colour in sorted(library.used_colours(writer.objects).items()):
            put(f"files/{file_id}/colors/{colour_id}.json", colour)
        for typography_id, typography in sorted(library.typographies.items()):
            put(f"files/{file_id}/typographies/{typography_id}.json", typography)
        put(f"files/{file_id}/pages/{page_id}.json", page)
        for shape_id in sorted(writer.objects):
            put(f"files/{file_id}/pages/{page_id}/{shape_id}.json", writer.objects[shape_id])


def write_mockup(repository: Path, screen: str, page_name: str, boards: list[Board],
                 palette: dict[str, str] | None = None, flows: dict[str, str] | None = None) -> Path:
    """Writes `docs/mockups/<screen>.penpot` and renders its SVG next to it; returns the `.penpot` path."""
    from penpot_render import render

    penpot = repository / "docs" / "mockups" / f"{screen}.penpot"
    write_penpot(penpot, screen, page_name, boards, palette, flows)
    svg, warnings = render(penpot)
    penpot.with_suffix(".svg").write_text(svg, encoding="utf-8", newline="\n")
    for warning in warnings:
        print(f"WARNING: {warning}")
    return penpot
