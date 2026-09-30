"""Write native Penpot mockups (`.penpot` files) from a small scene model (ADR-0018).

A screen script under `scripts/mockups/` describes boards made of rectangles, circles, text and named groups;
`write_penpot` turns them into a Penpot binfile that imports with every text as an editable text layer.

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
FONT = {"fontId": "gfont-roboto", "fontFamily": "Roboto"}
VARIANTS = {400: "regular", 500: "500", 700: "700"}
IDENTITY = {"a": 1, "b": 0, "c": 0, "d": 1, "e": 0, "f": 0}
# A fixed date keeps the output identical when an unchanged screen is regenerated.
TIMESTAMP = "2026-01-01T00:00:00.000Z"


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
    """A one-line text layer; `top` is the top of its box, and `width` fixes the box instead of growing with the text."""

    name: str
    x: float
    top: float
    text: str
    size: float = 14
    weight: int = 400
    color: str = "#424242"
    width: float | None = None
    align: str = "left"
    spacing: float = 0


@dataclass
class Group:
    name: str
    children: list[Item] = field(default_factory=list)


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
         color: str = "#424242", width: float | None = None, align: str = "left", spacing: float = 0) -> Text:
    """Places a text layer by its baseline, the way a designer lines text up."""
    return Text(name, x, round(baseline - size * 1.03, 2), value, size, weight, color, width, align, spacing)


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


def fills(colour: str | None, opacity: float = 1) -> list[dict]:
    return [] if colour is None else [{"fillColor": colour.lower(), "fillOpacity": opacity}]


def offset(item: Item, dx: float, dy: float) -> Item:
    """Moves a board-relative item to absolute page coordinates."""
    if isinstance(item, Rect):
        return Rect(item.name, item.x + dx, item.y + dy, item.w, item.h, item.fill, item.opacity, item.radius, item.stroke)
    if isinstance(item, Circle):
        return Circle(item.name, item.cx + dx, item.cy + dy, item.r, item.fill)
    if isinstance(item, Text):
        return Text(item.name, item.x + dx, item.top + dy, item.text, item.size, item.weight, item.color, item.width,
                    item.align, item.spacing)
    return Group(item.name, [offset(child, dx, dy) for child in item.children])


class PenpotWriter:
    """Turns boards into Penpot shape objects, keyed by id."""

    def __init__(self, page_id: str) -> None:
        self.page_id = page_id
        self.objects: dict[str, dict] = {}

    def base(self, shape_id: str, name: str, parent_id: str, frame_id: str) -> dict:
        return {"id": shape_id, "name": name, "parentId": parent_id, "frameId": frame_id, "pageId": self.page_id,
                "proportion": 1, "proportionLock": False, "rotation": 0}

    def add(self, item: Item, parent_id: str, frame_id: str, path: str) -> str:
        shape_id = stable_id(path, item.name)
        if shape_id in self.objects:
            raise ValueError(f"Two layers are named {item.name!r} in {path}; give sibling layers distinct names")
        base = self.base(shape_id, item.name, parent_id, frame_id)
        if isinstance(item, Rect):
            shape = {**base, "type": "rect", **geometry(item.x, item.y, item.w, item.h),
                     "fills": fills(item.fill, item.opacity)}
            if item.radius:
                shape.update({"r1": item.radius, "r2": item.radius, "r3": item.radius, "r4": item.radius})
            if item.stroke:
                shape["strokes"] = [{"strokeColor": item.stroke.lower(), "strokeOpacity": 1, "strokeWidth": 1,
                                     "strokeStyle": "solid", "strokeAlignment": "inner"}]
        elif isinstance(item, Circle):
            shape = {**base, "type": "circle", **geometry(*bounds(item)), "fills": fills(item.fill)}
        elif isinstance(item, Text):
            style = {**FONT, "fontVariantId": VARIANTS[item.weight], "fontSize": str(item.size),
                     "fontWeight": str(item.weight), "fontStyle": "normal", "lineHeight": "1.2",
                     "letterSpacing": str(item.spacing), "textTransform": "none", "textDecoration": "none",
                     "textDirection": "ltr", "textAlign": item.align, "fills": fills(item.color)}
            paragraph = {"type": "paragraph", **style, "children": [{"text": item.text, **style}]}
            shape = {**base, "type": "text", **geometry(*bounds(item)),
                     "growType": "fixed" if item.width else "auto-width",
                     "content": {"type": "root", "verticalAlign": "top",
                                 "children": [{"type": "paragraph-set", "children": [paragraph]}]}}
        else:
            shape = {**base, "type": "group", **geometry(*bounds(item)), "fills": [], "shapes": []}
            self.objects[shape_id] = shape
            shape["shapes"] = [self.add(child, shape_id, frame_id, f"{path}/{item.name}") for child in item.children]
            return shape_id
        self.objects[shape_id] = shape
        return shape_id

    def board(self, board: Board) -> str:
        board_id = stable_id("board", board.name)
        shape = {**self.base(board_id, board.name, ROOT_ID, ROOT_ID), "type": "frame",
                 **geometry(board.x, board.y, board.w, board.h), "fills": fills(board.fill), "strokes": [],
                 "hideFillOnExport": False, "showContent": False, "shapes": []}
        self.objects[board_id] = shape
        shape["shapes"] = [self.add(offset(child, board.x, board.y), board_id, board_id, board.name)
                           for child in board.children]
        return board_id


def write_penpot(path: Path, file_name: str, page_name: str, boards: list[Board]) -> None:
    """Writes the boards as one Penpot file with one page."""
    file_id = stable_id("file", file_name)
    page_id = stable_id("page", file_name, page_name)
    writer = PenpotWriter(page_id)
    board_ids = [writer.board(board) for board in boards]
    writer.objects[ROOT_ID] = {**writer.base(ROOT_ID, "Root Frame", ROOT_ID, ROOT_ID), "type": "frame",
                               **geometry(0, 0, 0.01, 0.01), "fills": fills("#FFFFFF"), "strokes": [],
                               "shapes": board_ids}
    manifest = {"type": "penpot/export-files", "version": 1, "generatedBy": "raidmanager/penpot_scene",
                "referer": "penpot", "files": [{"id": file_id, "name": file_name, "features": FEATURES}],
                "relations": []}
    file = {"id": file_id, "name": file_name, "revn": 1, "createdAt": TIMESTAMP, "modifiedAt": TIMESTAMP,
            "isShared": False, "version": FILE_VERSION, "features": FEATURES, "migrations": MIGRATIONS}
    page = {"id": page_id, "name": page_name, "index": 0}
    path.parent.mkdir(parents=True, exist_ok=True)
    with zipfile.ZipFile(path, "w", zipfile.ZIP_DEFLATED) as archive:
        def put(name: str, data: dict) -> None:
            info = zipfile.ZipInfo(name, date_time=(2026, 1, 1, 0, 0, 0))
            info.compress_type = zipfile.ZIP_DEFLATED
            archive.writestr(info, json.dumps(data, indent=2, ensure_ascii=False))

        put("manifest.json", manifest)
        put(f"files/{file_id}.json", file)
        put(f"files/{file_id}/pages/{page_id}.json", page)
        for shape_id in sorted(writer.objects):
            put(f"files/{file_id}/pages/{page_id}/{shape_id}.json", writer.objects[shape_id])


def write_mockup(repository: Path, screen: str, page_name: str, boards: list[Board]) -> Path:
    """Writes `docs/mockups/<screen>.penpot` and renders its SVG next to it; returns the `.penpot` path."""
    from penpot_render import render

    penpot = repository / "docs" / "mockups" / f"{screen}.penpot"
    write_penpot(penpot, screen, page_name, boards)
    svg, warnings = render(penpot)
    penpot.with_suffix(".svg").write_text(svg, encoding="utf-8", newline="\n")
    for warning in warnings:
        print(f"WARNING: {warning}")
    return penpot
