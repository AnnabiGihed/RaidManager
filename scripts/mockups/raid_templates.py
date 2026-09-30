"""Raid templates, weekly recurrence and copying a raid (story #21, design story #184, task #223).

Four boards in RaidManager's design system (ADR-0019), seen by an officer, linked as a clickable prototype:

1. A template: raid details, targets, requirements and its weekly recurrence. Generate raids opens 2.
2. Reviewing the generated raids before they're created: date in local time and UTC, reset context, targets, and
   one week that already has the raid, so it's skipped rather than duplicated. Back to template returns to 1.
3. Copying a previous raid: the new date, whether to start from its roster, and what is and isn't copied.
   Create draft opens 4.
4. The new draft: signups start empty, and the copied roster's readiness is checked again, not copied.

Run from the repository root: python scripts/mockups/raid_templates.py
Once the owner edits the design in Penpot, the downloaded file replaces docs/mockups/raid-templates.penpot and this
script is no longer run.
"""

from __future__ import annotations

import sys
from pathlib import Path

REPOSITORY = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(REPOSITORY / "scripts"))

from penpot_components import (  # noqa: E402
    BOARD_GAP, BOARD_H, BOARD_W, CONTENT_TOP, CONTENT_W, CONTENT_X, app_screen, badge, button, card, checkbox,
    form_field, notice, page_header,
)
from penpot_scene import HOUSE_PALETTE, Board, Circle, Click, Group, Item, Rect, text, write_mockup  # noqa: E402

P = HOUSE_PALETTE
ACCENT, SECONDARY, MUTED, DIVIDER = P["Brand/accent"], P["Text/secondary"], P["Text/muted"], P["Line/divider"]
CLASS_COLOURS = {"Death Knight": "#C41E3A", "Priest": "#FFFFFF", "Paladin": "#F48CBA", "Mage": "#3FC7EB",
                 "Rogue": "#FFF468"}
PALETTE = {**HOUSE_PALETTE, **{f"WoW class/{name}": colour for name, colour in CLASS_COLOURS.items()}}

PAGE = "Schedule"
ICC = "Icecrown Citadel"
TEMPLATE_NAME = "ICC 25 weekly"
TARGETS = "Icecrown Citadel 25 heroic + Ruby Sanctum 25"
TWENTY_FIVE = "25 players"
START = "20:00"
UTC_18 = "18:00 UTC"

TEMPLATE = "1 · Template with weekly recurrence"
REVIEW = "2 · Review generated raids"
COPY = "3 · Copy a previous raid"
DRAFT = "4 · Draft copied from a raid"

TOP = CONTENT_TOP + 120


def label(name: str, x: float, y: float, value: str) -> Item:
    return text(name, x, y, value, 11, 700, MUTED, None, "left", 1.2)


def template() -> list[Item]:
    left_x, right_x = CONTENT_X, CONTENT_X + 560
    x = left_x + 20
    raid: list[Item] = [
        *card(left_x, TOP, 540, 420), text("Raid title", x, TOP + 34, "Raid", 16, 600),
        form_field("Title field", x, TOP + 76, 500, "Title", ICC),
        form_field("Description field", x, TOP + 156, 500, "Description", "Weekly progression, then Halion if time allows."),
    ]
    for index, (instance, difficulty) in enumerate(((ICC, "25 players, heroic"), ("Ruby Sanctum", TWENTY_FIVE))):
        y = TOP + 236 + index * 80
        raid.append(Group(f"Target {index + 1}", [
            form_field("Instance field", x, y, 280, "Instance", instance, True),
            form_field("Difficulty field", x + 296, y, 204, "Difficulty", difficulty, True),
        ]))
    raid.append(text("Size", x, TOP + 400, f"Size: {TWENTY_FIVE}, from the targets", 12, 400, MUTED))
    recurrence: list[Item] = [
        *card(right_x, TOP, 560, 300), text("Recurrence title", right_x + 20, TOP + 34, "Weekly recurrence", 16, 600),
        form_field("Weekday field", right_x + 20, TOP + 76, 250, "Every", "Friday", True),
        form_field("Start field", right_x + 290, TOP + 76, 250, "Starts at", START, True),
        form_field("Deadline field", right_x + 20, TOP + 156, 250, "Signups close", "2 hours before", True),
        form_field("Ahead field", right_x + 290, TOP + 156, 250, "Create raids ahead", "4 weeks", True),
        text("Time note", right_x + 20, TOP + 260, "Times are in your time zone; each raid keeps its exact UTC time,", 12,
             400, MUTED),
        text("Time note 2", right_x + 20, TOP + 278, "even across a clock change.", 12, 400, MUTED),
    ]
    requirements_top = TOP + 316
    requirements: list[Item] = [
        *card(right_x, requirements_top, 560, 104),
        text("Requirements title", right_x + 20, requirements_top + 34, "Readiness requirements", 16, 600),
        text("Requirements", right_x + 20, requirements_top + 62,
             "GearScore 5,500 or more · data no older than 3 days", 13, 400, SECONDARY),
        text("Requirements 2", right_x + 20, requirements_top + 84, "Only characters without a save for these targets",
             13, 400, SECONDARY),
    ]
    return [
        page_header("Template", TEMPLATE_NAME, "Creates a draft raid each week for you to review before signups open."),
        Group("Raid card", raid), Group("Recurrence card", recurrence), Group("Requirements card", requirements),
        button("Generate button", right_x, TOP + 444, "Generate raids", "primary", 160, Click("navigate", REVIEW)),
        button("Save button", right_x + 168, TOP + 444, "Save template", "secondary", 144),
    ]


def review() -> list[Item]:
    head_h, row_h = 44, 76
    columns = {"include": 24, "date": 64, "reset": 330, "targets": 560, "result": 900}
    rows = [
        ("Fri 9 Oct, 20:00", UTC_18, "Lockout week from Wed 7 Oct", "new", ""),
        ("Fri 16 Oct, 20:00", UTC_18, "Lockout week from Wed 14 Oct", "exists", ""),
        ("Fri 23 Oct, 20:00", UTC_18, "Lockout week from Wed 21 Oct", "new", ""),
        ("Fri 30 Oct, 20:00", "19:00 UTC", "Lockout week from Wed 28 Oct", "new", "Clocks changed on 25 Oct"),
    ]
    table: list[Item] = card(CONTENT_X, TOP, CONTENT_W, head_h + row_h * len(rows) + 8)
    table.append(Group("Table header", [
        label(f"{heading.title()} heading", CONTENT_X + columns[key], TOP + 27, heading)
        for key, heading in (("date", "DATE"), ("reset", "RESET"), ("targets", "TARGETS"), ("result", "RESULT"))]))
    for index, (local, utc, reset, state, note) in enumerate(rows):
        y = TOP + head_h + index * row_h
        new = state == "new"
        row: list[Item] = [
            Rect("Divider", CONTENT_X, y, CONTENT_W, 1, DIVIDER),
            *checkbox(CONTENT_X + columns["include"], y + 29, new),
            text("Local", CONTENT_X + columns["date"], y + 33, local, 14, 600, P["Text/primary"] if new else MUTED),
            text("UTC", CONTENT_X + columns["date"], y + 53, f"{utc} · {note}" if note else utc, 12, 400, MUTED),
            text("Reset", CONTENT_X + columns["reset"], y + 42, reset, 13, 400, SECONDARY),
            text("Targets", CONTENT_X + columns["targets"], y + 42, TARGETS, 13, 400, SECONDARY),
            badge("Result badge", CONTENT_X + columns["result"], y + 26, "New draft" if new else "Already exists",
                  "info" if new else "neutral"),
        ]
        if not new:
            row.append(text("Skipped", CONTENT_X + columns["result"], y + 64, "Skipped, not duplicated", 11, 400, MUTED))
        table.append(Group(f"Row {local}", row))
    buttons_y = TOP + head_h + row_h * len(rows) + 32
    return [
        page_header("Template", "Review generated raids", f"{TEMPLATE_NAME} · the next 4 Fridays. They're created as drafts."),
        Group("Generated raids", table),
        button("Create button", CONTENT_X, buttons_y, "Create 3 drafts", "primary", 160),
        button("Back button", CONTENT_X + 168, buttons_y, "Back to template", "secondary", 168, Click("navigate", TEMPLATE)),
    ]


def copy_dialog() -> list[Item]:
    list_items: list[Item] = [*card(CONTENT_X, TOP, CONTENT_W, 196), label("Raid heading", CONTENT_X + 24, TOP + 27, "RAID")]
    for index, (title, when) in enumerate(((ICC, "Fri 25 Sep, 20:00 · Completed"),
                                           ("Weekly Vault run", "Sat 26 Sep, 21:00 · Completed"))):
        y = TOP + 44 + index * 72
        list_items.append(Group(f"Row {title}", [
            Rect("Divider", CONTENT_X, y, CONTENT_W, 1, DIVIDER),
            text("Title", CONTENT_X + 24, y + 32, title, 14, 600),
            text("When", CONTENT_X + 24, y + 52, when, 12, 400, MUTED),
            button("Copy button", CONTENT_X + CONTENT_W - 104, y + 16, "Copy", "secondary", 80),
        ]))
    w, h = 520, 360
    dialog_x, dialog_y = (BOARD_W - w) / 2, (BOARD_H - h) / 2
    copied = ["Copied: title, description, targets and requirements.",
              "Not copied: signups and readiness verdicts. They're checked",
              "again for the new date."]
    return [
        page_header("Schedule", "Past raids", "Copy a raid to start a new draft from it."),
        Group("Past raids", list_items),
        Rect("Dim overlay", 0, 0, BOARD_W, BOARD_H, P["Neutral/black"], 0.6),
        Group("Copy dialog", [
            *card(dialog_x, dialog_y, w, h),
            text("Title", dialog_x + 24, dialog_y + 44, f"Copy {ICC}", 18, 700),
            text("Source", dialog_x + 24, dialog_y + 66, "Fri 25 Sep, 20:00", 13, 400, SECONDARY),
            form_field("Date field", dialog_x + 24, dialog_y + 104, 228, "New date", "Fri 9 Oct", True),
            form_field("Time field", dialog_x + 268, dialog_y + 104, 228, "Starts at", START, True),
            *checkbox(dialog_x + 24, dialog_y + 176, True),
            text("Roster option", dialog_x + 52, dialog_y + 190, "Start from its roster (22 players)", 14),
            *[text(f"Copy rule {index + 1}", dialog_x + 24, dialog_y + 228 + index * 18, line, 12, 400, MUTED)
              for index, line in enumerate(copied)],
            button("Cancel button", dialog_x + w - 24 - 136 - 8 - 96, dialog_y + h - 64, "Cancel", "secondary", 96),
            button("Create button", dialog_x + w - 24 - 136, dialog_y + h - 64, "Create draft", "primary", 136, Click("navigate", DRAFT)),
        ]),
    ]


ROSTER = [("Tomas Hale", "Death Knight", "Tank"), ("Kiri Dawn", "Priest", "Healer"), ("Arvel Moss", "Paladin", "Healer"),
          ("Bryn Valewood", "Mage", "Ranged damage"), ("Selm Voss", "Rogue", "Melee damage")]


def draft() -> list[Item]:
    top = TOP + 88
    rows: list[Item] = [*card(CONTENT_X, top, 720, 60 + 52 * len(ROSTER) + 44),
                        text("Roster title", CONTENT_X + 20, top + 36, "Roster draft · 22 players", 16, 600)]
    for index, (name, wow_class, role) in enumerate(ROSTER):
        y = top + 60 + index * 52
        rows.append(Group(f"Row {name}", [
            Rect("Divider", CONTENT_X, y, 720, 1, DIVIDER),
            Circle("Class colour", CONTENT_X + 25, y + 26, 5, CLASS_COLOURS[wow_class]),
            text("Name", CONTENT_X + 40, y + 31, name, 14, 600),
            text("Role", CONTENT_X + 260, y + 31, f"{wow_class} · {role}", 13, 400, SECONDARY),
            badge("Readiness badge", CONTENT_X + 540, y + 14, "Checking again", "neutral"),
        ]))
    rows.append(text("More", CONTENT_X + 20, top + 60 + 52 * len(ROSTER) + 28, "17 more players", 12, 400, MUTED))
    return [
        page_header("Draft · copied", ICC, "Fri 9 Oct, 20:00 · 18:00 UTC · not open for signups yet"),
        button("Open button", CONTENT_X + CONTENT_W - 168, CONTENT_TOP + 28, "Open for signups", "primary", 168),
        notice("Copied notice", CONTENT_X, TOP, CONTENT_W, "Copied from Fri 25 Sep",
               "Signups start empty. The roster is a draft, and each player's readiness is checked again for 9 Oct.",
               "info"),
        Group("Roster draft", rows),
    ]


def boards() -> list[Board]:
    right, below = BOARD_W + BOARD_GAP, BOARD_H + BOARD_GAP

    def screen(name: str, x: float, y: float, content: list[Item], section: str) -> Board:
        return app_screen(name, x, y, PAGE, content, section=section)

    return [screen(TEMPLATE, 0, 0, template(), "Templates"), screen(REVIEW, right, 0, review(), "Templates"),
            screen(COPY, 0, below, copy_dialog(), PAGE), screen(DRAFT, right, below, draft(), PAGE)]


def main(repository: Path = REPOSITORY) -> Path:
    return write_mockup(repository, "raid-templates", "Raid templates", boards(), PALETTE,
                        {"Generate weekly raids": TEMPLATE, "Copy a previous raid": COPY})


if __name__ == "__main__":
    print("Wrote", main())
