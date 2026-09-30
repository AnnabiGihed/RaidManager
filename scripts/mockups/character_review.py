"""Character review page (story #18, design story #181, task #167): the first page after sign-in when characters
await the player's decision.

Four boards in RaidManager's design system (ADR-0019), seen by a player, linked as a clickable prototype:

1. Pending claims, with a conflict. Reject on Jainaice opens board 2; Approve on Thrallsham, the last claim the
   prototype decides, shows board 3.
2. Reject confirmation. Cancel and Reject go back to board 1.
3. All reviewed, with the notification after the approval. The conflicted character still waits for an officer.
4. The characters can't be loaded. Try again goes back to board 1.

The page shows only what the claims API returns (#161): name, realm, class, race, level, claim state and when the
companion found the character. "Decide later" leaves the page; undecided characters stay pending and the page
appears again at the next sign-in (story #18).

Run from the repository root: python scripts/mockups/character_review.py
Once the owner edits the design in Penpot, the downloaded file replaces docs/mockups/character-review.penpot and this
script is no longer run.
"""

from __future__ import annotations

import sys
from pathlib import Path

REPOSITORY = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(REPOSITORY / "scripts"))

from penpot_components import (  # noqa: E402
    BOARD_GAP, BOARD_H, BOARD_W, CONTENT_TOP, CONTENT_W, CONTENT_X, PLAYER, app_screen, badge, button, card, notice,
    page_header,
)
from penpot_scene import HOUSE_PALETTE, Board, Circle, Click, Group, Item, Rect, text  # noqa: E402
from penpot_scene import write_mockup  # noqa: E402

P = HOUSE_PALETTE
MUTED, SECONDARY = P["Text/muted"], P["Text/secondary"]
CLASS_COLOURS = {"Death Knight": "#C41E3A", "Mage": "#3FC7EB", "Shaman": "#0070DD", "Hunter": "#AAD372"}
PALETTE = {**HOUSE_PALETTE, **{f"WoW class/{name}": colour for name, colour in CLASS_COLOURS.items()}}

PAGE = "My characters"
SECTION = "Review new characters"
PENDING = "1 · Pending claims, with a conflict"
REJECT = "2 · Reject confirmation"
ALL_SET = "3 · All reviewed, after the last approval"
FAILED = "4 · The characters can't be loaded"

ROWS = [
    ("Arthasdk", "Icecrown", "Death Knight", "Human", 80, "Today, 14:05", "Pending"),
    ("Jainaice", "Lordaeron", "Mage", "Human", 80, "Today, 14:05", "Pending"),
    ("Thrallsham", "Icecrown", "Shaman", "Orc", 78, "Yesterday, 21:40", "Pending"),
    ("Sylvanash", "Icecrown", "Hunter", "Undead", 80, "Yesterday, 21:40", "Conflict"),
]
COLUMNS = {"character": 24, "class": 264, "race": 424, "level": 544, "found": 624, "status": 784, "decision": 904}
HEADINGS = (("character", "CHARACTER"), ("class", "CLASS"), ("race", "RACE"), ("level", "LEVEL"),
            ("found", "FOUND"), ("status", "STATUS"), ("decision", "DECISION"))
TABLE_TOP = CONTENT_TOP + 208
HEAD_H, ROW_H = 44, 72


def header(decide_later: bool = True) -> list[Item]:
    items: list[Item] = [page_header(
        "New characters", "Review your new characters",
        "Your companion found these characters on your WoW accounts. Only the ones you approve can sign up for raids.")]
    if decide_later:
        items.append(button("Decide later button", CONTENT_X + CONTENT_W - 128, CONTENT_TOP + 28, "Decide later",
                            "secondary", 128))
    return items


def character_cell(x: float, y: float, name: str, realm: str) -> list[Item]:
    return [text("Name", x, y + 32, name, 14, 600), text("Realm", x, y + 52, realm, 12, 400, MUTED)]


def row(index: int, claim: tuple, linked: bool) -> Group:
    name, realm, wow_class, race, level, found, state = claim
    x, y = CONTENT_X, TABLE_TOP + HEAD_H + index * ROW_H
    items: list[Item] = [Rect("Divider", x, y, CONTENT_W, 1, P["Line/divider"])]
    items += character_cell(x + COLUMNS["character"], y, name, realm)
    items += [
        Circle("Class colour", x + COLUMNS["class"] + 5, y + 36, 5, CLASS_COLOURS[wow_class]),
        text("Class", x + COLUMNS["class"] + 18, y + 41, wow_class, 14),
        text("Race", x + COLUMNS["race"], y + 41, race, 14),
        text("Level", x + COLUMNS["level"], y + 41, str(level), 14),
        text("Found", x + COLUMNS["found"], y + 41, found, 13, 400, SECONDARY),
        badge("Status badge", x + COLUMNS["status"], y + 24, state, "warning" if state == "Pending" else "danger"),
    ]
    dx = x + COLUMNS["decision"]
    if state == "Pending":
        approve = Click("navigate", ALL_SET) if linked and name == "Thrallsham" else None
        reject = Click("navigate", REJECT) if linked and name == "Jainaice" else None
        items += [button("Approve button", dx, y + 16, "Approve", "primary", 96, approve),
                  button("Reject button", dx + 104, y + 16, "Reject", "secondary", 88, reject)]
    else:
        items += [text("Next step", dx, y + 32, "An officer will review it", 13, 600),
                  text("Reason", dx, y + 52, "Another player owns it.", 12, 400, MUTED)]
    return Group(f"Row {name}", items)


def review_table(rows: list[tuple], linked: bool) -> Group:
    height = HEAD_H + ROW_H * len(rows) + 8
    items: list[Item] = card(CONTENT_X, TABLE_TOP, CONTENT_W, height)
    items.append(Group("Table header", [
        text(f"{label.title()} heading", CONTENT_X + COLUMNS[key], TABLE_TOP + 27, label, 11, 700, MUTED,
             None, "left", 1.2)
        for key, label in HEADINGS]))
    items += [row(index, claim, linked) for index, claim in enumerate(rows)]
    return Group("Review table", items)


def waiting_notice(rows: list[tuple]) -> Group:
    waiting = sum(1 for claim in rows if claim[6] == "Pending")
    return notice("Waiting notice", CONTENT_X, CONTENT_TOP + 120, CONTENT_W,
                  f"{waiting} characters are waiting for your decision",
                  "Undecided characters stay pending, and this page opens again at your next sign-in.", "info")


def pending(linked: bool = True) -> list[Item]:
    return [*header(), waiting_notice(ROWS), review_table(ROWS, linked)]


def reject_dialog() -> list[Item]:
    w, h = 480, 236
    x, y = (BOARD_W - w) / 2, (BOARD_H - h) / 2
    return [
        Rect("Dim overlay", 0, 0, BOARD_W, BOARD_H, P["Neutral/black"], 0.6),
        Group("Reject dialog", [
            *card(x, y, w, h),
            text("Title", x + 24, y + 44, "Reject Jainaice?", 18, 700),
            text("Body line 1", x + 24, y + 80, "Jainaice (Lordaeron) won't become one of your characters,", 14, 400,
                 SECONDARY),
            text("Body line 2", x + 24, y + 100, "so you can't sign it up for raids.", 14, 400, SECONDARY),
            text("Body line 3", x + 24, y + 128, "Reject it only if it isn't yours.", 14, 400, SECONDARY),
            button("Cancel button", x + w - 24 - 96 - 8 - 96, y + h - 64, "Cancel", "secondary", 96,
                   Click("navigate", PENDING)),
            button("Confirm reject button", x + w - 24 - 96, y + h - 64, "Reject", "danger", 96,
                   Click("navigate", PENDING)),
        ]),
    ]


def all_set() -> list[Item]:
    top, height = CONTENT_TOP + 120, 312
    middle = CONTENT_X + CONTENT_W / 2
    toast_x, toast_y, toast_w = BOARD_W - 40 - 420, 80, 420
    return [
        *header(decide_later=False),
        Group("Empty state", [
            *card(CONTENT_X, top, CONTENT_W, height),
            Circle("Success circle", middle, top + 80, 36, P["Status/success background"]),
            text("Success mark", middle - 20, top + 94, "✓", 40, 800, P["Brand/accent"], 40, "center", icon=True),
            text("Title", CONTENT_X, top + 160, "You're all set", 18, 700, P["Text/primary"], CONTENT_W, "center"),
            text("Message", CONTENT_X, top + 188, "No characters are waiting for your decision.", 14, 400,
                 SECONDARY, CONTENT_W, "center"),
            text("Conflict reminder", CONTENT_X, top + 212, "Sylvanash stays in conflict review until an officer decides.",
                 13, 400, MUTED, CONTENT_W, "center"),
            button("Go to characters button", middle - 80, top + 240, "Go to my characters", "primary", 160),
        ]),
        Group("Approved notification", [
            *card(toast_x, toast_y, toast_w, 72, P["Brand/accent"]),
            text("Title", toast_x + 24, toast_y + 31, "Thrallsham approved", 14, 600),
            text("Message", toast_x + 24, toast_y + 53, "It's now one of your characters and can sign up for raids.", 13,
                 400, SECONDARY),
        ]),
    ]


def load_failed() -> list[Item]:
    top = CONTENT_TOP + 120
    return [
        *header(),
        notice("Error message", CONTENT_X, top, CONTENT_W, "We couldn't load your characters",
               "Nothing was approved or rejected. Try again in a moment.", "danger"),
        button("Try again button", CONTENT_X, top + 96, "Try again", "primary", 112, Click("navigate", PENDING)),
    ]


def boards() -> list[Board]:
    right, below = BOARD_W + BOARD_GAP, BOARD_H + BOARD_GAP

    def screen(name: str, x: float, y: float, content: list[Item]) -> Board:
        return app_screen(name, x, y, PAGE, content, section=SECTION, user=PLAYER)

    return [
        screen(PENDING, 0, 0, pending()),
        screen(REJECT, right, 0, pending(linked=False) + reject_dialog()),
        screen(ALL_SET, 0, below, all_set()),
        screen(FAILED, right, below, load_failed()),
    ]


def main(repository: Path = REPOSITORY) -> Path:
    return write_mockup(repository, "character-review", "Character review", boards(), PALETTE,
                        {"Review new characters": PENDING, "Characters can't be loaded": FAILED})


if __name__ == "__main__":
    print("Wrote", main())
