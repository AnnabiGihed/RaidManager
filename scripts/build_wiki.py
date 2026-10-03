"""Build the GitHub Wiki pages from the repository's docs/ folder.

docs/ is the only source. The wiki is a generated mirror: every page is rebuilt on each merge to main and
overwrites any edit made in the wiki itself (ADR-0014).
"""

from __future__ import annotations

import argparse
import posixpath
import re
import shutil
import sys
import tempfile
from dataclasses import dataclass
from pathlib import Path


DEFAULT_REPOSITORY = "AnnabiGihed/RaidManager"
REPOSITORY_ROOT = Path(__file__).resolve().parents[1]
# The pages are written only here, inside the ignored site/ folder; the docs-publish workflow pushes them.
WIKI_OUTPUT = REPOSITORY_ROOT / "site" / "wiki"
HOME_SOURCE = "index.md"
NAV_ENTRY = re.compile(r"^(?P<indent>\s*+)- (?P<title>[^:]++):\s*+(?P<path>\S+\.md)?\s*$")
INLINE_LINK = re.compile(r"(?P<prefix>!?\[[^\]]*\]\()(?P<target>[^)\s]+)(?P<suffix>(?:\s+\"[^\"]*\")?\))")
REFERENCE_LINK = re.compile(r"^(?P<prefix>\s*+\[[^\]]++\]:\s*+)(?P<target>\S++)(?P<suffix>.*)$")
FENCE = re.compile(r"^\s*(```|~~~)")
EXTERNAL = re.compile(r"^(?:[a-z][a-z0-9+.-]*:|#|//)", re.IGNORECASE)


@dataclass(frozen=True)
class NavItem:
    """One line of the MkDocs navigation: a section heading or a page."""

    depth: int
    title: str
    source: str | None


@dataclass
class Wiki:
    """The generated wiki: page files, copied assets, and the problems found while building it."""

    pages: dict[str, str]
    assets: set[str]
    errors: list[str]


def page_name(source: str, title: str) -> str:
    """Returns the wiki page name for a docs file: its title with every run of other characters as one hyphen."""
    if source == HOME_SOURCE:
        return "Home"
    return re.sub(r"[^A-Za-z0-9]+", "-", title).strip("-")


def title_of(document: str) -> str | None:
    first = document.lstrip("﻿").splitlines()[0] if document.strip() else ""
    return first[2:].strip() if first.startswith("# ") else None


def read_nav(mkdocs: str) -> list[NavItem]:
    """Reads the nav block of mkdocs.yml without a YAML parser (the file uses Python tags a safe loader rejects)."""
    items: list[NavItem] = []
    in_nav = False
    for line in mkdocs.splitlines():
        if line.startswith("nav:"):
            in_nav = True
            continue
        if in_nav and line and not line.startswith(" "):
            break
        match = NAV_ENTRY.match(line) if in_nav else None
        if match:
            items.append(NavItem(len(match["indent"]) // 4, match["title"].strip(), match["path"]))
    return items


def rewrite_target(target: str, source: str, names: dict[str, str], repository: str,
                   docs: Path, errors: list[str], assets: set[str]) -> str:
    """Maps one link target in docs/<source> to its wiki or GitHub address."""
    if EXTERNAL.match(target):
        return target
    path, _, anchor = target.partition("#")
    resolved = posixpath.normpath(posixpath.join(posixpath.dirname(source), path))
    fragment = f"#{anchor}" if anchor else ""
    wiki = f"https://github.com/{repository}/wiki"
    if resolved.startswith("../"):
        outside = posixpath.normpath(posixpath.join("docs", resolved))
        return f"https://github.com/{repository}/blob/main/{outside}{fragment}"
    if not (docs / resolved).is_file():
        errors.append(f"docs/{source}: link to missing file {target}")
        return target
    if resolved.endswith(".md"):
        return f"{wiki}/{names[resolved]}{fragment}" if resolved != HOME_SOURCE else f"{wiki}{fragment}"
    assets.add(resolved)
    return f"{wiki}/{resolved}{fragment}"


def convert(document: str, source: str, names: dict[str, str], repository: str,
            docs: Path, errors: list[str], assets: set[str]) -> str:
    """Rewrites links outside code blocks, drops the H1 (the wiki shows the page name) and adds the source note."""
    lines = document.lstrip("﻿").splitlines()
    if lines and lines[0].startswith("# "):
        lines = lines[1:]
        while lines and not lines[0].strip():
            lines = lines[1:]

    def replace(match: re.Match[str]) -> str:
        target = rewrite_target(match["target"], source, names, repository, docs, errors, assets)
        return f"{match['prefix']}{target}{match['suffix']}"

    output: list[str] = []
    in_fence = False
    for line in lines:
        if FENCE.match(line):
            in_fence = not in_fence
        elif not in_fence:
            line = REFERENCE_LINK.sub(replace, line) if REFERENCE_LINK.match(line) else INLINE_LINK.sub(replace, line)
        output.append(line)
    note = (f"> Generated from [`docs/{source}`](https://github.com/{repository}/blob/main/docs/{source}). "
            "Edit that file in the repository: changes made in the wiki are overwritten.")
    return "\n".join([note, "", *output]).rstrip() + "\n"


def sidebar(nav: list[NavItem], names: dict[str, str]) -> str:
    lines = ["**[RaidManager](Home)**", ""]
    for item in nav:
        if item.source == HOME_SOURCE:
            continue
        indent = "  " * item.depth
        if item.source is None:
            lines.append(f"{indent}- **{item.title}**")
        else:
            lines.append(f"{indent}- [{item.title}]({names[item.source]})")
    return "\n".join(lines) + "\n"


def build(root: Path, repository: str = DEFAULT_REPOSITORY, commit: str | None = None) -> Wiki:
    docs = root / "docs"
    sources = sorted(path.relative_to(docs).as_posix() for path in docs.rglob("*.md"))
    errors: list[str] = []
    titles: dict[str, str] = {}
    for source in sources:
        title = title_of((docs / source).read_text(encoding="utf-8"))
        if title is None:
            errors.append(f"docs/{source}: the first line must be the page's # title")
            title = Path(source).stem
        titles[source] = title
    names = {source: page_name(source, titles[source]) for source in sources}
    seen: dict[str, str] = {}
    for source, name in names.items():
        if name.lower() in seen:
            errors.append(f"docs/{source} and docs/{seen[name.lower()]} both become the wiki page {name}")
        seen[name.lower()] = source

    nav = read_nav((root / "mkdocs.yml").read_text(encoding="utf-8"))
    listed = {item.source for item in nav if item.source}
    errors.extend(f"mkdocs.yml: nav lists missing file {path}" for path in sorted(listed - set(sources)))
    errors.extend(f"docs/{path}: missing from the mkdocs.yml nav" for path in sources if path not in listed)

    assets: set[str] = set()
    pages = {
        f"{names[source]}.md": convert((docs / source).read_text(encoding="utf-8"), source, names, repository,
                                       docs, errors, assets)
        for source in sources
    }
    if not errors:
        pages["_Sidebar.md"] = sidebar(nav, names)
        revision = f" at [`{commit[:7]}`](https://github.com/{repository}/commit/{commit})" if commit else ""
        pages["_Footer.md"] = (f"Generated from the [`docs/`](https://github.com/{repository}/tree/main/docs) folder"
                               f"{revision}. Edit the documentation there, never in the wiki.\n")
    return Wiki(pages, assets, errors)


def write(wiki: Wiki, docs: Path, out: Path) -> None:
    out.mkdir(parents=True, exist_ok=True)
    for name, content in wiki.pages.items():
        (out / name).write_text(content, encoding="utf-8", newline="\n")
    for asset in sorted(wiki.assets):
        target = out / asset
        target.parent.mkdir(parents=True, exist_ok=True)
        shutil.copyfile(docs / asset, target)


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--write", action="store_true", help=f"write the pages to {WIKI_OUTPUT}; omit to only check")
    parser.add_argument("--repository", default=DEFAULT_REPOSITORY)
    parser.add_argument("--commit", help="commit the pages are generated from, shown in the footer")
    args = parser.parse_args()
    wiki = build(REPOSITORY_ROOT, args.repository, args.commit)
    for error in wiki.errors:
        print(f"ERROR: {error}")
    if wiki.errors:
        return 1
    if args.write:
        shutil.rmtree(WIKI_OUTPUT, ignore_errors=True)
        write(wiki, REPOSITORY_ROOT / "docs", WIKI_OUTPUT)
    else:
        with tempfile.TemporaryDirectory() as scratch:
            write(wiki, REPOSITORY_ROOT / "docs", Path(scratch))
    print(f"Wiki built: {len(wiki.pages)} pages, {len(wiki.assets)} assets")
    return 0


if __name__ == "__main__":
    sys.exit(main())
