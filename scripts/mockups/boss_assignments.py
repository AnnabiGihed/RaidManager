"""Boss assignments (story #34, design story #194, task #243).

Four boards in RaidManager's design system (ADR-0019), linked as a clickable prototype. Each boss has assignment
rows: a task and the roster characters assigned to it. The characters are the published roster from the earlier
roster mockups:

1. An officer defining Deathbringer Saurfang's assignments, with the raid's bosses on the left. Copy from a prior
   run opens 2.
2. Assignments copied from the run on Fri 25 Sep, still editable: one copied character isn't in this roster, so
   that place waits for the officer. Done opens 1.
3. A player's view: Bryn Valewood sees only Arthasdk's assignments for the raid.
4. A stale reference after raid night: Pyrelight was swapped out for Shadestep, so its assignments are flagged.
   Assign Shadestep or choose someone else; nothing changes on its own.

Run from the repository root: python scripts/mockups/boss_assignments.py
Once the owner edits the design in Penpot, the downloaded file replaces docs/mockups/boss-assignments.penpot and
this script is no longer run.
"""

from __future__ import annotations

import sys
from pathlib import Path

REPOSITORY = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(REPOSITORY / "scripts"))

from penpot_components import (  # noqa: E402
    BOARD_GAP, BOARD_H, BOARD_W, CONTENT_TOP, CONTENT_W, CONTENT_X, PLAYER, app_screen, button, card, notice,
    page_header,
)
from penpot_scene import HOUSE_PALETTE, Board, Click, Group, Item, Rect, text, write_mockup  # noqa: E402
from roster_data import PALETTE_ADDITIONS, WHEN  # noqa: E402

P = HOUSE_PALETTE
PALETTE = {**HOUSE_PALETTE, **PALETTE_ADDITIONS}
ACCENT, SECONDARY, MUTED, DIVIDER = P["Brand/accent"], P["Text/secondary"], P["Text/muted"], P["Line/divider"]
TEXT, RAISED = P["Text/primary"], P["Surface/raised"]
PAGE = "Raid night"
SECTION = "Assignments"
SAURFANG = "Deathbringer Saurfang"
STALE_CHARACTER, SUBSTITUTE = "Pyrelight", "Shadestep"
TITLE = "Boss assignments"
MARROWGAR, DEATHWHISPER = "Lord Marrowgar", "Lady Deathwhisper"
BOSSES = [MARROWGAR, DEATHWHISPER, "Gunship Battle", SAURFANG, "Festergut", "Rotface",
          "Professor Putricide", "Blood Prince Council", "Blood-Queen Lana'thel", "Valithria Dreamwalker", "Sindragosa",
          "The Lich King", "Halion"]
# task, assigned characters
SAURFANG_TASKS = [
    ("Tanks: taunt on Rune of Blood", ["Stonebrow", "Tomasdk"]),
    ("Blood Beasts: slow and kill", ["Frostweave", "Swiftshot", "Longstride"]),
    ("Mark of the Fallen Champion: heal", ["Mendara", "Lightwarden"]),
    ("Melee: stay on the boss", ["Arthasdk", "Crusader", "Ironclad"]),
]

DEFINE = "1 · Define assignments"
COPIED = "2 · Copied from a prior run"
PLAYER_VIEW = "3 · A player's assignments"
STALE = "4 · Stale after a swap"

TOP = CONTENT_TOP + 120
LIST_W = 280


def boss_list(active: str, counts: dict[str, int], top: float = TOP) -> Group:
    items: list[Item] = [*card(CONTENT_X, top, LIST_W, 52 + 36 * len(BOSSES) + 8),
                         text("Title", CONTENT_X + 20, top + 34, "Bosses", 16, 600)]
    for index, boss in enumerate(BOSSES):
        y = top + 52 + index * 36
        selected = boss == active
        items.append(Group(f"{boss} entry", [
            Rect("Area", CONTENT_X + 8, y, LIST_W - 16, 32, P["Surface/selected"] if selected else P["Surface/card"], 1, 6),
            text("Boss", CONTENT_X + 20, y + 21, boss, 13, 600 if selected else 400, ACCENT if selected else TEXT),
            text("Count", CONTENT_X + LIST_W - 60, y + 21, str(counts.get(boss, 0)) if counts.get(boss) else "", 12, 400,
                 ACCENT if selected else MUTED, 36, "right"),
        ]))
    return Group("Boss list", items)


def chip(name: str, x: float, y: float, value: str, state: str = "") -> tuple[Group, float]:
    """An assigned character; `state` "stale" or "missing" marks a reference that needs the officer."""
    width = len(value) * 7 + 28
    fill, border, colour = {"stale": (P["Status/danger background"], P["Status/danger border"], P["Status/danger text"]),
                            "missing": (P["Status/warning background"], P["Status/warning border"],
                                        P["Status/warning title"])}.get(state, (RAISED, DIVIDER, TEXT))
    return Group(name, [Rect("Background", x, y, width, 28, fill, 1, 14, border),
                        text("Label", x, y + 19, value, 12, 600, colour, width, "center")]), width


def task_rows(x: float, y: float, width: float, tasks: list[tuple[str, list[str]]],
              states: dict[str, str] | None = None) -> list[Item]:
    items: list[Item] = []
    for index, (task, characters) in enumerate(tasks):
        ry = y + index * 64
        row: list[Item] = [Rect("Divider", x, ry, width, 1, DIVIDER), text("Task", x + 20, ry + 26, task, 14, 600)]
        cx = x + 20
        for position, character in enumerate(characters):
            layer, chip_w = chip(f"Assigned {position + 1}", cx, ry + 32, character, (states or {}).get(character, ""))
            row.append(layer)
            cx += chip_w + 8
        row.append(text("Assign", cx + 4, ry + 51, "+ Assign", 12, 600, ACCENT))
        items.append(Group(f"Task {index + 1}", row))
    return items


def assignments_card(tasks: list[tuple[str, list[str]]], states: dict[str, str] | None = None,
                     subtitle: str = "", top: float = TOP) -> Group:
    x, width = CONTENT_X + LIST_W + 20, CONTENT_W - LIST_W - 20
    items: list[Item] = [*card(x, top, width, 76 + 64 * len(tasks) + 56),
                         text("Title", x + 20, top + 34, SAURFANG, 16, 600)]
    if subtitle:
        items.append(text("Subtitle", x + 20, top + 54, subtitle, 12, 400, MUTED))
    items += task_rows(x, top + 76, width, tasks, states)
    items.append(text("Add task", x + 20, top + 76 + 64 * len(tasks) + 32, "+ Add a task", 13, 600, ACCENT))
    return Group("Assignments", items)


COUNTS = {MARROWGAR: 4, DEATHWHISPER: 5, SAURFANG: 4, "Festergut": 3, "Rotface": 3, "Halion": 4}


def define() -> list[Item]:
    return [page_header(PAGE, TITLE, WHEN), boss_list(SAURFANG, COUNTS), assignments_card(SAURFANG_TASKS),
            button("Copy button", CONTENT_X + CONTENT_W - 220, CONTENT_TOP + 28, "Copy from a prior run", "secondary",
                   220, Click("navigate", COPIED))]


def copied() -> list[Item]:
    tasks = [(task, ["Lorwyn" if character == "Swiftshot" else character for character in characters])
             for task, characters in SAURFANG_TASKS]
    return [
        page_header(PAGE, TITLE, WHEN),
        notice("Copied notice", CONTENT_X, TOP, CONTENT_W, "Copied from the run on Fri 25 Sep",
               "Everything stays editable. Lorwyn isn't in this roster, so that place waits for you.", "info"),
        boss_list(SAURFANG, COUNTS, TOP + 88),
        assignments_card(tasks, {"Lorwyn": "missing"}, "Copied · Lorwyn isn't in this roster", TOP + 88),
        button("Done button", CONTENT_X + CONTENT_W - 104, CONTENT_TOP + 28, "Done", "primary", 104,
               Click("navigate", DEFINE)),
    ]


PLAYER_TASKS = [(MARROWGAR, "Bone Spikes: break them on the east side"),
                (DEATHWHISPER, "Adds: kill the fanatics on the left stairs"),
                (SAURFANG, "Melee: stay on the boss"),
                ("Professor Putricide", "Malleable Goo: call it in raid chat"),
                ("Sindragosa", "Unchained Magic: stop casting, spread out")]


def player_view() -> list[Item]:
    items: list[Item] = [*card(CONTENT_X, TOP, CONTENT_W, 52 + 56 * len(PLAYER_TASKS) + 8),
                         text("Title", CONTENT_X + 20, TOP + 34, "Arthasdk · Frost DPS", 16, 600)]
    for index, (boss, task) in enumerate(PLAYER_TASKS):
        y = TOP + 52 + index * 56
        items.append(Group(f"{boss} assignment", [
            Rect("Divider", CONTENT_X, y, CONTENT_W, 1, DIVIDER),
            text("Boss", CONTENT_X + 20, y + 34, boss, 14, 600),
            text("Task", CONTENT_X + 280, y + 34, task, 14, 400, SECONDARY),
        ]))
    return [page_header("Raids", "Your assignments", "Icecrown Citadel · Fri 9 Oct, 21:00"),
            Group("Player assignments", items),
            text("Note", CONTENT_X, TOP + 52 + 56 * len(PLAYER_TASKS) + 40,
                 "Only your own assignments show here; the full plan is on the raid page.", 13, 400, MUTED)]


def stale() -> list[Item]:
    tasks = [(task, [STALE_CHARACTER if character == "Frostweave" else character for character in characters])
             for task, characters in SAURFANG_TASKS]
    return [
        page_header(PAGE, TITLE, WHEN),
        notice("Stale notice", CONTENT_X, TOP, CONTENT_W, "Pyrelight left the roster tonight",
               "It was swapped for Shadestep at 21:02. Its 3 assignments still name it until you choose.", "danger"),
        boss_list(SAURFANG, COUNTS, TOP + 88),
        assignments_card(tasks, {STALE_CHARACTER: "stale"}, "Pyrelight is no longer in the roster", TOP + 88),
        button("Assign button", CONTENT_X + LIST_W + 20, TOP + 88 + 76 + 64 * len(tasks) + 72, f"Assign {SUBSTITUTE} instead",
               "primary", 220),
        button("Choose button", CONTENT_X + LIST_W + 248, TOP + 88 + 76 + 64 * len(tasks) + 72, "Choose someone else",
               "secondary", 200),
    ]


def boards() -> list[Board]:
    right, below = BOARD_W + BOARD_GAP, BOARD_H + BOARD_GAP

    def screen(name: str, x: float, y: float, content: list[Item]) -> Board:
        return app_screen(name, x, y, PAGE, content, section=SECTION)

    return [screen(DEFINE, 0, 0, define()), screen(COPIED, right, 0, copied()),
            app_screen(PLAYER_VIEW, 0, below, "Raids", player_view(), section=SECTION, user=PLAYER),
            screen(STALE, right, below, stale())]


def main(repository: Path = REPOSITORY) -> Path:
    return write_mockup(repository, "boss-assignments", TITLE, boards(), PALETTE,
                        {"Define and copy assignments": DEFINE, "Stale after a swap": STALE})


if __name__ == "__main__":
    print("Wrote", main())
