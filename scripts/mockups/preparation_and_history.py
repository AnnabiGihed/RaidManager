"""Gear warnings and attendance history (story #36, design story #196, task #247).

Four boards in RaidManager's design system (ADR-0019), seen by an officer, linked as a clickable prototype. The players
come from the earlier roster mockups:

1. Gear warnings for the roster: missing enchants and empty sockets from the synced item links, per character.
   Inspect on Bladewind opens 2.
2. Bladewind's loadout inspected: each slot's item, item level, enchant and gems, with the data source and time.
   Back opens 1.
3. The raid's gear requirements and GearScore, advisory only: the characters below them stay selectable and their
   readiness is unchanged.
4. Attendance history across the last raids, each record with its source and recording time.

Run from the repository root: python scripts/mockups/preparation_and_history.py
Once the owner edits the design in Penpot, the downloaded file replaces docs/mockups/preparation-and-history.penpot
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
from roster_data import CLASS_COLOURS, PALETTE_ADDITIONS, WHEN  # noqa: E402

P = HOUSE_PALETTE
PALETTE = {**HOUSE_PALETTE, **PALETTE_ADDITIONS}
ACCENT, SECONDARY, MUTED, DIVIDER = P["Brand/accent"], P["Text/secondary"], P["Text/muted"], P["Line/divider"]
TEXT = P["Text/primary"]
PAGE = "Raid night"
INSPECTED = "Bladewind"
FROM_ADDON = "WoW addon · today 14:05"

WARNINGS = "1 · Gear warnings"
INSPECT = "2 · Inspect a loadout"
REQUIREMENTS = "3 · Advisory requirements"
HISTORY = "4 · Attendance history"

TOP = CONTENT_TOP + 120
# character, player, class, GearScore, missing enchants, empty sockets
GEAR = [
    ("Bladewind", "Ivo Marsh", "Warrior", "5,380", 2, 1),
    ("Brookpaw", "Hale Brook", "Druid", "5,450", 0, 2),
    ("Mendara", "Nia Holt", "Shaman", "5,210", 1, 0),
    ("Stonebrow", "Doran Pike", "Warrior", "5,650", 0, 0),
    ("Arthasdk", "Bryn Valewood", "Death Knight", "5,712", 0, 0),
    ("Kirilight", "Kiri Dawn", "Priest", "5,420", 0, 0),
]


def label(name: str, x: float, y: float, value: str) -> Item:
    return text(name, x, y, value, 11, 700, MUTED, None, "left", 1.2)


def gear_warnings() -> list[Item]:
    head_h, row_h = 44, 60
    columns = {"character": 24, "score": 320, "enchants": 460, "sockets": 680, "action": 980}
    items: list[Item] = [*card(CONTENT_X, TOP + 40, CONTENT_W, head_h + row_h * len(GEAR) + 8)]
    items.append(Group("Table header", [
        label(f"{heading.title()} heading", CONTENT_X + columns[key], TOP + 40 + 27, heading)
        for key, heading in (("character", "CHARACTER"), ("score", "GEARSCORE"), ("enchants", "ENCHANTS"),
                             ("sockets", "GEMS"))]))
    for index, (character, player, wow_class, score, enchants, sockets) in enumerate(GEAR):
        y = TOP + 40 + head_h + index * row_h
        row: list[Item] = [
            Rect("Divider", CONTENT_X, y, CONTENT_W, 1, DIVIDER),
            Circle("Class colour", CONTENT_X + 29, y + 24, 5, CLASS_COLOURS[wow_class]),
            text("Character", CONTENT_X + 42, y + 28, character, 14, 600),
            text("Player", CONTENT_X + 42, y + 46, player, 12, 400, MUTED),
            text("Score", CONTENT_X + columns["score"], y + 36, score, 14),
            badge("Enchants", CONTENT_X + columns["enchants"], y + 18,
                  f"{enchants} missing" if enchants else "All enchanted", "warning" if enchants else "success"),
            badge("Sockets", CONTENT_X + columns["sockets"], y + 18,
                  f"{sockets} empty socket" + ("s" if sockets > 1 else "") if sockets else "All filled",
                  "warning" if sockets else "success"),
        ]
        if enchants or sockets:
            row.append(button("Inspect button", CONTENT_X + columns["action"], y + 10, "Inspect", "secondary", 104,
                              Click("navigate", INSPECT) if character == INSPECTED else None))
        items.append(Group(f"Row {character}", row))
    return [
        page_header(PAGE, "Gear before the raid", WHEN),
        text("Summary", CONTENT_X, TOP + 17, "3 characters have missing enchants or empty sockets · from synced item links",
             13, 400, SECONDARY),
        Group("Gear table", items),
        text("Advisory", CONTENT_X, TOP + 40 + head_h + row_h * len(GEAR) + 40,
             "Gear warnings are advice for you and the player; they never remove anyone from the roster.", 13, 400, MUTED),
    ]


# slot, item, item level, enchant, gems (filled, sockets)
SLOTS = [
    ("Head", "Sanctified Ymirjar Lord's Helmet", "264", "Arcanum of Torment", (2, 2)),
    ("Shoulder", "Sanctified Ymirjar Lord's Shoulderplates", "264", "Greater Inscription of the Axe", (1, 1)),
    ("Chest", "Sanctified Ymirjar Lord's Battleplate", "264", "", (1, 2)),
    ("Hands", "Sanctified Ymirjar Lord's Gauntlets", "264", "Crusher", (1, 1)),
    ("Legs", "Sanctified Ymirjar Lord's Legplates", "264", "Icescale Leg Armor", (2, 2)),
    ("Feet", "Apocalypse's Advance", "264", "", (1, 1)),
    ("Main hand", "Cryptmaker", "264", "Berserking", (0, 0)),
    ("Off hand", "Bryntroll, the Bone Arbiter", "264", "Berserking", (1, 1)),
]


def inspect() -> list[Item]:
    row_h = 48
    columns = {"slot": 24, "item": 130, "level": 470, "enchant": 540, "gems": 860}
    items: list[Item] = [*card(CONTENT_X, TOP, CONTENT_W, 52 + 44 + row_h * len(SLOTS) + 8),
                         text("Title", CONTENT_X + 20, TOP + 34, "Bladewind · Fury · GearScore 5,380", 16, 600),
                         text("Source", CONTENT_X + CONTENT_W - 320, TOP + 34, f"Source: {FROM_ADDON}", 12, 400, MUTED,
                              300, "right")]
    items.append(Group("Table header", [
        label(f"{heading.title()} heading", CONTENT_X + columns[key], TOP + 52 + 27, heading)
        for key, heading in (("slot", "SLOT"), ("item", "ITEM"), ("level", "ILVL"), ("enchant", "ENCHANT"),
                             ("gems", "GEMS"))]))
    for index, (slot, item, level, enchant, (filled, sockets)) in enumerate(SLOTS):
        y = TOP + 96 + index * row_h
        row: list[Item] = [Rect("Divider", CONTENT_X, y, CONTENT_W, 1, DIVIDER),
                           text("Slot", CONTENT_X + columns["slot"], y + 30, slot, 12, 400, MUTED),
                           text("Item", CONTENT_X + columns["item"], y + 30, item, 13, 600),
                           text("Level", CONTENT_X + columns["level"], y + 30, level, 13, 400, SECONDARY)]
        if enchant:
            row.append(text("Enchant", CONTENT_X + columns["enchant"], y + 30, enchant, 13, 400, TEXT))
        else:
            row.append(badge("Enchant", CONTENT_X + columns["enchant"], y + 12, "No enchant", "warning"))
        if sockets == 0:
            row.append(text("Gems", CONTENT_X + columns["gems"], y + 30, "No sockets", 12, 400, MUTED))
        elif filled < sockets:
            row.append(badge("Gems", CONTENT_X + columns["gems"], y + 12, f"{filled} of {sockets} gems", "warning"))
        else:
            row.append(text("Gems", CONTENT_X + columns["gems"], y + 30, f"{filled} of {sockets} gems", 13, 400, TEXT))
        items.append(Group(f"Slot {slot}", row))
    return [
        page_header(PAGE, "Inspect Bladewind", "Ivo Marsh · primary loadout"),
        Group("Loadout", items),
        button("Back button", CONTENT_X, TOP + 52 + 44 + row_h * len(SLOTS) + 32, "Back to gear", "secondary", 136,
               Click("navigate", WARNINGS)),
    ]


def requirements() -> list[Item]:
    below = [("Mendara", "Nia Holt", "Shaman", "5,210", "Available"), ("Bladewind", "Ivo Marsh", "Warrior", "5,380",
                                                                       "Available")]
    left_w = 400
    rules: list[Item] = [*card(CONTENT_X, TOP + 96, left_w, 220),
                         text("Title", CONTENT_X + 20, TOP + 130, "This raid's gear requirements", 16, 600),
                         text("Rule 1", CONTENT_X + 20, TOP + 162, "GearScore 5,500 or more", 14),
                         text("Rule 2", CONTENT_X + 20, TOP + 188, "Every enchantable item enchanted", 14),
                         text("Rule 3", CONTENT_X + 20, TOP + 214, "Every socket filled", 14),
                         text("Edit", CONTENT_X + 20, TOP + 254, "Edit requirements in the raid", 13, 600, ACCENT)]
    list_x, list_w = CONTENT_X + left_w + 20, CONTENT_W - left_w - 20
    rows: list[Item] = [*card(list_x, TOP + 96, list_w, 52 + 64 * len(below) + 8),
                        text("Title", list_x + 20, TOP + 130, "Below the GearScore requirement", 16, 600)]
    for index, (character, player, wow_class, score, verdict) in enumerate(below):
        y = TOP + 148 + index * 64
        rows.append(Group(f"Row {character}", [
            Rect("Divider", list_x, y, list_w, 1, DIVIDER),
            Circle("Class colour", list_x + 25, y + 26, 5, CLASS_COLOURS[wow_class]),
            text("Character", list_x + 38, y + 30, character, 14, 600),
            text("Player", list_x + 38, y + 48, player, 12, 400, MUTED),
            badge("Score", list_x + 220, y + 18, f"GS {score} · advisory", "warning"),
            badge("Readiness", list_x + 400, y + 18, verdict, "success"),
            text("Selectable", list_x + list_w - 160, y + 36, "Still selectable", 12, 400, SECONDARY, 140, "right"),
        ]))
    return [
        page_header(PAGE, "Gear requirements", WHEN),
        notice("Advisory notice", CONTENT_X, TOP, CONTENT_W, "Requirements are advice, not rules",
               "They never exclude anyone, never change readiness and never override a lockout. You decide.", "info"),
        Group("Requirements", rules), Group("Below requirements", rows),
    ]


RAIDS = ["25 Sep", "26 Sep", "2 Oct", "3 Oct", "9 Oct"]
MARKS = {"P": ("Present", P["Status/success background"], ACCENT), "L": ("Late", P["Status/warning background"],
                                                                         P["Status/warning title"]),
         "A": ("Absent", P["Status/danger background"], P["Status/danger text"]),
         "B": ("Bench", P["Surface/raised"], SECONDARY)}
HISTORY_ROWS = [("Doran Pike", "Stonebrow", "PPPPP"), ("Bryn Valewood", "Arthasdk", "PPLPP"),
                ("Kiri Dawn", "Kirilight", "BPPBP"), ("Selm Voss", "Shadestep", "PLPBL"),
                ("Faye Lorn", "Pyrelight", "PPAPA"), ("Hale Brook", "Brookpaw", "PPPPL")]


def history() -> list[Item]:
    head_h, row_h = 52, 52
    first_x = CONTENT_X + 340
    items: list[Item] = [*card(CONTENT_X, TOP + 40, CONTENT_W, head_h + row_h * len(HISTORY_ROWS) + 8),
                         label("Player heading", CONTENT_X + 24, TOP + 40 + 32, "PLAYER")]
    items += [label(f"Raid {index + 1} heading", first_x + index * 90, TOP + 40 + 32, raid.upper())
              for index, raid in enumerate(RAIDS)]
    items.append(label("Rate heading", CONTENT_X + 820, TOP + 40 + 32, "ATTENDED"))
    for index, (player, character, marks) in enumerate(HISTORY_ROWS):
        y = TOP + 40 + head_h + index * row_h
        row: list[Item] = [Rect("Divider", CONTENT_X, y, CONTENT_W, 1, DIVIDER),
                           text("Player", CONTENT_X + 24, y + 24, player, 14, 600),
                           text("Character", CONTENT_X + 24, y + 42, character, 12, 400, MUTED)]
        for position, mark in enumerate(marks):
            _, fill, colour = MARKS[mark]
            cx = first_x + position * 90 + 14
            row += [Circle(f"Mark {position + 1}", cx, y + 26, 14, fill),
                    text(f"Mark {position + 1} letter", cx - 14, y + 31, mark, 12, 700, colour, 28, "center")]
        attended = sum(1 for mark in marks if mark in "PL")
        row.append(text("Rate", CONTENT_X + 820, y + 31, f"{attended} of {len(marks)}", 14, 600))
        items.append(Group(f"Row {player}", row))
    legend_y = TOP + 40 + head_h + row_h * len(HISTORY_ROWS) + 32
    legend: list[Item] = [text("Legend", CONTENT_X, legend_y, "  ·  ".join(f"{key} {name}" for key, (name, _, _) in MARKS.items()),
                   13, 400, SECONDARY),
              text("Source", CONTENT_X, legend_y + 24,
                   "Fri 9 Oct, Faye Lorn: Absent · recorded at 20:58 by Gihed Annabi during raid-night check-in", 13,
                   400, TEXT),
              text("Source 2", CONTENT_X, legend_y + 46,
                   "Sat 3 Oct, Kiri Dawn: Bench · entered after the raid at 23:40 by Tomas Hale", 13, 400, TEXT)]
    return [page_header("Raids", "Attendance history", "Citadel Vanguard · the last 5 raids"),
            text("Summary", CONTENT_X, TOP + 17, "Each mark keeps its source and when it was recorded.", 13, 400,
                 SECONDARY),
            Group("History table", items), Group("Legend and sources", legend)]


def boards() -> list[Board]:
    right, below = BOARD_W + BOARD_GAP, BOARD_H + BOARD_GAP

    def screen(name: str, x: float, y: float, content: list[Item], section: str, page: str = PAGE) -> Board:
        return app_screen(name, x, y, page, content, section=section)

    return [screen(WARNINGS, 0, 0, gear_warnings(), "Preparation"), screen(INSPECT, right, 0, inspect(), "Preparation"),
            screen(REQUIREMENTS, 0, below, requirements(), "Preparation"),
            screen(HISTORY, right, below, history(), "Attendance", "Raids")]


def main(repository: Path = REPOSITORY) -> Path:
    return write_mockup(repository, "preparation-and-history", "Preparation and history", boards(), PALETTE,
                        {"Inspect gear": WARNINGS, "Attendance history": HISTORY})


if __name__ == "__main__":
    print("Wrote", main())
