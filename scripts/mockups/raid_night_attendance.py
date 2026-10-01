"""Raid-night attendance and swaps (story #33, design story #193, task #241).

Four boards in RaidManager's design system (ADR-0019), seen by an officer on raid night, linked as a clickable
prototype. The roster is the published roster from `roster_publication.py`:

1. Check-in: every place present, late with the expected time, absent or not checked in yet, and the bench.
   Find a substitute on Pyrelight (absent) opens 2.
2. A substitute swap for Pyrelight: Shadestep from the bench, with ownership, lockout and availability checked
   again. Choosing Lothar instead opens 3; Swap in Shadestep opens 4.
3. The swap refused: Lothar has an active Icecrown Citadel save, so it can't be swapped in or invited. No override.
   Back opens 2.
4. The night's log after the raid: check-ins, absence, the swap and the refusal, each with who and when.

Run from the repository root: python scripts/mockups/raid_night_attendance.py
Once the owner edits the design in Penpot, the downloaded file replaces docs/mockups/raid-night-attendance.penpot
and this script is no longer run.
"""

from __future__ import annotations

import sys
from pathlib import Path

REPOSITORY = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(REPOSITORY / "scripts"))

from penpot_components import (  # noqa: E402
    BOARD_GAP, BOARD_H, BOARD_W, CONTENT_TOP, CONTENT_W, CONTENT_X, app_screen, badge, button, card, notice,
    page_header,
)
from penpot_scene import HOUSE_PALETTE, Board, Circle, Click, Group, Item, Rect, text, write_mockup  # noqa: E402
from roster_data import CLASS_COLOURS, PALETTE_ADDITIONS, Slot  # noqa: E402
from roster_publication import roster  # noqa: E402

P = HOUSE_PALETTE
PALETTE = {**HOUSE_PALETTE, **PALETTE_ADDITIONS}
ACCENT, SECONDARY, MUTED, DIVIDER = P["Brand/accent"], P["Text/secondary"], P["Text/muted"], P["Line/divider"]
TEXT = P["Text/primary"]
PAGE = "Raid night"
OFFICER_NAME = "Gihed Annabi"
TITLE = "Icecrown Citadel · tonight"
WHEN = "Fri 9 Oct, 21:00 · 19:00 UTC · invites from 20:45"
ABSENT_CHARACTER, SUBSTITUTE, LOCKED_CHARACTER = "Pyrelight", "Shadestep", "Lothar"
PRESENT, LATE, ABSENT, WAITING = "Present", "Late", "Absent", "Not checked in"
TONES = {PRESENT: "success", LATE: "warning", ABSENT: "danger", WAITING: "neutral"}
STATUS = {"Brookpaw": (LATE, "expected 21:15"), ABSENT_CHARACTER: (ABSENT, "no reply"), "Swiftshot": (WAITING, ""),
          "Stormcall": (WAITING, "")}

CHECK_IN = "1 · Check-in"
SWAP = "2 · Substitute swap"
REFUSED = "3 · Swap refused"
LOG = "4 · After the raid"

TOP = CONTENT_TOP + 120
ROW_H = 36


def status_of(slot: Slot) -> tuple[str, str]:
    return STATUS.get(slot.character, (PRESENT, "20:52"))


def attendance_row(name: str, x: float, y: float, width: float, slot: Slot, linked: bool) -> Group:
    state, detail = status_of(slot)
    items: list[Item] = [
        Rect("Divider", x, y, width, 1, DIVIDER),
        Circle("Class colour", x + 14, y + 18, 4, CLASS_COLOURS[slot.wow_class]),
        text("Character", x + 26, y + 23, slot.character, 13, 600, TEXT),
        text("Player", x + 130, y + 23, slot.player, 12, 400, MUTED),
        badge("Status", x + 260, y + 6, state, TONES[state]),
    ]
    if state == ABSENT:
        items.append(button("Substitute button", x + 384, y + 0, "Find a substitute", "secondary", 140,
                            Click("navigate", SWAP) if linked else None))
    else:
        items.append(text("Detail", x + 400, y + 23, detail, 12, 400, SECONDARY))
    return Group(name, items)


def check_in(linked: bool = True) -> list[Item]:
    places = [(group, slot) for group, members in enumerate(roster(), 1) for slot in members]
    counts = {state: sum(1 for _, slot in places if status_of(slot)[0] == state) for state in TONES}
    column_w = (CONTENT_W - 16) / 2
    items: list[Item] = [badge(f"{state} count", CONTENT_X + index * 160, TOP, f"{count} {state.lower()}", TONES[state])
                         for index, (state, count) in enumerate(counts.items())]
    half = (len(places) + 1) // 2
    for column, chunk in enumerate((places[:half], places[half:])):
        x = CONTENT_X + column * (column_w + 16)
        rows: list[Item] = [*card(x, TOP + 40, column_w, 16 + ROW_H * len(chunk))]
        rows += [attendance_row(f"Place {group}.{index + 1}", x + 8, TOP + 48 + index * ROW_H, column_w - 16, slot,
                                linked) for index, (group, slot) in enumerate(chunk)]
        items.append(Group(f"Column {column + 1}", rows))
    bench_y = TOP + 40 + 16 + ROW_H * half + 16
    items.append(Group("Bench", [
        *card(CONTENT_X, bench_y, CONTENT_W, 56),
        text("Title", CONTENT_X + 20, bench_y + 34, "Bench", 13, 700, SECONDARY),
        text("Shadestep", CONTENT_X + 90, bench_y + 34, "Shadestep (Selm Voss) · arriving 21:30", 13, 400, TEXT),
        text("Ironhide", CONTENT_X + 420, bench_y + 34, "Ironhide (Eli Brant) · online", 13, 400, TEXT),
    ]))
    return [page_header(PAGE, TITLE, WHEN), Group("Attendance", items)]


def check(name: str, x: float, y: float, title: str, detail: str, ok: bool) -> Group:
    return Group(name, [Circle("Mark circle", x + 12, y + 12, 12, P["Status/success background"] if ok
                               else P["Status/danger background"]),
                        text("Mark", x, y + 17, "✓" if ok else "!", 13, 800,
                             ACCENT if ok else P["Status/danger text"], 24, "center", icon=True),
                        text("Title", x + 36, y + 10, title, 14, 600),
                        text("Detail", x + 36, y + 30, detail, 12, 400, SECONDARY)])


def candidate(name: str, x: float, y: float, character: str, detail: str, selected: bool,
              on_click: Click | None) -> Group:
    return Group(name, [Rect("Area", x, y, 340, 52, P["Surface/selected"] if selected else P["Surface/raised"], 1, 8,
                             ACCENT if selected else DIVIDER),
                        text("Character", x + 16, y + 22, character, 14, 600, ACCENT if selected else TEXT),
                        text("Detail", x + 16, y + 41, detail, 12, 400, SECONDARY)], on_click)


def swap_screen(refused: bool) -> list[Item]:
    left_w = 380
    choices: list[Item] = [*card(CONTENT_X, TOP + 96, left_w, 260),
                           text("Title", CONTENT_X + 20, TOP + 130, "Substitutes", 16, 600),
                           candidate("Shadestep option", CONTENT_X + 20, TOP + 148, SUBSTITUTE,
                                     "Selm Voss · bench · Combat rogue", not refused,
                                     Click("navigate", SWAP) if refused else None),
                           candidate("Lothar option", CONTENT_X + 20, TOP + 208, LOCKED_CHARACTER,
                                     "Tomas Hale · signed up · Protection paladin", refused,
                                     None if refused else Click("navigate", REFUSED)),
                           candidate("Ironhide option", CONTENT_X + 20, TOP + 268, "Ironhide",
                                     "Eli Brant · bench · Protection warrior", False, None)]
    panel_x, panel_w = CONTENT_X + left_w + 20, CONTENT_W - left_w - 20
    if refused:
        checks = [check("Ownership check", panel_x + 20, TOP + 152, "Ownership", "Approved for Tomas Hale", True),
                  check("Lockout check", panel_x + 20, TOP + 208, "Locked through raid",
                        "Icecrown Citadel 25 heroic save until Wed 14 Oct · addon, today 14:05", False)]
        action: list[Item] = [text("No override", panel_x + 20, TOP + 300,
                                   "A locked character can't be swapped in or invited. Choose another substitute.",
                                   13, 400, P["Status/danger text"]),
                              button("Back button", panel_x + 20, TOP + 320, "Choose another", "secondary", 160,
                                     Click("navigate", SWAP))]
    else:
        checks = [check("Ownership check", panel_x + 20, TOP + 152, "Ownership", "Approved for Selm Voss", True),
                  check("Lockout check", panel_x + 20, TOP + 208, "Available",
                        "No Icecrown Citadel or Ruby Sanctum save · addon, today 20:40", True),
                  check("Availability check", panel_x + 20, TOP + 264, "Arriving late", "Signed up late, arriving 21:30",
                        True)]
        action = [button("Swap button", panel_x + 20, TOP + 320, f"Swap in {SUBSTITUTE}", "primary", 200,
                         Click("navigate", LOG))]
    picked = LOCKED_CHARACTER if refused else SUBSTITUTE
    panel: list[Item] = [*card(panel_x, TOP + 96, panel_w, 300),
                         text("Title", panel_x + 20, TOP + 130, f"Checked again for {picked}", 16, 600), *checks, *action]
    return [
        page_header(PAGE, f"Replace {ABSENT_CHARACTER}", "Faye Lorn is absent · group 5 · Fire mage"),
        notice("Absent notice", CONTENT_X, TOP, CONTENT_W, "Pyrelight hasn't checked in",
               "Pick a substitute. Each one is checked again for ownership, lockout and availability before the swap.",
               "warning"),
        Group("Substitutes", choices), Group("Checks", panel),
    ]


LOG_ROWS = [
    ("20:45", OFFICER_NAME, "Opened check-in for the published roster (version 2)"),
    ("20:52", OFFICER_NAME, "Marked 21 players present"),
    ("20:58", OFFICER_NAME, "Marked Pyrelight (Faye Lorn) absent"),
    ("21:01", "RaidManager", "Refused Lothar as a substitute: locked through raid"),
    ("21:02", OFFICER_NAME, "Swapped in Shadestep (Selm Voss) for Pyrelight in group 5"),
    ("21:05", OFFICER_NAME, "Marked Swiftshot and Stormcall present"),
    ("21:15", OFFICER_NAME, "Marked Brookpaw present, arrived late"),
    ("21:31", OFFICER_NAME, "Marked Shadestep present, arrived late"),
]


def log() -> list[Item]:
    items: list[Item] = [*card(CONTENT_X, TOP, CONTENT_W, 52 + 44 * len(LOG_ROWS) + 8),
                         text("Title", CONTENT_X + 20, TOP + 34, "Attendance and changes", 16, 600)]
    for index, (when, who, what) in enumerate(LOG_ROWS):
        y = TOP + 52 + index * 44
        items.append(Group(f"Entry {index + 1}", [
            Rect("Divider", CONTENT_X, y, CONTENT_W, 1, DIVIDER),
            text("When", CONTENT_X + 20, y + 28, when, 13, 600, SECONDARY),
            text("What", CONTENT_X + 90, y + 28, what, 14),
            text("Who", CONTENT_X + CONTENT_W - 220, y + 28, who, 12, 400, MUTED, 200, "right"),
        ]))
    return [page_header(PAGE, "Icecrown Citadel · Fri 9 Oct", "Completed · 25 attended, 1 absent, 1 swap"),
            Group("Night log", items),
            text("History note", CONTENT_X, TOP + 52 + 44 * len(LOG_ROWS) + 40,
                 "This log stays with the raid and feeds the attendance history.", 13, 400, MUTED)]


def boards() -> list[Board]:
    right, below = BOARD_W + BOARD_GAP, BOARD_H + BOARD_GAP

    def screen(name: str, x: float, y: float, content: list[Item]) -> Board:
        return app_screen(name, x, y, PAGE, content, section="Tonight")

    return [screen(CHECK_IN, 0, 0, check_in()), screen(SWAP, right, 0, swap_screen(False)),
            screen(REFUSED, 0, below, swap_screen(True)), screen(LOG, right, below, log())]


def main(repository: Path = REPOSITORY) -> Path:
    return write_mockup(repository, "raid-night-attendance", "Raid-night attendance", boards(), PALETTE,
                        {"Check in and swap": CHECK_IN, "Refused swap": REFUSED})


if __name__ == "__main__":
    print("Wrote", main())
