"""The website signup form (story #22, design story #185, task #225).

Four boards in RaidManager's design system (ADR-0019), seen by a player, linked as a clickable prototype:

1. Signing up: several characters and specs offered, one preferred, a note on one option, presets and a quick
   selection; the readiness verdict beside each option, and a character locked through the raid that can't be
   offered; availability Confirmed. Late opens 2; Sign up opens 3.
2. The same form with availability Late and its arrival time. Confirmed goes back to 1; Sign up opens 3.
3. Signed up: the offered options, availability and note, with Edit (back to 1) and Withdraw (opens 4) until
   signups close.
4. The withdraw confirmation. Cancel returns to 3; Withdraw returns to an empty form (1).

The data comes from `RaidSignup` (availability, late arrival, comment), `SignupOption` (character and loadout),
`RaidAvailability` and `ReadinessVerdict`, plus the per-option notes and presets the story adds.

Run from the repository root: python scripts/mockups/raid_signup.py
Once the owner edits the design in Penpot, the downloaded file replaces docs/mockups/raid-signup.penpot and this
script is no longer run.
"""

from __future__ import annotations

import sys
from pathlib import Path

REPOSITORY = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(REPOSITORY / "scripts"))

from penpot_components import (  # noqa: E402
    BOARD_GAP, BOARD_H, BOARD_W, CONTENT_TOP, CONTENT_X, PLAYER, app_screen, badge, button, card, checkbox,
    form_field, notice, page_header,
)
from penpot_scene import HOUSE_PALETTE, Board, Circle, Click, Group, Item, Rect, text, write_mockup  # noqa: E402

P = HOUSE_PALETTE
ACCENT, SECONDARY, MUTED, DIVIDER = P["Brand/accent"], P["Text/secondary"], P["Text/muted"], P["Line/divider"]
RAISED, SELECTED = P["Surface/raised"], P["Surface/selected"]
CLASS_COLOURS = {"Death Knight": "#C41E3A", "Mage": "#3FC7EB", "Shaman": "#0070DD", "Paladin": "#F48CBA"}
PALETTE = {**HOUSE_PALETTE, **{f"WoW class/{name}": colour for name, colour in CLASS_COLOURS.items()}}

PAGE = "Raids"
SECTION = "Sign up"
TITLE = "Sign up for Icecrown Citadel"
WHEN = "Fri 9 Oct, 20:00 · 18:00 UTC · signups close Fri 9 Oct, 18:00"
NOTE = "Can swap to Blood if we're short on tanks."
DEATH_KNIGHT = "Death Knight"
LOCKED = "Locked through raid"

FORM = "1 · Sign up, confirmed"
LATE = "2 · Sign up, late"
SIGNED_UP = "3 · Signed up"
WITHDRAW = "4 · Withdraw confirmation"

TOP = CONTENT_TOP + 120
LEFT_W, RIGHT_X, RIGHT_W = 720, CONTENT_X + 740, 380
VERDICTS = {"Available": "success", "Resets before raid": "info", "Needs fresh sync": "warning",
            LOCKED: "danger"}
# character, class, loadout, role and GearScore, verdict, offered, preferred, detail line
OPTIONS = [
    ("Arthasdk", DEATH_KNIGHT, "Frost DPS", "Melee damage · GS 5,712", "Available", True, True, ""),
    ("Arthasdk", DEATH_KNIGHT, "Blood Tank", "Tank · GS 5,480", "Available", True, False,
     "Note: only if we're short on tanks"),
    ("Jainaice", "Mage", "Fire DPS", "Ranged damage · GS 5,340", "Needs fresh sync", True, False,
     "Last synced 5 days ago; log in with it to refresh"),
    ("Thrallsham", "Shaman", "Restoration", "Healer · GS 4,120", "Resets before raid", False, False,
     "Saved now; the save resets Wed 7 Oct, before the raid"),
    ("Lothar", "Paladin", "Protection", "Tank · GS 5,600", LOCKED, False, False,
     "Saved to Icecrown Citadel 25 heroic until Wed 14 Oct; can't be offered"),
]


def label(name: str, x: float, y: float, value: str) -> Item:
    return text(name, x, y, value, 11, 700, MUTED, None, "left", 1.2)


def chip(name: str, x: float, y: float, value: str, active: bool) -> Group:
    width = len(value) * 7 + 28
    return Group(name, [Rect("Background", x, y, width, 28, SELECTED if active else RAISED, 1, 14,
                             ACCENT if active else DIVIDER),
                        text("Label", x, y + 19, value, 12, 600, ACCENT if active else SECONDARY, width, "center")])


def option_row(index: int, y: float, option: tuple) -> Group:
    name, wow_class, loadout, detail, verdict, offered, preferred, note = option
    locked = verdict == LOCKED
    x = CONTENT_X
    items: list[Item] = [Rect("Divider", x, y, LEFT_W, 1, DIVIDER)]
    items += checkbox(x + 20, y + 21, offered) if not locked else [
        Rect("Checkbox", x + 20, y + 21, 18, 18, P["Surface/card"], 1, 4, DIVIDER)]
    colour = MUTED if locked else P["Text/primary"]
    items += [
        Circle("Class colour", x + 58, y + 30, 5, CLASS_COLOURS[wow_class]),
        text("Character", x + 72, y + 27, f"{name} · {loadout}", 14, 600, colour),
        text("Detail", x + 72, y + 46, detail, 12, 400, MUTED),
        badge("Readiness badge", x + 400, y + 18, verdict, VERDICTS[verdict]),
    ]
    if not locked:
        items.append(text("Preferred", x + LEFT_W - 52, y + 36, "★" if preferred else "☆", 16, 800,
                          ACCENT if preferred else MUTED, 24, "center", icon=True))
    if note:
        items.append(text("Note", x + 72, y + 64, note, 12, 400, P["Status/danger text"] if locked else SECONDARY))
    return Group(f"Option {index + 1}", items)


def options_card(y: float) -> Group:
    row_h = 76
    items: list[Item] = [
        *card(CONTENT_X, y, LEFT_W, 148 + row_h * len(OPTIONS) + 8),
        text("Title", CONTENT_X + 20, y + 34, "Characters and specs", 16, 600),
        text("Caption", CONTENT_X + 20, y + 54, "Offer every option you'd play and star the one you prefer. Officers pick one.",
             12, 400, MUTED),
        label("Presets label", CONTENT_X + 20, y + 90, "PRESETS"),
        chip("Mains preset", CONTENT_X + 96, y + 72, "Mains", True),
        chip("Healers preset", CONTENT_X + 172, y + 72, "Healer alts", False),
        text("Save preset", CONTENT_X + 290, y + 91, "Save as preset", 12, 600, ACCENT),
        text("Select available", CONTENT_X + LEFT_W - 200, y + 91, "Select all available", 12, 600, ACCENT, 180, "right"),
        Group("Table header", [label("Option heading", CONTENT_X + 72, y + 132, "OPTION"),
                               label("Readiness heading", CONTENT_X + 400, y + 132, "READINESS"),
                               label("Preferred heading", CONTENT_X + LEFT_W - 96, y + 132, "PREFERRED")]),
    ]
    items += [option_row(index, y + 148 + index * row_h, option) for index, option in enumerate(OPTIONS)]
    return Group("Options card", items)


def availability_pill(name: str, x: float, y: float, value: str, selected: bool, target: str | None) -> Group:
    return Group(name, [Rect("Background", x, y, 162, 40, SELECTED if selected else RAISED, 1, 8,
                             ACCENT if selected else DIVIDER),
                        text("Label", x, y + 25, value, 13, 700 if selected else 400, ACCENT if selected else SECONDARY,
                             162, "center")],
                 Click("navigate", target) if target and not selected else None)


def availability_card(late: bool) -> Group:
    x, y = RIGHT_X, TOP
    choice = "Late" if late else "Confirmed"
    targets = {"Confirmed": FORM, "Late": LATE}
    pills = [availability_pill(f"{value} option", x + 20 + (index % 2) * 178, y + 60 + (index // 2) * 52, value,
                               value == choice, targets.get(value))
             for index, value in enumerate(("Confirmed", "Tentative", "Late", "Declined"))]
    items: list[Item] = [*card(x, y, RIGHT_W, 468), text("Title", x + 20, y + 34, "Your availability", 16, 600), *pills]
    if late:
        items += [form_field("Arrival field", x + 20, y + 190, 340, "Arriving at", "20:30 · 18:30 UTC", True)]
    else:
        items.append(text("Availability note", x + 20, y + 190, "Declined keeps your reply without offering characters.",
                          12, 400, MUTED))
    items += [
        form_field("Note field", x + 20, y + 270, 340, "Note to officers", NOTE),
        text("One signup", x + 20, y + 350, "One signup per player; you take at most one roster place.", 12, 400, MUTED),
        button("Sign up button", x + 20, y + 392, "Sign up", "primary", 340, Click("navigate", SIGNED_UP)),
    ]
    return Group("Availability card", items)


def form(late: bool) -> list[Item]:
    return [page_header(PAGE, TITLE, WHEN), options_card(TOP), availability_card(late)]


def signed_up(linked: bool = True) -> list[Item]:
    offered = [option for option in OPTIONS if option[5]]
    row_h = 64
    rows: list[Item] = [*card(CONTENT_X, TOP + 96, LEFT_W, 60 + row_h * len(offered) + 8),
                        text("Title", CONTENT_X + 20, TOP + 130, "You offered", 16, 600)]
    for index, (name, wow_class, loadout, detail, verdict, _, preferred, _) in enumerate(offered):
        y = TOP + 156 + index * row_h
        row: list[Item] = [Rect("Divider", CONTENT_X, y, LEFT_W, 1, DIVIDER),
                           Circle("Class colour", CONTENT_X + 25, y + 32, 5, CLASS_COLOURS[wow_class]),
                           text("Character", CONTENT_X + 40, y + 29, f"{name} · {loadout}", 14, 600),
                           text("Detail", CONTENT_X + 40, y + 48, detail, 12, 400, MUTED),
                           badge("Readiness badge", CONTENT_X + 400, y + 20, verdict, VERDICTS[verdict])]
        if preferred:
            row.append(badge("Preferred badge", CONTENT_X + 580, y + 20, "Preferred", "neutral"))
        rows.append(Group(f"Offered {index + 1}", row))
    summary: list[Item] = [
        *card(RIGHT_X, TOP + 96, RIGHT_W, 244),
        label("Availability label", RIGHT_X + 20, TOP + 132, "AVAILABILITY"),
        text("Availability", RIGHT_X + 20, TOP + 156, "Confirmed", 15, 600),
        label("Note label", RIGHT_X + 20, TOP + 196, "NOTE TO OFFICERS"),
        text("Note", RIGHT_X + 20, TOP + 218, NOTE, 13, 400, SECONDARY),
        button("Edit button", RIGHT_X + 20, TOP + 276, "Edit signup", "secondary", 164,
               Click("navigate", FORM) if linked else None),
        button("Withdraw button", RIGHT_X + 196, TOP + 276, "Withdraw", "danger", 164,
               Click("navigate", WITHDRAW) if linked else None),
    ]
    return [
        page_header(PAGE, "You're signed up for Icecrown Citadel", WHEN),
        notice("Signed notice", CONTENT_X, TOP, 1120, "Signup saved",
               "You can edit or withdraw until signups close on Fri 9 Oct, 18:00, in 2 days.", "success"),
        Group("Offered options", rows), Group("Signup summary", summary),
    ]


def withdraw_dialog() -> list[Item]:
    w, h = 480, 216
    x, y = (BOARD_W - w) / 2, (BOARD_H - h) / 2
    return [
        Rect("Dim overlay", 0, 0, BOARD_W, BOARD_H, P["Neutral/black"], 0.6),
        Group("Withdraw dialog", [
            *card(x, y, w, h),
            text("Title", x + 24, y + 44, "Withdraw your signup?", 18, 700),
            text("Body line 1", x + 24, y + 80, "Officers won't see your characters for this raid. You can", 14, 400,
                 SECONDARY),
            text("Body line 2", x + 24, y + 100, "sign up again until signups close.", 14, 400, SECONDARY),
            button("Cancel button", x + w - 24 - 120 - 8 - 96, y + h - 64, "Cancel", "secondary", 96,
                   Click("navigate", SIGNED_UP)),
            button("Confirm button", x + w - 24 - 120, y + h - 64, "Withdraw", "danger", 120, Click("navigate", FORM)),
        ]),
    ]


def boards() -> list[Board]:
    right, below = BOARD_W + BOARD_GAP, BOARD_H + BOARD_GAP

    def screen(name: str, x: float, y: float, content: list[Item]) -> Board:
        return app_screen(name, x, y, PAGE, content, section=SECTION, user=PLAYER)

    return [screen(FORM, 0, 0, form(False)), screen(LATE, right, 0, form(True)),
            screen(SIGNED_UP, 0, below, signed_up()),
            screen(WITHDRAW, right, below, [*signed_up(linked=False), *withdraw_dialog()])]


def main(repository: Path = REPOSITORY) -> Path:
    return write_mockup(repository, "raid-signup", "Raid signup", boards(), PALETTE,
                        {"Sign up for a raid": FORM, "Edit or withdraw": SIGNED_UP})


if __name__ == "__main__":
    print("Wrote", main())
