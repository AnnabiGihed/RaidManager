"""Characters arriving from a sync while the website is open (story #595, tasks #596 and #608).

Five boards in RaidManager's design system (ADR-0019), seen by a player, linked as a clickable prototype:

1. A sync brings characters to review while the player is on another page (here My characters): a notification
   names them, with a "Review them" link that opens board 2. It stays until the player closes it with its × or
   follows the link, so a player back from the game still sees it (owner decision on #595). My characters also shows
   the notice of board 5.
2. The review page opened from the notification: the new characters wait for a decision, as on the review page of
   story #18.
3. The review page open with nothing left to decide, before a sync.
4. The same page after a sync brought two characters: the list appears without a refresh, and a notification says
   what arrived, without a link since the player is already there.
5. My characters while characters wait for review, without a sync: a notice says how many wait, and its "Review them"
   button opens board 2. None shows when nothing waits (owner decision on #595).

A sync that brings no character to review shows nothing. The notification and the live update follow the boards of
`character-review` (the table, the waiting notice, the "You're all set" card) and `character-profile` (My characters).

Run from the repository root: python scripts/mockups/character_sync.py
Once the owner edits the design in Penpot, the downloaded file replaces docs/mockups/character-sync.penpot and this
script is no longer run.
"""

from __future__ import annotations

import sys
from dataclasses import replace
from pathlib import Path

REPOSITORY = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(REPOSITORY / "scripts"))
sys.path.insert(0, str(REPOSITORY / "scripts" / "mockups"))

import character_profile  # noqa: E402
import character_review  # noqa: E402
from penpot_components import (  # noqa: E402
    BOARD_GAP, BOARD_H, BOARD_W, CONTENT_W, CONTENT_X, PLAYER, app_screen, button, notice, toast,
)
from penpot_scene import HOUSE_PALETTE, Board, Click, Group, Item, offset, write_mockup  # noqa: E402

P = HOUSE_PALETTE
# The characters the sync brings aren't on My characters yet: two classes the other files don't draw.
NEW_CLASSES = {"Paladin": "#F48CBA", "Rogue": "#FFF468"}
character_review.CLASS_COLOURS.update(NEW_CLASSES)
PALETTE = {**character_review.PALETTE, **character_profile.PALETTE,
           **{f"WoW class/{name}": colour for name, colour in NEW_CLASSES.items()}}

OTHER_PAGE = "1 · A sync brings characters, on another page"
OPENED = "2 · The review page, opened from the notification"
NOTHING_LEFT = "3 · The review page with nothing to decide, before a sync"
UPDATED = "4 · The same page after the sync, without a refresh"
WAITING = "5 · My characters while characters wait for review"

ARRIVED = [
    ("Uthertank", "Icecrown", "Paladin", "Human", 80, "Today, 14:05", "Pending"),
    ("Valeerarog", "Icecrown", "Rogue", "Blood Elf", 74, "Today, 14:05", "Pending"),
]
CONFLICT = [row for row in character_review.ROWS if row[0] == "Sylvanash"]
AFTER_SYNC = ARRIVED + CONFLICT
NAMES = "Uthertank and Valeerarog"


def without_clicks(items: list[Item]) -> list[Item]:
    """Drops the clicks of a board borrowed from another file, whose targets aren't boards of this one."""
    return [replace(item, on_click=None, children=without_clicks(item.children)) if isinstance(item, Group) else item
            for item in items]


def my_characters_waiting() -> list[Item]:
    """My characters with the review notice above the table, which moves down to make room."""
    top = character_profile.TOP
    header, *rest = without_clicks(character_profile.characters())
    return [
        header,
        Group("Review notice", [
            notice("Notice", CONTENT_X, top, CONTENT_W, "2 characters wait for your review",
                   "Approve the ones that are yours so they can sign up for raids.", "info"),
            button("Review them button", CONTENT_X + CONTENT_W - 24 - 128, top + 16, "Review them", "primary", 128,
                   Click("navigate", OPENED)),
        ]),
        *(offset(item, 0, 96) for item in rest),
    ]


def other_page() -> list[Item]:
    return [*my_characters_waiting(),
            toast("Sync notification", "2 new characters to review", f"Your companion found {NAMES}.",
                  P["Accent/blue"], 400, "Review them", Click("navigate", OPENED))]


def review_page(notification: bool) -> list[Item]:
    items: list[Item] = [*character_review.header(), character_review.waiting_notice(AFTER_SYNC),
                         character_review.review_table(AFTER_SYNC, linked=False)]
    if notification:
        items.append(toast("Arrived notification", "2 new characters arrived", f"{NAMES} were added to the list.",
                           P["Accent/blue"], 400))
    return without_clicks(items)


def nothing_left() -> list[Item]:
    return [item for item in without_clicks(character_review.all_set()) if item.name != "Approved notification"]


def boards() -> list[Board]:
    right, below = BOARD_W + BOARD_GAP, BOARD_H + BOARD_GAP

    def screen(name: str, x: float, y: float, content: list[Item], review: bool) -> Board:
        if review:
            return app_screen(name, x, y, character_review.PAGE, content, section=character_review.SECTION,
                              user=PLAYER)
        return app_screen(name, x, y, character_profile.PAGE, content, user=PLAYER)

    return [
        screen(OTHER_PAGE, 0, 0, other_page(), review=False),
        screen(OPENED, right, 0, review_page(notification=False), review=True),
        screen(NOTHING_LEFT, 0, below, nothing_left(), review=True),
        screen(UPDATED, right, below, review_page(notification=True), review=True),
        screen(WAITING, 0, 2 * below, my_characters_waiting(), review=False),
    ]


def main(repository: Path = REPOSITORY) -> Path:
    return write_mockup(repository, "character-sync", "Character sync", boards(), PALETTE,
                        {"Sync notification": OTHER_PAGE, "Review page updated by a sync": NOTHING_LEFT,
                         "Review notice on My characters": WAITING})


if __name__ == "__main__":
    print("Wrote", main())
