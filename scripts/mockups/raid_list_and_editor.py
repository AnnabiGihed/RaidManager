"""The raid list and raid editor (story #20, design story #183, task #221).

Four boards in RaidManager's design system (ADR-0019), seen by an officer, linked as a clickable prototype:

1. Upcoming raids: title and targets, start in local time and UTC, status, signup totals, Edit and Roster.
   New raid opens 3; Edit on Icecrown Citadel opens 4; the Past tab opens 2.
2. Past raids: completed and cancelled. The Upcoming tab opens 1.
3. Creating a raid: title, description, start, signup deadline, several instance and difficulty targets, the size
   they imply, and the readiness requirements. Save draft and Open for signups go back to 1.
4. Editing an open raid after changing its start: the warning that readiness is recalculated for its signups and the
   Discord post is updated. Save changes and Cancel go back to 1.

The data comes from the `Raid` aggregate (start, signup deadline, description, status, targets), `RaidTarget`,
`RaidRequirements` and the signups' availability, plus the title the story adds.

Run from the repository root: python scripts/mockups/raid_list_and_editor.py
Once the owner edits the design in Penpot, the downloaded file replaces docs/mockups/raid-list-and-editor.penpot
and this script is no longer run.
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
from penpot_scene import HOUSE_PALETTE, Board, Click, Group, Item, Rect, text, write_mockup  # noqa: E402

P = HOUSE_PALETTE
ACCENT, SECONDARY, MUTED, DIVIDER = P["Brand/accent"], P["Text/secondary"], P["Text/muted"], P["Line/divider"]
PAGE = "Schedule"
ICC = "Icecrown Citadel"
ICC_25H = "Icecrown Citadel · 25 players heroic"
VOA = "Weekly Vault run"
TWENTY_FIVE = "25 players"
UTC_18, UTC_19 = "18:00 UTC", "19:00 UTC"

UPCOMING = "1 · Upcoming raids"
PAST = "2 · Past raids"
CREATE = "3 · New raid"
EDIT = "4 · Edit an open raid"

TOP = CONTENT_TOP + 120


def label(name: str, x: float, y: float, value: str) -> Item:
    return text(name, x, y, value, 11, 700, MUTED, None, "left", 1.2)


def tabs(active: str) -> Group:
    items: list[Item] = [Rect("Divider", CONTENT_X, TOP + 36, CONTENT_W, 1, DIVIDER)]
    for index, (name, target) in enumerate((("Upcoming", UPCOMING), ("Past", PAST))):
        x = CONTENT_X + index * 120
        selected = name == active
        tab: list[Item] = [Rect("Area", x, TOP, 104, 36, P["Surface/page"]),
                           text("Label", x, TOP + 22, name, 14, 600 if selected else 400, ACCENT if selected else SECONDARY,
                                104, "center")]
        if selected:
            tab.append(Rect("Underline", x, TOP + 34, 104, 3, ACCENT, 1, 2))
        items.append(Group(f"{name} tab", tab, None if selected else Click("navigate", target)))
    return Group("Tabs", items)


def raid_table(rows: list[tuple], linked: bool) -> Group:
    top, head_h, row_h = TOP + 56, 44, 76
    columns = {"raid": 24, "starts": 400, "status": 590, "signups": 770, "actions": 950}
    items: list[Item] = card(CONTENT_X, top, CONTENT_W, head_h + row_h * len(rows) + 8)
    items.append(Group("Table header", [
        label(f"{heading.title()} heading", CONTENT_X + columns[key], top + 27, heading)
        for key, heading in (("raid", "RAID"), ("starts", "STARTS"), ("status", "STATUS"), ("signups", "SIGNUPS"))]))
    for index, (title, targets, local, utc, status, tone, signups, detail, actions) in enumerate(rows):
        y = top + head_h + index * row_h
        row: list[Item] = [
            Rect("Divider", CONTENT_X, y, CONTENT_W, 1, DIVIDER),
            text("Title", CONTENT_X + columns["raid"], y + 32, title, 14, 600),
            text("Targets", CONTENT_X + columns["raid"], y + 53, targets, 12, 400, MUTED),
            text("Local start", CONTENT_X + columns["starts"], y + 32, local, 13),
            text("UTC start", CONTENT_X + columns["starts"], y + 53, utc, 12, 400, MUTED),
            badge("Status badge", CONTENT_X + columns["status"], y + 26, status, tone),
            text("Signups", CONTENT_X + columns["signups"], y + 32, signups, 13),
            text("Signup detail", CONTENT_X + columns["signups"], y + 53, detail, 12, 400, MUTED),
        ]
        ax = CONTENT_X + columns["actions"]
        if "edit" in actions:
            target = Click("navigate", EDIT) if linked and title == ICC else None
            row.append(button("Edit button", ax, y + 18, "Edit", "secondary", 64, target))
            ax += 72
        if "roster" in actions:
            row.append(button("Roster button", ax, y + 18, "Roster", "secondary", 80))
        items.append(Group(f"Row {title}", row))
    return Group("Raids table", items)


def upcoming() -> list[Item]:
    rows = [
        (ICC, f"{ICC_25H} + Ruby Sanctum · {TWENTY_FIVE}", "Fri 2 Oct, 20:00", UTC_18, "Open for signups",
         "info", "18 confirmed", "4 tentative · 2 late · 3 declined", ("edit", "roster")),
        (VOA, f"Vault of Archavon · {TWENTY_FIVE}", "Sat 3 Oct, 21:00", UTC_19, "Roster published", "success",
         "25 confirmed", "1 tentative", ("edit", "roster")),
        ("ICC 10 alt run", "Icecrown Citadel · 10 players", "Tue 6 Oct, 20:00", UTC_18, "Draft", "neutral",
         "Not open yet", "Open it for signups to start", ("edit",)),
    ]
    return [
        page_header("Schedule", "Raids", "Times show in your time zone, with UTC beside them."),
        button("New raid button", CONTENT_X + CONTENT_W - 112, CONTENT_TOP + 28, "New raid", "primary", 112,
               Click("navigate", CREATE)),
        tabs("Upcoming"), raid_table(rows, True),
    ]


def past() -> list[Item]:
    rows = [
        (ICC, ICC_25H, "Fri 25 Sep, 20:00", UTC_18, "Completed", "neutral", "22 confirmed", "3 tentative",
         ("roster",)),
        (VOA, f"Vault of Archavon · {TWENTY_FIVE}", "Sat 26 Sep, 21:00", UTC_19, "Completed", "neutral",
         "25 confirmed", "No tentative", ("roster",)),
        ("Ulduar 25", f"Ulduar · {TWENTY_FIVE}", "Wed 23 Sep, 20:00", UTC_18, "Cancelled", "danger",
         "9 confirmed", "Cancelled on Tue 22 Sep", ()),
    ]
    return [page_header("Schedule", "Raids", "Times show in your time zone, with UTC beside them."), tabs("Past"),
            raid_table(rows, False)]


def raid_form(top: float, editing: bool) -> list[Item]:
    left_x, right_x = CONTENT_X, CONTENT_X + 560
    x = left_x + 20
    start_time = "21:00" if editing else "20:00"
    utc = UTC_19 if editing else UTC_18
    details: list[Item] = [
        *card(left_x, top, 540, 424), text("Details title", x, top + 34, "Details", 16, 600),
        form_field("Title field", x, top + 76, 500, "Title", ICC),
        form_field("Description field", x, top + 156, 500, "Description", "Weekly progression, then Halion if time allows."),
        form_field("Start date field", x, top + 236, 240, "Start", "Fri 9 Oct", True),
        form_field("Start time field", x + 260, top + 236, 240, "Time", start_time, True),
        text("Start UTC", x, top + 310, f"{utc} · the weekly reset is Wed 04:00", 12, 400, MUTED),
        form_field("Deadline date field", x, top + 340, 240, "Signups close", "Fri 9 Oct", True),
        form_field("Deadline time field", x + 260, top + 340, 240, "Time", "18:00", True),
    ]
    if editing:
        details.append(Rect("Changed marker", x + 260, top + 246, 240, 40, None, 1, 8, ACCENT))
    targets: list[Item] = [*card(right_x, top, 560, 286), text("Targets title", right_x + 20, top + 34, "Targets", 16, 600)]
    for index, (instance, difficulty) in enumerate(((ICC, "25 players, heroic"), ("Ruby Sanctum", TWENTY_FIVE))):
        y = top + 76 + index * 80
        targets.append(Group(f"Target {index + 1}", [
            form_field("Instance field", right_x + 20, y, 280, "Instance", instance, True),
            form_field("Difficulty field", right_x + 316, y, 196, "Difficulty", difficulty, True),
            text("Remove", right_x + 524, y + 36, "×", 16, 800, SECONDARY, 24, "center", icon=True),
        ]))
    targets += [text("Add target", right_x + 20, top + 256, "+ Add target", 13, 600, ACCENT),
                text("Size", right_x + 280, top + 256, f"Size: {TWENTY_FIVE}, from the targets", 12, 400, MUTED, 260,
                     "right")]
    requirements_top = top + 302
    requirements: list[Item] = [
        *card(right_x, requirements_top, 560, 180),
        text("Requirements title", right_x + 20, requirements_top + 34, "Readiness requirements", 16, 600),
        form_field("Gear score field", right_x + 20, requirements_top + 76, 250, "Minimum GearScore", "5,500"),
        form_field("Data age field", right_x + 290, requirements_top + 76, 250, "Character data no older than", "3 days",
                   True),
        Rect("Checkbox", right_x + 20, requirements_top + 144, 18, 18, ACCENT, 1, 4),
        text("Check mark", right_x + 20, requirements_top + 158, "✓", 12, 800, P["Brand/on accent"], 18, "center",
             icon=True),
        text("Checkbox label", right_x + 48, requirements_top + 158, "Only characters without a save for these targets",
             13),
    ]
    return [Group("Details form", details), Group("Targets form", targets), Group("Requirements form", requirements)]


def create() -> list[Item]:
    buttons_y = TOP + 498
    return [
        page_header("Schedule", "New raid", "Players see it once you open it for signups."),
        *raid_form(TOP, False),
        button("Open button", CONTENT_X + 560, buttons_y, "Open for signups", "primary", 168, Click("navigate", UPCOMING)),
        button("Draft button", CONTENT_X + 736, buttons_y, "Save draft", "secondary", 120, Click("navigate", UPCOMING)),
    ]


def edit() -> list[Item]:
    form_top = TOP + 88
    return [
        page_header("Schedule", f"Edit {ICC}", "Open for signups · 18 confirmed, 4 tentative, 2 late"),
        notice("Recalculation warning", CONTENT_X, TOP, CONTENT_W, "Changing the time or targets recalculates readiness",
               "The 24 signups are checked again, players whose verdict changes are told, and the Discord post is updated.",
               "warning"),
        *raid_form(form_top, True),
        button("Save button", CONTENT_X + 560, form_top + 498, "Save changes", "primary", 144, Click("navigate", UPCOMING)),
        button("Cancel button", CONTENT_X + 712, form_top + 498, "Cancel", "secondary", 96, Click("navigate", UPCOMING)),
    ]


def boards() -> list[Board]:
    right, below = BOARD_W + BOARD_GAP, BOARD_H + BOARD_GAP

    def screen(name: str, x: float, y: float, content: list[Item]) -> Board:
        return app_screen(name, x, y, PAGE, content, links={PAGE: UPCOMING})

    return [screen(UPCOMING, 0, 0, upcoming()), screen(PAST, right, 0, past()),
            screen(CREATE, 0, below, create()), screen(EDIT, right, below, edit())]


def main(repository: Path = REPOSITORY) -> Path:
    return write_mockup(repository, "raid-list-and-editor", "Raid list and editor", boards(),
                        flows={"Create a raid": UPCOMING, "Edit an open raid": EDIT})


if __name__ == "__main__":
    print("Wrote", main())
