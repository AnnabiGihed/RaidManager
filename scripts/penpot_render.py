"""Render a Penpot file (`.penpot`) to the SVG that GitHub, the site and the wiki show (ADR-0018).

The SVG is a deterministic rendering of the file, so CI can check that each mockup's SVG matches its `.penpot`.
It draws boards, groups, rectangles, circles and text from the file's own data, including after a designer edited
it in Penpot. Shapes it can't draw (paths, booleans, images, raw SVG) become labelled placeholders and a warning.

    python scripts/penpot_render.py docs/mockups/<screen>.penpot            # writes docs/mockups/<screen>.svg
    python scripts/penpot_render.py docs/mockups/<screen>.penpot --check    # fails when the SVG is out of date
"""

from __future__ import annotations

import argparse
import json
import sys
import zipfile
from html import escape
from pathlib import Path

ROOT_ID = "00000000-0000-0000-0000-000000000000"
FONT_STACK = "Roboto, Arial, sans-serif"
DRAWN_TYPES = frozenset({"frame", "group", "rect", "circle", "text"})
BOARD_GAP = 80


def number(value: float) -> str:
    """Formats a coordinate without trailing zeros, so the output is stable and compact."""
    return f"{round(float(value), 2):g}"


def read_page(path: Path) -> tuple[str, dict[str, dict]]:
    """Returns the first page's name and its shapes, keyed by id."""
    with zipfile.ZipFile(path) as archive:
        manifest = json.loads(archive.read("manifest.json"))
        file_id = manifest["files"][0]["id"]
        pages = []
        for name in archive.namelist():
            parts = name.split("/")
            if len(parts) == 4 and parts[:3] == ["files", file_id, "pages"] and name.endswith(".json"):
                pages.append(json.loads(archive.read(name)))
        if not pages:
            raise ValueError(f"{path} has no page")
        page = min(pages, key=lambda item: item.get("index", 0))
        prefix = f"files/{file_id}/pages/{page['id']}/"
        shapes = {}
        for name in archive.namelist():
            if name.startswith(prefix) and name.endswith(".json"):
                shape = json.loads(archive.read(name))
                shapes[shape["id"]] = shape
    return page["name"], shapes


def first(items: list[dict] | None, key: str) -> dict | None:
    return next((item for item in items or [] if key in item and not item.get("hidden")), None)


def fill_attributes(shape: dict) -> str:
    fill = first(shape.get("fills"), "fillColor")
    if fill is None:
        return 'fill="none"'
    opacity = fill.get("fillOpacity", 1)
    return f'fill="{fill["fillColor"]}"' + ("" if opacity == 1 else f' fill-opacity="{number(opacity)}"')


def transform_attribute(shape: dict) -> str:
    """Applies a Penpot transform, which is relative to the shape's centre, as an SVG matrix."""
    matrix = shape.get("transform") or {}
    a, b, c, d, e, f = (matrix.get(key, default) for key, default in zip("abcdef", (1, 0, 0, 1, 0, 0)))
    if (a, b, c, d, e, f) == (1, 0, 0, 1, 0, 0):
        return ""
    cx, cy = shape["x"] + shape["width"] / 2, shape["y"] + shape["height"] / 2
    tx = cx + e - a * cx - c * cy
    ty = cy + f - b * cx - d * cy
    return f' transform="matrix({number(a)} {number(b)} {number(c)} {number(d)} {number(tx)} {number(ty)})"'


class Renderer:
    def __init__(self, shapes: dict[str, dict]) -> None:
        self.shapes = shapes
        self.warnings: list[str] = []
        self.clips = 0

    def children(self, shape: dict) -> list[dict]:
        return [self.shapes[child] for child in shape.get("shapes", []) if child in self.shapes]

    def draw(self, shape: dict) -> list[str]:
        if shape.get("hidden"):
            return []
        kind = shape.get("type")
        opacity = shape.get("opacity", 1)
        wrap_open = f'<g opacity="{number(opacity)}">' if opacity != 1 else ""
        parts = [wrap_open] if wrap_open else []
        if kind == "frame":
            parts += self.frame(shape)
        elif kind == "group":
            for child in self.children(shape):
                parts += self.draw(child)
        elif kind == "rect":
            parts.append(self.rect(shape))
        elif kind == "circle":
            parts.append(self.circle(shape))
        elif kind == "text":
            parts += self.text(shape)
        else:
            parts += self.placeholder(shape)
        if wrap_open:
            parts.append("</g>")
        return parts

    def frame(self, shape: dict) -> list[str]:
        x, y, w, h = shape["x"], shape["y"], shape["width"], shape["height"]
        self.clips += 1
        clip = f"clip{self.clips}"
        parts = [f'<clipPath id="{clip}"><rect x="{number(x)}" y="{number(y)}" width="{number(w)}" height="{number(h)}"/></clipPath>',
                 f'<rect x="{number(x)}" y="{number(y)}" width="{number(w)}" height="{number(h)}" {fill_attributes(shape)}/>']
        clipped = not shape.get("showContent", False)
        parts.append(f'<g clip-path="url(#{clip})">' if clipped else "<g>")
        for child in self.children(shape):
            parts += self.draw(child)
        parts.append("</g>")
        return parts

    def stroke(self, shape: dict) -> tuple[str, float]:
        stroke = first(shape.get("strokes"), "strokeColor")
        if stroke is None:
            return "", 0
        width = stroke.get("strokeWidth", 1)
        inset = {"inner": width / 2, "outer": -width / 2}.get(stroke.get("strokeAlignment", "center"), 0)
        opacity = stroke.get("strokeOpacity", 1)
        attributes = f' stroke="{stroke["strokeColor"]}" stroke-width="{number(width)}"'
        if opacity != 1:
            attributes += f' stroke-opacity="{number(opacity)}"'
        return attributes, inset

    def rect(self, shape: dict) -> str:
        stroke, inset = self.stroke(shape)
        x, y = shape["x"] + inset, shape["y"] + inset
        w, h = shape["width"] - 2 * inset, shape["height"] - 2 * inset
        radius = shape.get("r1", 0)
        rx = f' rx="{number(radius)}"' if radius else ""
        return (f'<rect x="{number(x)}" y="{number(y)}" width="{number(w)}" height="{number(h)}"{rx} '
                f'{fill_attributes(shape)}{stroke}{transform_attribute(shape)}/>')

    def circle(self, shape: dict) -> str:
        stroke, inset = self.stroke(shape)
        rx, ry = shape["width"] / 2 - inset, shape["height"] / 2 - inset
        cx, cy = shape["x"] + shape["width"] / 2, shape["y"] + shape["height"] / 2
        return (f'<ellipse cx="{number(cx)}" cy="{number(cy)}" rx="{number(rx)}" ry="{number(ry)}" '
                f'{fill_attributes(shape)}{stroke}{transform_attribute(shape)}/>')

    def text(self, shape: dict) -> list[str]:
        parts = []
        top = shape["y"]
        paragraphs = [paragraph for paragraph_set in (shape.get("content") or {}).get("children", [])
                      for paragraph in paragraph_set.get("children", [])]
        for paragraph in paragraphs:
            leaves = paragraph.get("children") or [{}]
            style = {**paragraph, **leaves[0]}
            size = float(style.get("fontSize", 14))
            line = "".join(leaf.get("text", "") for leaf in leaves)
            transform = style.get("textTransform", "none")
            line = line.upper() if transform == "uppercase" else line.lower() if transform == "lowercase" else line
            align = style.get("textAlign", "left")
            anchor, x = {"center": ("middle", shape["x"] + shape["width"] / 2),
                         "right": ("end", shape["x"] + shape["width"])}.get(align, ("start", shape["x"]))
            fill = first(style.get("fills"), "fillColor") or {"fillColor": "#000000"}
            spacing = float(style.get("letterSpacing", 0) or 0)
            spacing_attribute = f' letter-spacing="{number(spacing)}"' if spacing else ""
            parts.append(f'<text x="{number(x)}" y="{number(top + size * 1.03)}" font-family="{FONT_STACK}" '
                         f'font-size="{number(size)}" font-weight="{style.get("fontWeight", "400")}" '
                         f'fill="{fill["fillColor"]}" text-anchor="{anchor}"{spacing_attribute}'
                         f'{transform_attribute(shape)}>{escape(line)}</text>')
            top += size * float(style.get("lineHeight", 1.2) or 1.2)
        return parts

    def placeholder(self, shape: dict) -> list[str]:
        self.warnings.append(f"{shape.get('name')} ({shape.get('type')}) is drawn as a placeholder")
        x, y, w, h = shape.get("x", 0), shape.get("y", 0), shape.get("width", 0), shape.get("height", 0)
        return [f'<rect x="{number(x)}" y="{number(y)}" width="{number(w)}" height="{number(h)}" fill="none" '
                'stroke="#9e9e9e" stroke-dasharray="4 4"/>',
                f'<text x="{number(x + 4)}" y="{number(y + 14)}" font-family="{FONT_STACK}" font-size="11" '
                f'fill="#9e9e9e">{escape(str(shape.get("name", "")))}</text>']


def render(path: Path) -> tuple[str, list[str]]:
    """Returns the SVG of a `.penpot` file's first page and the warnings for shapes drawn as placeholders."""
    page_name, shapes = read_page(path)
    root = shapes[ROOT_ID]
    top_level = [shapes[child] for child in root.get("shapes", []) if child in shapes]
    if not top_level:
        raise ValueError(f"{path} has no boards")
    left = min(shape["x"] for shape in top_level) - BOARD_GAP
    top = min(shape["y"] for shape in top_level) - BOARD_GAP
    right = max(shape["x"] + shape["width"] for shape in top_level) + BOARD_GAP
    bottom = max(shape["y"] + shape["height"] for shape in top_level) + BOARD_GAP
    width, height = right - left, bottom - top
    renderer = Renderer(shapes)
    parts = [f'<svg xmlns="http://www.w3.org/2000/svg" width="{number(width)}" height="{number(height)}" '
             f'viewBox="{number(left)} {number(top)} {number(width)} {number(height)}">',
             f"<title>{escape(page_name)} (rendered from {escape(path.name)})</title>",
             f'<rect x="{number(left)}" y="{number(top)}" width="{number(width)}" height="{number(height)}" fill="#e8e8e8"/>']
    for shape in top_level:
        if shape.get("type") == "frame":
            parts.append(f'<text x="{number(shape["x"])}" y="{number(shape["y"] - 14)}" font-family="{FONT_STACK}" '
                         f'font-size="16" fill="#616161">{escape(shape.get("name", ""))}</text>')
        parts += renderer.draw(shape)
    parts.append("</svg>")
    return "\n".join(parts) + "\n", renderer.warnings


def svg_path(penpot: Path) -> Path:
    return penpot.with_suffix(".svg")


def is_current(penpot: Path) -> bool:
    """Tells whether the SVG next to a `.penpot` file is its current rendering."""
    svg = svg_path(penpot)
    return svg.is_file() and svg.read_text(encoding="utf-8").replace("\r\n", "\n") == render(penpot)[0]


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("penpot", type=Path, nargs="+", help="the .penpot files to render")
    parser.add_argument("--check", action="store_true", help="fail when an SVG isn't the current rendering")
    args = parser.parse_args()
    failed = False
    for penpot in args.penpot:
        svg, warnings = render(penpot)
        for warning in warnings:
            print(f"WARNING: {penpot}: {warning}")
        if args.check:
            if not is_current(penpot):
                print(f"ERROR: {svg_path(penpot)} isn't the current rendering; run python scripts/penpot_render.py {penpot}")
                failed = True
        else:
            svg_path(penpot).write_text(svg, encoding="utf-8", newline="\n")
            print(f"Rendered {svg_path(penpot)}")
    return 1 if failed else 0


if __name__ == "__main__":
    sys.exit(main())
