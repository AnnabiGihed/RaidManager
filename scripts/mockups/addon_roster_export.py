"""The roster export to the WoW addon (story #35, design story #195, task #245).

Following ADR-0021, the roster reaches the addon as a pasted code. Five boards, linked as a clickable prototype. The
roster is the published roster (version 2) from the earlier roster mockups:

1. Website: the code for the published roster, the characters it contains, and those left out (locked or
   unapproved) with the reason. Copy code opens 2.
2. In game: `/raidmanager import` opens a box with the pasted code. Import opens 3.
3. In game: the roster frame, by group, showing who is in the raid, invited or not invited yet, the bench and the
   characters left out. Invitations happen only on a click.
4. In game: a code for an older published version refused. Paste again opens 2.
5. Website: no code while a raid's roster isn't published.

Run from the repository root: python scripts/mockups/addon_roster_export.py
Once the owner edits the design in Penpot, the downloaded file replaces docs/mockups/addon-roster-export.penpot and
this script is no longer run.
"""

from __future__ import annotations

import sys
from pathlib import Path

REPOSITORY = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(REPOSITORY / "scripts"))

from penpot_components import (  # noqa: E402
    ADDON_TITLE_H, BOARD_GAP, BOARD_H, BOARD_W, CONTENT_TOP, CONTENT_W, CONTENT_X, addon_frame, app_screen, button,
    card, notice, page_header,
)
from penpot_scene import HOUSE_PALETTE, Board, Click, Group, Item, Rect, text, write_mockup  # noqa: E402
from roster_data import PALETTE_ADDITIONS, WHEN  # noqa: E402
from roster_publication import roster  # noqa: E402

P = HOUSE_PALETTE
PALETTE = {**HOUSE_PALETTE, **PALETTE_ADDITIONS}
ACCENT, SECONDARY, MUTED, DIVIDER = P["Brand/accent"], P["Text/secondary"], P["Text/muted"], P["Line/divider"]
TEXT, RAISED = P["Text/primary"], P["Surface/raised"]
DANGER_TEXT = P["Status/danger text"]
PAGE = "Raid night"
SECTION = "Addon export"
LEFT_OUT = {"Frostweave": "Ilsa Brand · locked through raid since 16:40",
            "Tidefist": "Bram Oakes · claim not approved, in conflict review"}
CODE_LINES = ["RM1;ICC25H;2026-10-09T19:00Z;v2;",
              "Stonebrow-Icecrown:1:T;Tomasdk-Icecrown:1:T;",
              "Arthasdk-Icecrown:1:M;Kirilight-Icecrown:1:H;…"]
IN_GAME_W, IN_GAME_H = 520, 680
IMPORT_TITLE = "RaidManager · Import roster"

EXPORT = "1 · Website: code for the addon"
PASTE = "2 · In game: paste the code"
FRAME = "3 · In game: roster frame"
STALE = "4 · In game: older code refused"
UNPUBLISHED = "5 · Website: roster not published"


def invite_list() -> list[list[str]]:
    return [[slot.character for slot in members if slot.character not in LEFT_OUT] for members in roster()]


def export() -> list[Item]:
    groups = invite_list()
    count = sum(len(members) for members in groups)
    left_w = 600
    contents: list[Item] = [*card(CONTENT_X, CONTENT_TOP + 120, left_w, 52 + 40 * len(groups) + 8),
                            text("Title", CONTENT_X + 20, CONTENT_TOP + 154, f"{count} characters to invite", 16, 600)]
    for index, members in enumerate(groups):
        y = CONTENT_TOP + 172 + index * 40
        contents += [text(f"Group {index + 1} label", CONTENT_X + 20, y + 24, f"Group {index + 1}", 13, 700, SECONDARY),
                     text(f"Group {index + 1} members", CONTENT_X + 100, y + 24, ", ".join(members), 13)]
    left_top = CONTENT_TOP + 120 + 52 + 40 * len(groups) + 24
    left_out: list[Item] = [*card(CONTENT_X, left_top, left_w, 52 + 40 * len(LEFT_OUT) + 8, P["Status/danger"]),
                            text("Title", CONTENT_X + 24, left_top + 34, "Left out of the code", 16, 600)]
    for index, (character, reason) in enumerate(LEFT_OUT.items()):
        y = left_top + 52 + index * 40
        left_out += [text(f"{character} name", CONTENT_X + 24, y + 22, character, 14, 600),
                     text(f"{character} reason", CONTENT_X + 140, y + 22, reason, 13, 400, SECONDARY)]
    code_x, code_w = CONTENT_X + left_w + 20, CONTENT_W - left_w - 20
    code: list[Item] = [
        *card(code_x, CONTENT_TOP + 120, code_w, 340),
        text("Title", code_x + 20, CONTENT_TOP + 154, "Code for the addon", 16, 600),
        text("Version", code_x + 20, CONTENT_TOP + 176, "Published version 2 · today at 15:10", 12, 400, MUTED),
        Rect("Code box", code_x + 20, CONTENT_TOP + 192, code_w - 40, 84, RAISED, 1, 8, DIVIDER),
        *[text(f"Code line {index + 1}", code_x + 34, CONTENT_TOP + 218 + index * 22, line, 12, 400, SECONDARY)
          for index, line in enumerate(CODE_LINES)],
        button("Copy button", code_x + 20, CONTENT_TOP + 292, "Copy code", "primary", 132, Click("navigate", PASTE)),
        text("How", code_x + 20, CONTENT_TOP + 364, "In game, type /raidmanager import and paste it.", 13, 400, SECONDARY),
        text("New version", code_x + 20, CONTENT_TOP + 386, "Publishing again makes a new code; the addon refuses", 12,
             400, MUTED),
        text("New version 2", code_x + 20, CONTENT_TOP + 404, "this one after that.", 12, 400, MUTED),
    ]
    return [page_header(PAGE, "Roster for the addon", WHEN), Group("Contents", contents), Group("Left out", left_out),
            Group("Code", code)]


def paste() -> list[Item]:
    top = ADDON_TITLE_H + 24
    return [
        text("Intro", 20, top + 16, "Paste the code from the website's raid-night page.", 13, 400, SECONDARY),
        Rect("Edit box", 20, top + 32, IN_GAME_W - 40, 120, RAISED, 1, 6, P["Brand/accent"]),
        *[text(f"Pasted line {index + 1}", 32, top + 58 + index * 22, line, 12, 400, TEXT)
          for index, line in enumerate(CODE_LINES)],
        button("Import button", 20, top + 172, "Import", "primary", 112, Click("navigate", FRAME)),
        button("Cancel button", 140, top + 172, "Cancel", "secondary", 96),
        text("Safety", 20, top + 244, "The code holds names, realms, groups and roles only.", 12, 400, MUTED),
    ]


def frame() -> list[Item]:
    groups = invite_list()
    statuses = ["In raid"] * 9 + ["Invited"] * 2 + ["Not invited"] * 30
    colours = {"In raid": ACCENT, "Invited": P["Status/warning title"], "Not invited": MUTED}
    top = ADDON_TITLE_H + 16
    items: list[Item] = [
        text("Raid", 16, top + 16, "Icecrown Citadel 25 heroic · Fri 21:00", 13, 700, TEXT),
        text("Counts", 16, top + 36, "Version 2 · 9 in raid · 2 invited · 12 not invited", 12, 400, SECONDARY),
    ]
    column_w = (IN_GAME_W - 48) / 2
    index = 0
    for column, group_numbers in enumerate(((0, 1, 2), (3, 4))):
        x = 16 + column * (column_w + 16)
        y = top + 56
        for group_number in group_numbers:
            rows: list[Item] = [text("Label", x, y + 16, f"Group {group_number + 1}", 12, 700, SECONDARY)]
            y += 22
            for character in groups[group_number]:
                state = statuses[index]
                index += 1
                rows += [text(f"{character} name", x + 8, y + 14, character, 12, 600, TEXT),
                         text(f"{character} state", x + column_w - 90, y + 14, state, 11, 400, colours[state], 90, "right")]
                y += 20
            items.append(Group(f"Group {group_number + 1}", rows))
            y += 8
    bottom = top + 56 + 22 * 3 + 20 * 14 + 24
    items += [
        Rect("Divider", 16, bottom, IN_GAME_W - 32, 1, DIVIDER),
        text("Left out title", 16, bottom + 24, "Not invited by the addon", 12, 700, DANGER_TEXT),
        *[text(f"{character} left out", 16, bottom + 44 + position * 18, f"{character}: {reason.split(' · ')[1]}", 12,
               400, SECONDARY) for position, (character, reason) in enumerate(LEFT_OUT.items())],
        text("Bench", 16, bottom + 98, "Bench: Shadestep, Ironhide", 12, 400, SECONDARY),
        button("Invite button", 16, IN_GAME_H - 64, "Invite the 12 not invited", "primary", 220),
        text("Click note", 248, IN_GAME_H - 38, "Invites only when you click; never in combat.", 11, 400, MUTED),
    ]
    return items


def stale() -> list[Item]:
    top = ADDON_TITLE_H + 24
    return [
        Rect("Error box", 20, top + 16, IN_GAME_W - 40, 96, P["Status/danger background"], 1, 8, P["Status/danger border"]),
        text("Error title", 36, top + 44, "This code is for version 1", 14, 700, DANGER_TEXT),
        text("Error line 1", 36, top + 66, "Version 2 was published today at 15:10. Copy the new", 12, 400,
             DANGER_TEXT),
        text("Error line 2", 36, top + 86, "code from the website and paste it again.", 12, 400, DANGER_TEXT),
        button("Again button", 20, top + 132, "Paste again", "primary", 132, Click("navigate", PASTE)),
        text("Kept", 20, top + 204, "Nothing was imported; the roster frame isn't changed.", 12, 400, MUTED),
    ]


def unpublished() -> list[Item]:
    return [
        page_header(PAGE, "Roster for the addon", "Weekly Vault run · Sat 10 Oct, 21:00 · 19:00 UTC"),
        notice("Unpublished notice", CONTENT_X, CONTENT_TOP + 120, CONTENT_W, "Publish the roster first",
               "The addon code is made from the published roster. This raid only has drafts, so there's nothing to invite.",
               "warning"),
        button("Builder button", CONTENT_X, CONTENT_TOP + 216, "Open the roster builder", "primary", 212),
    ]


def boards() -> list[Board]:
    right, below = BOARD_W + BOARD_GAP, BOARD_H + BOARD_GAP
    step = IN_GAME_W + BOARD_GAP

    def screen(name: str, x: float, content: list[Item]) -> Board:
        return app_screen(name, x, 0, PAGE, content, section=SECTION)

    def in_game(name: str, index: int, title: str, content: list[Item], height: float = IN_GAME_H) -> Board:
        return addon_frame(name, index * step, below, title, content, width=IN_GAME_W, height=height)

    return [screen(EXPORT, 0, export()), screen(UNPUBLISHED, right, unpublished()),
            in_game(PASTE, 0, IMPORT_TITLE, paste(), 380),
            in_game(FRAME, 1, "RaidManager · Roster", frame()),
            in_game(STALE, 2, IMPORT_TITLE, stale(), 300)]


def main(repository: Path = REPOSITORY) -> Path:
    return write_mockup(repository, "addon-roster-export", "Addon roster export", boards(), PALETTE,
                        {"Export and invite": EXPORT, "Older code refused": STALE})


if __name__ == "__main__":
    print("Wrote", main())
