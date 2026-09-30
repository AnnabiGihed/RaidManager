"""Companion pairing and revocation (story #15, design story #179, task #215).

The flow follows the pairing sequence (docs/diagrams/sequence-pairing.mmd): the companion starts a short-lived
request and shows a code, the signed-in player confirms that code on the website, and the player can revoke the
companion from the website. Eight boards in RaidManager's design system (ADR-0019), linked as a clickable prototype:

Website, in the app shell:
1. Confirm the code. Confirm pairing shows 2.
2. Paired companions, after confirming. Revoke on the laptop opens 3.
3. Revoke confirmation. Cancel goes back to 2, Revoke shows 4.
4. Paired companions after revoking.

Companion, in its desktop window:
5. Waiting, with the code. Open the website shows 1.
6. Paired.
7. The code expired. Get a new code shows 5.
8. The pairing was revoked on the website. Pair again shows 5.

Run from the repository root: python scripts/mockups/companion_pairing.py
Once the owner edits the design in Penpot, the downloaded file replaces docs/mockups/companion-pairing.penpot and
this script is no longer run.
"""

from __future__ import annotations

import sys
from pathlib import Path

REPOSITORY = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(REPOSITORY / "scripts"))

from penpot_components import (  # noqa: E402
    BOARD_GAP, BOARD_H, BOARD_W, CONTENT_TOP, CONTENT_W, CONTENT_X, PLAYER, TITLE_BAR_H, WINDOW_PADDING, WINDOW_W,
    app_screen, badge, button, card, companion_window, notice, page_header,
)
from penpot_scene import HOUSE_PALETTE, Board, Circle, Click, Group, Item, Rect, text, write_mockup  # noqa: E402

P = HOUSE_PALETTE
ACCENT, SECONDARY, MUTED, DIVIDER = P["Brand/accent"], P["Text/secondary"], P["Text/muted"], P["Line/divider"]
PAGE = "Companion & sync"
CODE = "K7M-4QX"
DESKTOP, LAPTOP = "BRYN-DESKTOP", "BRYN-LAPTOP"
CODE_LABEL = "PAIRING CODE"

CONFIRM = "1 · Website: confirm the code"
PAIRED_LIST = "2 · Website: paired companions"
REVOKE = "3 · Website: revoke confirmation"
REVOKED_LIST = "4 · Website: after revoking"
WAITING = "5 · Companion: waiting"
PAIRED = "6 · Companion: paired"
EXPIRED = "7 · Companion: code expired"
UNPAIRED = "8 · Companion: pairing revoked"
INNER_W = WINDOW_W - 2 * WINDOW_PADDING


def label(name: str, x: float, y: float, value: str) -> Item:
    return text(name, x, y, value, 11, 700, MUTED, None, "left", 1.2)


def confirm() -> list[Item]:
    top, width = CONTENT_TOP + 120, 560
    x = CONTENT_X + 24
    return [
        page_header("Companion", "Pair this companion",
                    "Check that the code matches the one on your companion, then confirm. Only pair a computer you use."),
        Group("Code card", [
            *card(CONTENT_X, top, width, 300),
            label("Code label", x, top + 36, CODE_LABEL),
            text("Code", x, top + 84, CODE, 32, 700, ACCENT, None, "left", 4),
            text("Expiry", x, top + 110, "Expires in 9 minutes", 13, 400, SECONDARY),
            label("Computer label", x, top + 156, "COMPUTER"),
            text("Computer", x, top + 180, DESKTOP, 14),
            button("Confirm button", x, top + 220, "Confirm pairing", "primary", 160, Click("navigate", PAIRED_LIST)),
            button("Cancel button", x + 168, top + 220, "Cancel", "secondary", 96),
        ]),
    ]


def companions_table(revoked: bool, linked: bool) -> Group:
    top, head_h, row_h = CONTENT_TOP + 120, 44, 72
    columns = {"computer": 24, "paired": 300, "upload": 520, "status": 760, "action": 900}
    rows = [(DESKTOP, "Today, 14:02", "Today, 14:05", False), (LAPTOP, "12 September", "28 September, 21:40", revoked)]
    items: list[Item] = card(CONTENT_X, top, CONTENT_W, head_h + row_h * len(rows) + 8)
    items.append(Group("Table header", [
        label(f"{heading.title()} heading", CONTENT_X + columns[key], top + 27, heading)
        for key, heading in (("computer", "COMPUTER"), ("paired", "PAIRED"), ("upload", "LAST UPLOAD"),
                             ("status", "STATUS"))]))
    for index, (computer, paired, upload, is_revoked) in enumerate(rows):
        y = top + head_h + index * row_h
        row: list[Item] = [
            Rect("Divider", CONTENT_X, y, CONTENT_W, 1, DIVIDER),
            text("Computer", CONTENT_X + columns["computer"], y + 41, computer, 14, 600),
            text("Paired", CONTENT_X + columns["paired"], y + 41, paired, 13, 400, SECONDARY),
            text("Last upload", CONTENT_X + columns["upload"], y + 41, upload, 13, 400, SECONDARY),
            badge("Status badge", CONTENT_X + columns["status"], y + 24, "Revoked" if is_revoked else "Active",
                  "neutral" if is_revoked else "success"),
        ]
        if is_revoked:
            row.append(text("Revoked at", CONTENT_X + columns["action"], y + 41, "Revoked today, 17:20", 13, 400, MUTED))
        else:
            target = Click("navigate", REVOKE) if linked and computer == LAPTOP else None
            row.append(button("Revoke button", CONTENT_X + columns["action"], y + 16, "Revoke", "secondary", 96, target))
        items.append(Group(f"Row {computer}", row))
    return Group("Companions table", items)


def toast(title: str, message: str) -> Group:
    x = BOARD_W - 40 - 380
    return Group("Notification", [*card(x, 80, 380, 72, ACCENT),
                                  text("Title", x + 24, 111, title, 14, 600),
                                  text("Message", x + 24, 133, message, 13, 400, SECONDARY)])


def companions(revoked: bool = False, linked: bool = True) -> list[Item]:
    return [
        page_header("Companion", "Paired companions",
                    "These computers can upload your character data. Revoke one you no longer use."),
        companions_table(revoked, linked),
        text("Pairing note", CONTENT_X, CONTENT_TOP + 120 + 44 + 2 * 72 + 44,
             "To pair another computer, start pairing in its companion.", 13, 400, MUTED),
    ]


def revoke_dialog() -> list[Item]:
    w, h = 480, 236
    x, y = (BOARD_W - w) / 2, (BOARD_H - h) / 2
    body = ["It stops uploading at once. Characters and snapshots it", "already uploaded stay in RaidManager.",
            "Pair it again to resume."]
    return [
        Rect("Dim overlay", 0, 0, BOARD_W, BOARD_H, P["Neutral/black"], 0.6),
        Group("Revoke dialog", [
            *card(x, y, w, h),
            text("Title", x + 24, y + 44, f"Revoke {LAPTOP}?", 18, 700),
            *[text(f"Body line {index + 1}", x + 24, y + 80 + index * 20, line, 14, 400, SECONDARY)
              for index, line in enumerate(body)],
            button("Cancel button", x + w - 24 - 96 - 8 - 96, y + h - 64, "Cancel", "secondary", 96,
                   Click("navigate", PAIRED_LIST)),
            button("Confirm revoke button", x + w - 24 - 96, y + h - 64, "Revoke", "danger", 96,
                   Click("navigate", REVOKED_LIST)),
        ]),
    ]


def window_heading(title: str, lines: list[str], top: float = TITLE_BAR_H + 52) -> list[Item]:
    items: list[Item] = [text("Heading", WINDOW_PADDING, top, title, 18, 700)]
    items += [text(f"Intro line {index + 1}", WINDOW_PADDING, top + 30 + index * 20, line, 14, 400, SECONDARY)
              for index, line in enumerate(lines)]
    return items


def code_card(y: float, expired: bool) -> Group:
    colour = MUTED if expired else ACCENT
    return Group("Code card", [
        *card(WINDOW_PADDING, y, INNER_W, 124),
        label("Code label", WINDOW_PADDING + 24, y + 32, CODE_LABEL),
        text("Code", WINDOW_PADDING, y + 78, CODE, 32, 700, colour, INNER_W, "center", 4),
        text("Expiry", WINDOW_PADDING, y + 104, "Expired" if expired else "Expires in 9:42", 13, 400,
             MUTED if expired else SECONDARY, INNER_W, "center"),
    ])


def waiting() -> list[Item]:
    return [
        *window_heading("Pair with RaidManager", ["Open the website, sign in with Discord, and", "confirm this code."]),
        code_card(TITLE_BAR_H + 128, False),
        Group("Status", [Circle("Dot", WINDOW_PADDING + 5, TITLE_BAR_H + 297, 5, P["Accent/amber"]),
                         text("Label", WINDOW_PADDING + 18, TITLE_BAR_H + 302,
                              "Waiting for confirmation on the website", 13, 400, SECONDARY)]),
        button("Open website button", WINDOW_PADDING, TITLE_BAR_H + 336, "Open the website", "primary", INNER_W,
               Click("navigate", CONFIRM)),
        button("New code button", WINDOW_PADDING, TITLE_BAR_H + 388, "Get a new code", "secondary", INNER_W),
    ]


def paired() -> list[Item]:
    middle = WINDOW_W / 2
    top = TITLE_BAR_H + 72
    return [
        Circle("Success circle", middle, top + 36, 36, P["Status/success background"]),
        text("Success mark", middle - 20, top + 50, "✓", 40, 800, ACCENT, 40, "center", icon=True),
        text("Heading", 0, top + 124, f"Paired with {PLAYER.name}", 18, 700, P["Text/primary"], WINDOW_W, "center"),
        text("Message line 1", 0, top + 154, "This computer can now upload your character data.", 14, 400,
             SECONDARY, WINDOW_W, "center"),
        text("Message line 2", 0, top + 174, "Next, choose the WoW folders to watch.", 14, 400, SECONDARY, WINDOW_W,
             "center"),
        button("Choose folders button", WINDOW_PADDING, top + 212, "Choose folders", "primary", INNER_W),
        text("Revoke note", 0, top + 288, f"You can revoke it anytime on the website, under {PAGE}.", 12, 400,
             MUTED, WINDOW_W, "center"),
    ]


def expired() -> list[Item]:
    return [
        *window_heading("Pair with RaidManager", ["This code can't be confirmed anymore."]),
        code_card(TITLE_BAR_H + 108, True),
        notice("Expired notice", WINDOW_PADDING, TITLE_BAR_H + 256, INNER_W, "This code expired",
               "Codes last 10 minutes. Get a new one to try again.", "warning"),
        button("New code button", WINDOW_PADDING, TITLE_BAR_H + 352, "Get a new code", "primary", INNER_W,
               Click("navigate", WAITING)),
    ]


def unpaired() -> list[Item]:
    return [
        *window_heading("Uploads stopped", [f"{DESKTOP} isn't paired with your account anymore."]),
        notice("Revoked notice", WINDOW_PADDING, TITLE_BAR_H + 108, INNER_W, "This companion was revoked",
               "It was revoked on the website, so it can't upload.", "danger"),
        button("Pair again button", WINDOW_PADDING, TITLE_BAR_H + 204, "Pair again", "primary", INNER_W,
               Click("navigate", WAITING)),
        text("Kept note", WINDOW_PADDING, TITLE_BAR_H + 284, "Data it already uploaded stays in RaidManager.", 12,
             400, MUTED),
    ]


def boards() -> list[Board]:
    column, row = BOARD_W + BOARD_GAP, BOARD_H + BOARD_GAP
    window = WINDOW_W + BOARD_GAP

    def website(name: str, x: float, content: list[Item]) -> Board:
        return app_screen(name, x, 0, PAGE, content, user=PLAYER)

    return [
        website(CONFIRM, 0, confirm()),
        website(PAIRED_LIST, column, [*companions(), toast(f"{DESKTOP} is paired", "It can upload your character data now.")]),
        website(REVOKE, 2 * column, [*companions(linked=False), *revoke_dialog()]),
        website(REVOKED_LIST, 3 * column, [*companions(revoked=True),
                                          toast(f"{LAPTOP} was revoked", "It can't upload until it's paired again.")]),
        companion_window(WAITING, 0, row, waiting()),
        companion_window(PAIRED, window, row, paired()),
        companion_window(EXPIRED, 2 * window, row, expired()),
        companion_window(UNPAIRED, 3 * window, row, unpaired()),
    ]


def main(repository: Path = REPOSITORY) -> Path:
    return write_mockup(repository, "companion-pairing", "Companion pairing", boards(),
                        flows={"Pair a companion": WAITING, "Revoke a companion": PAIRED_LIST,
                               "Expired code": EXPIRED, "Revoked companion": UNPAIRED})


if __name__ == "__main__":
    print("Wrote", main())
