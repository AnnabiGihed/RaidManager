"""The candidate workspace (story #29, design story #189, task #233).

Four boards in RaidManager's design system (ADR-0019), seen by an officer, linked as a clickable prototype. The
sample players come from `roster_data.py`, shared with the other roster mockups:

1. All candidates: player, preferred option, other offered loadouts, availability, note, readiness, data freshness
   and assignment, with the role counts above. The Healers count opens 2; the Unassigned tanks filter opens 3.
2. Filtered by role Healer, with one candidate's full offer expanded. Clear filters opens 1.
3. No candidate matches the filters. Clear filters opens 1.
4. Data still syncing: a signup that just arrived from Discord and two characters whose data is being refreshed.

Website and Discord signups show as one candidate each.

Run from the repository root: python scripts/mockups/candidate_workspace.py
Once the owner edits the design in Penpot, the downloaded file replaces docs/mockups/candidate-workspace.penpot and
this script is no longer run.
"""

from __future__ import annotations

import sys
from pathlib import Path

REPOSITORY = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(REPOSITORY / "scripts"))

from penpot_components import (  # noqa: E402
    BOARD_GAP, BOARD_H, BOARD_W, CONTENT_TOP, CONTENT_W, CONTENT_X, app_screen, badge, button, card, form_field,
    notice, page_header,
)
from penpot_scene import HOUSE_PALETTE, Board, Circle, Click, Group, Item, Rect, text, write_mockup  # noqa: E402
from roster_data import (  # noqa: E402
    AVAILABILITY_TONES, CANDIDATES, CLASS_COLOURS, HEALER, PALETTE_ADDITIONS, ROLE_COUNTS,
    TOTAL_OPTIONS, TOTAL_PLAYERS, VERDICT_TONES, WHEN, Candidate,
)

P = HOUSE_PALETTE
PALETTE = {**HOUSE_PALETTE, **PALETTE_ADDITIONS}
ACCENT, SECONDARY, MUTED, DIVIDER = P["Brand/accent"], P["Text/secondary"], P["Text/muted"], P["Line/divider"]
RAISED, SELECTED = P["Surface/raised"], P["Surface/selected"]
TEXT = P["Text/primary"]
PAGE = "Roster builder"
SECTION = "Candidates"
TITLE = "Icecrown Citadel candidates"
CLEAR = "Clear filters"
CLEAR_BUTTON = "Clear button"

ALL = "1 · All candidates"
FILTERED = "2 · Filtered, one offer expanded"
EMPTY = "3 · No candidate matches"
SYNCING = "4 · Data still syncing"

TOP = CONTENT_TOP + 120
COLUMNS = {"player": 24, "preferred": 220, "availability": 500, "readiness": 640, "data": 830, "assignment": 950}


def label(name: str, x: float, y: float, value: str) -> Item:
    return text(name, x, y, value, 11, 700, MUTED, None, "left", 1.2)


def role_counts(linked: bool) -> Group:
    width = (CONTENT_W - 3 * 16) / 4
    tiles: list[Item] = []
    for index, (role, (offered, preferred)) in enumerate(ROLE_COUNTS.items()):
        x = CONTENT_X + index * (width + 16)
        target = Click("navigate", FILTERED) if linked and role == HEALER else None
        tiles.append(Group(f"{role} count", [
            *card(x, TOP, width, 76),
            label("Role", x + 20, TOP + 28, role.upper()),
            text("Offered", x + 20, TOP + 58, f"{offered} offered", 18, 700),
            text("Preferred", x + 140, TOP + 58, f"{preferred} prefer it", 12, 400, SECONDARY),
        ], target))
    return Group("Role counts", tiles)


def chip(name: str, x: float, y: float, value: str, active: bool = True, on_click: Click | None = None) -> Group:
    width = len(value) * 7 + 40
    items: list[Item] = [Rect("Background", x, y, width, 28, SELECTED if active else RAISED, 1, 14, ACCENT if active else DIVIDER),
                         text("Label", x + 14, y + 19, value, 12, 600, ACCENT if active else SECONDARY)]
    if active:
        items.append(text("Remove", x + width - 22, y + 19, "×", 12, 800, ACCENT, 12, "center", icon=True))
    return Group(name, items, on_click)


def filter_bar(y: float, search: str, chips: list[str], linked: bool) -> Group:
    items: list[Item] = [
        form_field("Search field", CONTENT_X, y, 300, "Search", search or "Player or character"),
        form_field("Role filter", CONTENT_X + 316, y, 150, "Role", "Healer" if "Role: Healer" in chips else "Any", True),
        form_field("Response filter", CONTENT_X + 482, y, 150, "Response", "Any", True),
        form_field("Assignment filter", CONTENT_X + 648, y, 170, "Assignment", "Any", True),
        form_field("Readiness filter", CONTENT_X + 834, y, 150, "Readiness", "Available" if "Readiness: Available" in chips else "Any", True),
    ]
    if linked:
        items.append(chip("Unassigned tanks", CONTENT_X, y + 64, "Saved view: unassigned tanks", False,
                          Click("navigate", EMPTY)))
    x = CONTENT_X
    for index, value in enumerate(chips):
        items.append(chip(f"Active filter {index + 1}", x, y + 64, value))
        x += len(value) * 7 + 48
    return Group("Filters", items)


def candidate_row(index: int, y: float, candidate: Candidate, syncing: str = "") -> Group:
    option = candidate.preferred
    others = len(candidate.options) - 1
    availability = candidate.availability + (f" {candidate.arrival}" if candidate.arrival else "")
    items: list[Item] = [
        Rect("Divider", CONTENT_X, y, CONTENT_W, 1, DIVIDER),
        text("Player", CONTENT_X + COLUMNS["player"], y + 28, candidate.player, 14, 600),
        text("Options", CONTENT_X + COLUMNS["player"], y + 46,
             f"{len(candidate.options)} option" + ("" if others == 0 else "s"), 12, 400, MUTED),
        Circle("Class colour", CONTENT_X + COLUMNS["preferred"] + 5, y + 23, 5, CLASS_COLOURS[option.wow_class]),
        text("Preferred", CONTENT_X + COLUMNS["preferred"] + 16, y + 28, f"{option.character} · {option.spec}", 14, 600),
        text("Role", CONTENT_X + COLUMNS["preferred"] + 16, y + 46,
             f"{option.role} · GS {option.gear_score}" + (f" · +{others} more" if others else ""), 12, 400, MUTED),
        badge("Availability badge", CONTENT_X + COLUMNS["availability"], y + 12, availability,
              AVAILABILITY_TONES[candidate.availability]),
    ]
    if candidate.availability == "Declined":
        items.append(text("Readiness", CONTENT_X + COLUMNS["readiness"], y + 28, "Not offered", 12, 400, MUTED))
    elif syncing:
        items.append(badge("Readiness badge", CONTENT_X + COLUMNS["readiness"], y + 12, "Checking", "neutral"))
    else:
        items.append(badge("Readiness badge", CONTENT_X + COLUMNS["readiness"], y + 12, option.verdict,
                           VERDICT_TONES[option.verdict]))
    items += [
        text("Data", CONTENT_X + COLUMNS["data"], y + 28, syncing or candidate.freshness, 13, 400,
             ACCENT if syncing else SECONDARY),
        text("Assignment", CONTENT_X + COLUMNS["assignment"], y + 28, candidate.assignment, 13, 400,
             TEXT if candidate.assignment != "Unassigned" else MUTED),
    ]
    if candidate.note:
        items.append(text("Note", CONTENT_X + COLUMNS["preferred"] + 16, y + 64, f"Note: {candidate.note}", 12, 400,
                          SECONDARY))
    return Group(f"Row {index + 1} {candidate.player}", items)


def expanded_offer(y: float, candidate: Candidate) -> Group:
    items: list[Item] = [Rect("Background", CONTENT_X, y, CONTENT_W, 36 + 40 * len(candidate.options), RAISED),
                         label("Offer label", CONTENT_X + 236, y + 24, f"ALL OPTIONS FROM {candidate.player.upper()}")]
    for index, option in enumerate(candidate.options):
        oy = y + 36 + index * 40
        items += [Circle(f"Class colour {index + 1}", CONTENT_X + 241, oy + 16, 5, CLASS_COLOURS[option.wow_class]),
                  text(f"Option {index + 1}", CONTENT_X + 252, oy + 21,
                       f"{option.character} · {option.spec} · {option.role} · GS {option.gear_score}"
                       + (" · preferred" if index == 0 else ""), 13, 400, TEXT),
                  badge(f"Verdict {index + 1}", CONTENT_X + COLUMNS["readiness"], oy + 4, option.verdict,
                        VERDICT_TONES[option.verdict])]
    return Group("Expanded offer", items)


def table(y: float, candidates: list[Candidate], expanded: str = "", syncing: dict[str, str] | None = None) -> Group:
    head_h = 44
    heights = [80 if candidate.note else 64 for candidate in candidates]
    extra = next((36 + 40 * len(c.options) for c in candidates if c.player == expanded), 0)
    items: list[Item] = card(CONTENT_X, y, CONTENT_W, head_h + sum(heights) + extra + 8)
    items.append(Group("Table header", [
        label(f"{heading.title()} heading", CONTENT_X + COLUMNS[key], y + 27, heading)
        for key, heading in (("player", "PLAYER"), ("preferred", "PREFERRED OPTION"), ("availability", "RESPONSE"),
                             ("readiness", "READINESS"), ("data", "DATA"), ("assignment", "ASSIGNMENT"))]))
    ry = y + head_h
    for index, candidate in enumerate(candidates):
        items.append(candidate_row(index, ry, candidate, (syncing or {}).get(candidate.player, "")))
        ry += heights[index]
        if candidate.player == expanded:
            items.append(expanded_offer(ry, candidate))
            ry += extra
    return Group("Candidates table", items)


def header() -> Group:
    return page_header(PAGE, TITLE, WHEN)


def all_candidates() -> list[Item]:
    table_y = TOP + 176
    return [header(), role_counts(True), filter_bar(TOP + 108, "", [], True), table(table_y + 36, [c for c in CANDIDATES[:6] if c.player != "Selm Voss"]),
            text("Count", CONTENT_X + CONTENT_W - 360, table_y + 20,
                 f"Showing 5 of {TOTAL_PLAYERS} players · {TOTAL_OPTIONS} options offered", 12, 400, MUTED, 360,
                 "right")]


def filtered() -> list[Item]:
    healers = [c for c in CANDIDATES if c.preferred.role == HEALER]
    return [header(), role_counts(False), filter_bar(TOP + 108, "", ["Role: Healer"], False),
            button(CLEAR_BUTTON, CONTENT_X + 160, TOP + 168, CLEAR, "secondary", 128, Click("navigate", ALL)),
            table(TOP + 220, healers, expanded="Kiri Dawn")]


def empty() -> list[Item]:
    y = TOP + 220
    return [header(), role_counts(False),
            filter_bar(TOP + 108, "", ["Role: Tank", "Assignment: Unassigned", "Readiness: Available"], False),
            Group("Empty state", [
                *card(CONTENT_X, y, CONTENT_W, 176),
                text("Title", CONTENT_X, y + 64, "No candidate matches these filters", 18, 700, TEXT,
                     CONTENT_W, "center"),
                text("Message", CONTENT_X, y + 92, "Every available tank is already in a draft or on the bench.", 14, 400,
                     SECONDARY, CONTENT_W, "center"),
                button(CLEAR_BUTTON, CONTENT_X + CONTENT_W / 2 - 64, y + 116, CLEAR, "secondary", 128,
                       Click("navigate", ALL)),
            ])]


def syncing() -> list[Item]:
    arrived = [c for c in CANDIDATES if c.player in ("Selm Voss", "Ilsa Brand", "Bryn Valewood", "Arvel Moss")]
    return [header(),
            notice("Syncing notice", CONTENT_X, TOP, CONTENT_W, "Some data is still arriving",
                   "Selm Voss just signed up in Discord, and two characters are syncing. Their readiness appears when it's checked.",
                   "info"),
            filter_bar(TOP + 108, "", [], False),
            table(TOP + 176, arrived, syncing={"Selm Voss": "From Discord, now", "Ilsa Brand": "Syncing…"})]


def boards() -> list[Board]:
    right, below = BOARD_W + BOARD_GAP, BOARD_H + BOARD_GAP

    def screen(name: str, x: float, y: float, content: list[Item]) -> Board:
        return app_screen(name, x, y, PAGE, content, section=SECTION)

    return [screen(ALL, 0, 0, all_candidates()), screen(FILTERED, right, 0, filtered()),
            screen(EMPTY, 0, below, empty()), screen(SYNCING, right, below, syncing())]


def main(repository: Path = REPOSITORY) -> Path:
    return write_mockup(repository, "candidate-workspace", "Candidate workspace", boards(), PALETTE,
                        {"Filter candidates": ALL, "Data still syncing": SYNCING})


if __name__ == "__main__":
    print("Wrote", main())
