"""Roster review and publication (story #32, design story #192, task #239).

Four boards, linked as a clickable prototype. The roster is Draft A from `roster_data.py` after the composer's swap
(Kirilight in for Shadestep) with both placeholders filled; the place cards come from `roster_composer.py`:

1. Final review: the assignments changed since the published roster (outlined), the warnings still open, and who is
   told. Publish opens 2.
2. The published roster on the website: version, time, and the Discord post's status. View the Discord post opens 3.
3. The single Discord post, edited in place with the change.
4. The published roster flagged for review: Frostweave got a lockout after publication. Nothing was replaced, and the
   Discord post stays as published until the officer publishes again.

Run from the repository root: python scripts/mockups/roster_publication.py
Once the owner edits the design in Penpot, the downloaded file replaces docs/mockups/roster-publication.penpot and
this script is no longer run.
"""

from __future__ import annotations

import sys
from pathlib import Path

REPOSITORY = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(REPOSITORY / "scripts"))

from penpot_components import (  # noqa: E402
    BOARD_GAP, BOARD_H, BOARD_W, CONTENT_TOP, CONTENT_W, CONTENT_X, DISCORD_MUTED, DISCORD_PALETTE, DISCORD_TEXT,
    DISCORD_W, DISCORD_WHITE, app_screen, badge, button, card, discord_button, discord_embed, discord_message,
    discord_screen, notice, page_header,
)
from penpot_scene import HOUSE_PALETTE, Board, Click, Group, Item, Rect, text, write_mockup  # noqa: E402
from roster_composer import GROUP_GAP, GROUP_W, SLOT_H, slot_card  # noqa: E402
from roster_data import DRAFT_A, HEALER, MELEE, PALETTE_ADDITIONS, WHEN, Slot  # noqa: E402

P = HOUSE_PALETTE
# The Priest class colour is Discord's white, so the library keeps one name for it.
PALETTE = {**HOUSE_PALETTE, **DISCORD_PALETTE,
           **{name: colour for name, colour in PALETTE_ADDITIONS.items() if colour != DISCORD_PALETTE["Discord/white"]}}
ACCENT, SECONDARY, MUTED, DIVIDER = P["Brand/accent"], P["Text/secondary"], P["Text/muted"], P["Line/divider"]
TEXT, WARNING_TEXT = P["Text/primary"], P["Status/warning title"]
PAGE = "Roster builder"
TITLE = "Icecrown Citadel roster"
FLAGGED_CHARACTER = "Frostweave"

REVIEW = "1 · Final review"
PUBLISHED = "2 · Published on the website"
DISCORD = "3 · The Discord post"
FLAGGED = "4 · Flagged for review"

TOP = CONTENT_TOP + 120
CHANGES = {"Kirilight": "replaces Shadestep in group 1",
           "Mendara": "fills the healer place in group 2",
           "Ironclad": "fills the melee place in group 4"}
FILLS = {HEALER: Slot("Nia Holt", "Mendara", "Shaman", "Restoration", HEALER),
         MELEE: Slot("Bo Rennick", "Ironclad", "Warrior", "Arms", MELEE)}
SWAP_IN = Slot("Kiri Dawn", "Kirilight", "Priest", "Holy", HEALER)
BENCH = ["Shadestep (Selm Voss)", "Ironhide (Eli Brant)"]


def roster() -> list[list[Slot]]:
    """Draft A after the swap, with both placeholders filled."""
    result = []
    for members in DRAFT_A:
        group = []
        for slot in members:
            if slot.character == "Shadestep":
                group.append(SWAP_IN)
            elif not slot.character:
                group.append(FILLS[slot.role])
            else:
                group.append(slot)
        result.append(group)
    return result


def groups(top: float, state_of: dict[str, str]) -> Group:
    items: list[Item] = []
    for index, members in enumerate(roster()):
        x = CONTENT_X + index * (GROUP_W + GROUP_GAP)
        group: list[Item] = [*card(x, top, GROUP_W, 40 + SLOT_H * len(members) + 4),
                             text("Title", x + 12, top + 26, f"Group {index + 1}", 13, 700, SECONDARY)]
        group += [slot_card(f"Place {position + 1}", x + 8, top + 40 + position * SLOT_H, slot,
                            state_of.get(slot.character, ""))
                  for position, slot in enumerate(members)]
        items.append(Group(f"Group {index + 1}", group))
    return Group("Groups", items)


def side_panel(x: float, top: float, width: float, title: str, lines: list[tuple[str, str]], height: float) -> Group:
    items: list[Item] = [*card(x, top, width, height), text("Title", x + 16, top + 30, title, 14, 700)]
    for index, (value, colour) in enumerate(lines):
        items.append(text(f"Line {index + 1}", x + 16, top + 56 + index * 20, value, 12, 400, colour))
    return Group(title, items)


def review() -> list[Item]:
    top = TOP
    panel_x = CONTENT_X + 5 * GROUP_W + 4 * GROUP_GAP + 20
    panel_w = CONTENT_X + CONTENT_W - panel_x
    changes = [(f"{name} {change}", TEXT) for name, change in CHANGES.items()]
    warnings = [("Frostweave needs a fresh sync", WARNING_TEXT),
                ("Spell critical taken +5% missing", WARNING_TEXT)]
    told = [("25 selected players", SECONDARY), ("2 benched players", SECONDARY), ("by Discord message", SECONDARY)]
    return [
        page_header(PAGE, "Review before publishing", "Draft A · " + WHEN),
        groups(top, {name: "changed" for name in CHANGES}),
        side_panel(panel_x, top, panel_w, "Changed", [(f"{len(CHANGES)} places", ACCENT), *[
            (name, TEXT) for name in CHANGES]], 140),
        side_panel(panel_x, top + 156, panel_w, "Still open", [("2 warnings", WARNING_TEXT)], 84),
        side_panel(panel_x, top + 256, panel_w, "Told when published", told, 124),
        Group("Changes detail", [
            *card(CONTENT_X, top + 304, 5 * GROUP_W + 4 * GROUP_GAP, 140),
            text("Title", CONTENT_X + 20, top + 334, "Since the published roster", 14, 700),
            *[text(f"Change {index + 1}", CONTENT_X + 20, top + 360 + index * 20, value, 13, 400, colour)
              for index, (value, colour) in enumerate(changes)],
            text("Warnings title", CONTENT_X + 460, top + 334, "Warnings still open", 14, 700),
            *[text(f"Warning {index + 1}", CONTENT_X + 460, top + 360 + index * 20, value, 13, 400, colour)
              for index, (value, colour) in enumerate(warnings)],
        ]),
        button("Publish button", CONTENT_X, top + 468, "Publish roster", "primary", 160, Click("navigate", PUBLISHED)),
        button("Back button", CONTENT_X + 168, top + 468, "Back to drafts", "secondary", 152),
        text("Publish note", CONTENT_X + 336, top + 493, "Publishing edits the raid's Discord post; it doesn't post a new one.",
             12, 400, MUTED),
    ]


def published() -> list[Item]:
    top = TOP + 56
    return [
        page_header(PAGE, TITLE, WHEN),
        Group("Publication status", [
            badge("Published badge", CONTENT_X, TOP, "Published · version 2", "success"),
            text("Published by", CONTENT_X + 190, TOP + 17, "Today at 15:10 by Gihed Annabi", 13, 400, SECONDARY),
            badge("Discord badge", CONTENT_X + 460, TOP, "Discord post updated", "info"),
        ]),
        Group("Discord link", [Rect("Area", CONTENT_X + 636, TOP - 4, 190, 32, P["Surface/page"]),
                               text("Label", CONTENT_X + 640, TOP + 17, "View the Discord post →", 13, 600, ACCENT)],
              Click("navigate", DISCORD)),
        groups(top, {}),
        text("Bench", CONTENT_X, top + 300, "Bench: " + ", ".join(BENCH), 13, 400, SECONDARY),
        button("Edit button", CONTENT_X + CONTENT_W - 136, CONTENT_TOP + 28, "Edit roster", "secondary", 136),
    ]


def discord_post() -> list[Item]:
    y = 72
    embed_y, width = y + 32, 720
    group_lines = [", ".join(slot.character for slot in members) for members in roster()]
    content: list[Item] = [
        *discord_embed(72, embed_y, width, 392),
        text("Embed title", 88, embed_y + 30, "Icecrown Citadel · roster", 16, 700, DISCORD_WHITE),
        text("Embed when", 88, embed_y + 52, "Friday 9 October 2026 21:00 · version 2", 14, 400, DISCORD_TEXT),
        *[item for index, line in enumerate(group_lines) for item in (
            text(f"Group {index + 1} title", 88, embed_y + 90 + index * 48, f"Group {index + 1}", 12, 700, DISCORD_WHITE),
            text(f"Group {index + 1} members", 88, embed_y + 110 + index * 48, line, 14, 400, DISCORD_TEXT))],
        text("Bench title", 88, embed_y + 340, "Bench", 12, 700, DISCORD_WHITE),
        text("Bench members", 88, embed_y + 360, ", ".join(BENCH), 14, 400, DISCORD_TEXT),
        text("Edited note", 88, embed_y + 382, "Edited today at 15:10: Kirilight replaces Shadestep; two places filled.",
             12, 400, DISCORD_MUTED),
        discord_button("Website button", 72, embed_y + 404, "Open the roster ↗"),
    ]
    return [discord_message("Roster post", y, "Today at 12:00 (edited)", content)]


def flagged() -> list[Item]:
    top = TOP + 96
    return [
        page_header(PAGE, TITLE, WHEN),
        notice("Flagged notice", CONTENT_X, TOP, CONTENT_W, "This published roster needs review",
               "Frostweave (Ilsa Brand) got an Icecrown Citadel save after publication. Nothing was replaced.", "danger"),
        groups(top, {FLAGGED_CHARACTER: "flagged"}),
        text("Kept note", CONTENT_X, top + 300,
             "The Discord post still shows the published roster until you publish again. Ilsa Brand and the officers were told.",
             13, 400, SECONDARY),
        button("Composer button", CONTENT_X, top + 324, "Review in the composer", "primary", 216),
        button("Readiness button", CONTENT_X + 224, top + 324, "View readiness", "secondary", 152),
    ]


def boards() -> list[Board]:
    right, below = BOARD_W + BOARD_GAP, BOARD_H + BOARD_GAP

    def screen(name: str, x: float, y: float, content: list[Item]) -> Board:
        return app_screen(name, x, y, PAGE, content, section="Publication")

    return [screen(REVIEW, 0, 0, review()), screen(PUBLISHED, right, 0, published()),
            discord_screen(DISCORD, 0, below, discord_post(), height=BOARD_H),
            screen(FLAGGED, right, below, flagged())]


def main(repository: Path = REPOSITORY) -> Path:
    return write_mockup(repository, "roster-publication", "Roster publication", boards(), PALETTE,
                        {"Publish a roster": REVIEW, "Roster flagged for review": FLAGGED})


if __name__ == "__main__":
    print("Wrote", main())
