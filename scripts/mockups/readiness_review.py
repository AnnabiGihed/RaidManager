"""Readiness re-evaluation and exceptions (story #28, design story #188, task #231).

Four boards in RaidManager's design system (ADR-0019), seen by an officer, linked as a clickable prototype:

1. The raid's readiness after a check: a new snapshot and a time change re-evaluated every signup and roster place.
   Each row shows the verdict per target, the overall verdict (the most restrictive), the evidence and what
   changed. Review on Lothar opens 2; Record exception on Jainaice opens 3.
2. A published roster place made ineligible: Lothar is locked through the raid. Nothing was replaced; the officer
   decides. Locked through raid has no exception.
3. Recording an exception for Needs fresh sync, with its reason. Record exception opens 4.
4. The exception shown with its reason; the verdict stays Needs fresh sync, and a new snapshot ends it.

The data comes from `ReadinessVerdict`, `EligibilityAssessment` (target, verdict, reset), `RaidLockout` and each
character's data source and observation times.

Run from the repository root: python scripts/mockups/readiness_review.py
Once the owner edits the design in Penpot, the downloaded file replaces docs/mockups/readiness-review.penpot and
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

P = HOUSE_PALETTE
ACCENT, SECONDARY, MUTED, DIVIDER = P["Brand/accent"], P["Text/secondary"], P["Text/muted"], P["Line/divider"]
CLASS_COLOURS = {"Paladin": "#F48CBA", "Mage": "#3FC7EB", "Priest": "#FFFFFF", "Druid": "#FF7C0A"}
PALETTE = {**HOUSE_PALETTE, **{f"WoW class/{name}": colour for name, colour in CLASS_COLOURS.items()}}

PAGE = "Readiness"
SECTION = "Icecrown Citadel"
TITLE = "Icecrown Citadel readiness"
WHEN = "Fri 9 Oct, 21:00 · 19:00 UTC · Icecrown Citadel 25 heroic + Ruby Sanctum 25"
LOCKED, AVAILABLE, STALE, RESETS = "Locked through raid", "Available", "Needs fresh sync", "Resets before raid"
TONES = {AVAILABLE: "success", RESETS: "info", STALE: "warning", LOCKED: "danger"}
REASON = "Bryn confirmed in Discord that Jainaice has no Icecrown save this week."
ON_ROSTER = "On the roster"

CHECKED = "1 · Readiness checked again"
INELIGIBLE = "2 · Roster place made ineligible"
EXCEPTION = "3 · Record an exception"
RECORDED = "4 · Exception recorded"

TOP = CONTENT_TOP + 120
# character, class, player, place, ICC verdict, Ruby Sanctum verdict, evidence, what changed, action
ROWS = [
    ("Lothar", "Paladin", "Tomas Hale", ON_ROSTER, LOCKED, AVAILABLE,
     "Icecrown Citadel 25 heroic save until Wed 14 Oct · addon, today 14:05", "New snapshot", "review"),
    ("Jainaice", "Mage", "Bryn Valewood", ON_ROSTER, STALE, STALE,
     "No raid-save scan since Sun 4 Oct · addon", "Data got too old", "exception"),
    ("Kirilight", "Priest", "Kiri Dawn", "Signed up", RESETS, AVAILABLE,
     "Save resets Wed 7 Oct, 04:00 · addon, yesterday 22:10", "Raid moved to 21:00", ""),
    ("Moonveil", "Druid", "Arvel Moss", ON_ROSTER, AVAILABLE, AVAILABLE,
     "No matching save · addon, today 09:30", "", ""),
]


def label(name: str, x: float, y: float, value: str) -> Item:
    return text(name, x, y, value, 11, 700, MUTED, None, "left", 1.2)


def overall(icc: str, ruby: str) -> str:
    order = [AVAILABLE, RESETS, STALE, LOCKED]
    return max(icc, ruby, key=order.index)


def readiness_table(linked: bool, excepted: bool) -> Group:
    head_h, row_h = 44, 88
    columns = {"character": 24, "icc": 300, "ruby": 470, "overall": 640, "action": 900}
    items: list[Item] = card(CONTENT_X, TOP + 88, CONTENT_W, head_h + row_h * len(ROWS) + 8)
    top = TOP + 88
    items.append(Group("Table header", [
        label(f"{heading.title()} heading", CONTENT_X + columns[key], top + 27, heading)
        for key, heading in (("character", "CHARACTER"), ("icc", "ICC 25 HEROIC"), ("ruby", "RUBY SANCTUM 25"),
                             ("overall", "OVERALL"))]))
    for index, (name, wow_class, player, place, icc, ruby, evidence, changed, action) in enumerate(ROWS):
        y = top + head_h + index * row_h
        verdict = overall(icc, ruby)
        row: list[Item] = [
            Rect("Divider", CONTENT_X, y, CONTENT_W, 1, DIVIDER),
            Circle("Class colour", CONTENT_X + 29, y + 26, 5, CLASS_COLOURS[wow_class]),
            text("Character", CONTENT_X + 42, y + 30, name, 14, 600),
            text("Player", CONTENT_X + 42, y + 49, f"{player} · {place}", 12, 400, MUTED),
            text("Evidence", CONTENT_X + 42, y + 72, evidence, 12, 400, SECONDARY),
            badge("ICC badge", CONTENT_X + columns["icc"], y + 14, icc, TONES[icc]),
            badge("Ruby badge", CONTENT_X + columns["ruby"], y + 14, ruby, TONES[ruby]),
            badge("Overall badge", CONTENT_X + columns["overall"], y + 14, verdict, TONES[verdict]),
        ]
        if changed:
            row.append(text("Changed", CONTENT_X + columns["overall"], y + 56, f"Changed: {changed}", 12, 400, MUTED))
        ax = CONTENT_X + columns["action"]
        if action == "review":
            row.append(button("Review button", ax, y + 12, "Review", "primary", 96,
                              Click("navigate", INELIGIBLE) if linked else None))
        elif action == "exception" and not excepted:
            row.append(button("Exception button", ax, y + 12, "Record exception", "secondary", 176,
                              Click("navigate", EXCEPTION) if linked else None))
        elif action == "exception":
            row += [badge("Exception badge", ax, y + 14, "Exception", "neutral"),
                    text("Exception reason", CONTENT_X + columns["icc"], y + 72,
                         "Exception by Gihed Annabi: " + REASON, 12, 400, P["Status/warning title"])]
        items.append(Group(f"Row {name}", row))
    return Group("Readiness table", items)


def checked(linked: bool = True, excepted: bool = False) -> list[Item]:
    return [
        page_header("Readiness", TITLE, WHEN),
        notice("Checked notice", CONTENT_X, TOP, CONTENT_W, "Readiness was checked again today at 14:05",
               "A new snapshot arrived and the raid moved to 21:00. Two roster places need you; nothing was replaced.",
               "info"),
        readiness_table(linked, excepted),
    ]


def ineligible() -> list[Item]:
    left_w = 720
    evidence: list[Item] = [
        *card(CONTENT_X, TOP + 96, left_w, 332),
        text("Title", CONTENT_X + 20, TOP + 130, "Evidence for Lothar", 16, 600),
    ]
    rows = [("Icecrown Citadel 25 heroic", LOCKED, "Save ID 43127 · resets Wed 14 Oct, 04:00, after the raid"),
            ("Ruby Sanctum 25", AVAILABLE, "No matching save")]
    for index, (target, verdict, detail) in enumerate(rows):
        y = TOP + 152 + index * 68
        evidence.append(Group(f"Target {index + 1}", [
            Rect("Divider", CONTENT_X, y, left_w, 1, DIVIDER),
            text("Target", CONTENT_X + 20, y + 28, target, 14, 600),
            text("Detail", CONTENT_X + 20, y + 48, detail, 12, 400, SECONDARY),
            badge("Verdict", CONTENT_X + left_w - 20 - 170, y + 16, verdict, TONES[verdict]),
        ]))
    evidence += [
        Rect("Divider", CONTENT_X, TOP + 288, left_w, 1, DIVIDER),
        label("Source label", CONTENT_X + 20, TOP + 318, "SOURCE"),
        text("Source", CONTENT_X + 20, TOP + 340, "WoW addon raid-save scan, complete, today 14:05", 13),
        label("Start label", CONTENT_X + 20, TOP + 378, "RAID START"),
        text("Start", CONTENT_X + 20, TOP + 400, "Fri 9 Oct, 21:00 · 19:00 UTC", 13),
    ]
    decision_x = CONTENT_X + 740
    decision: list[Item] = [
        *card(decision_x, TOP + 96, 380, 332),
        text("Title", decision_x + 20, TOP + 130, "Your decision", 16, 600),
        text("Line 1", decision_x + 20, TOP + 160, "Lothar can't raid while saved. Remove it,", 13, 400, SECONDARY),
        text("Line 2", decision_x + 20, TOP + 180, "then choose a replacement in the roster.", 13, 400, SECONDARY),
        button("Remove button", decision_x + 20, TOP + 212, "Remove from the roster", "danger", 340),
        button("Roster button", decision_x + 20, TOP + 264, "Open the roster builder", "secondary", 340),
        text("No override", decision_x + 20, TOP + 336, "Locked through raid has no exception.", 12, 400, MUTED),
        text("Told", decision_x + 20, TOP + 356, "Tomas Hale and the officers were told.", 12, 400, MUTED),
    ]
    return [
        page_header("Readiness", "Lothar is locked through the raid", "Published roster place · Tomas Hale · " + SECTION),
        notice("Ineligible notice", CONTENT_X, TOP, CONTENT_W, "This published roster place is no longer eligible",
               "A new save for Icecrown Citadel 25 heroic arrived after the roster was published. Nothing was changed.",
               "danger"),
        Group("Evidence", evidence), Group("Decision", decision),
        button("Back button", CONTENT_X, TOP + 452, "Back to readiness", "secondary", 176, Click("navigate", CHECKED)),
    ]


def exception_dialog() -> list[Item]:
    w, h = 560, 336
    x, y = (BOARD_W - w) / 2, (BOARD_H - h) / 2
    lines = ["Jainaice needs a fresh sync: no raid-save scan since Sun 4 Oct.",
             "An exception keeps it on the roster. The verdict stays Needs fresh",
             "sync, and your reason is shown to Bryn Valewood and the officers."]
    return [
        Rect("Dim overlay", 0, 0, BOARD_W, BOARD_H, P["Neutral/black"], 0.6),
        Group("Exception dialog", [
            *card(x, y, w, h),
            text("Title", x + 24, y + 44, "Keep Jainaice on the roster?", 18, 700),
            *[text(f"Body line {index + 1}", x + 24, y + 76 + index * 20, line, 14, 400, SECONDARY)
              for index, line in enumerate(lines)],
            form_field("Reason field", x + 24, y + 160, w - 48, "Reason (required)", REASON),
            text("Ends note", x + 24, y + 238, "A new snapshot ends the exception and asks you to review again.", 12, 400,
                 MUTED),
            button("Cancel button", x + w - 24 - 176 - 8 - 96, y + h - 64, "Cancel", "secondary", 96,
                   Click("navigate", CHECKED)),
            button("Record button", x + w - 24 - 176, y + h - 64, "Record exception", "primary", 176,
                   Click("navigate", RECORDED)),
        ]),
    ]


def recorded() -> list[Item]:
    tx, ty = BOARD_W - 40 - 380, BOARD_H - 40 - 72
    return [*checked(linked=False, excepted=True),
            Group("Recorded notification", [*card(tx, ty, 380, 72, ACCENT),
                                            text("Title", tx + 24, ty + 31, "Exception recorded", 14, 600),
                                            text("Message", tx + 24, ty + 53, "Bryn Valewood can see your reason.", 13, 400,
                                                 SECONDARY)])]


def boards() -> list[Board]:
    right, below = BOARD_W + BOARD_GAP, BOARD_H + BOARD_GAP

    def screen(name: str, x: float, y: float, content: list[Item]) -> Board:
        return app_screen(name, x, y, PAGE, content, section=SECTION)

    return [screen(CHECKED, 0, 0, checked()), screen(INELIGIBLE, right, 0, ineligible()),
            screen(EXCEPTION, 0, below, [*checked(linked=False), *exception_dialog()]),
            screen(RECORDED, right, below, recorded())]


def main(repository: Path = REPOSITORY) -> Path:
    return write_mockup(repository, "readiness-review", "Readiness review", boards(), PALETTE,
                        {"Review an ineligible place": CHECKED, "Record an exception": EXCEPTION})


if __name__ == "__main__":
    print("Wrote", main())
