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

Website states added by #513 (owner decisions on #513, 2026-10-04):
9. The code expired. 10. The code was already confirmed. 11. No pairing shows the code. 12. The page was opened
without a code. 13. The code can't be checked; Try again shows 1. Each offers "See paired companions", which shows 14.
14. Paired companions with one expired after 180 days unused, and a revocation that failed.
15. No paired companions yet. 16. The companions can't be loaded; Try again shows 2.
17. The confirmation failed, on the confirm page.

Uploads come with #384, so "Last upload" says "No upload yet" until then (owner decision on #513). Times are in UTC,
as on the character review page.

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
CODE_EXPIRED = "9 · Website: code expired"
CODE_USED = "10 · Website: code already confirmed"
CODE_UNKNOWN = "11 · Website: unknown code"
NO_CODE = "12 · Website: opened without a code"
CODE_FAILED = "13 · Website: code can't be checked"
EXPIRED_LIST = "14 · Website: expired companion, revoke failed"
EMPTY_LIST = "15 · Website: no paired companions"
LIST_FAILED = "16 · Website: companions can't be loaded"
CONFIRM_FAILED = "17 · Website: confirmation failed"
NO_UPLOAD = "No upload yet"
CONFIRM_SUBTITLE = "Check that the code matches the one on your companion, then confirm. Only pair a computer you use."
TRY_AGAIN = "Nothing changed. Try again in a moment."
INNER_W = WINDOW_W - 2 * WINDOW_PADDING


def label(name: str, x: float, y: float, value: str) -> Item:
    return text(name, x, y, value, 11, 700, MUTED, None, "left", 1.2)


def confirm() -> list[Item]:
    top, width = CONTENT_TOP + 120, 560
    x = CONTENT_X + 24
    return [
        page_header("Companion", "Pair this companion", CONFIRM_SUBTITLE),
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


def companions_table(revoked: bool, linked: bool, expired: bool = False) -> Group:
    top, head_h, row_h = CONTENT_TOP + 120, 44, 72
    columns = {"computer": 24, "paired": 300, "upload": 520, "status": 760, "action": 900}
    laptop_status = "Active"
    if expired:
        laptop_status = "Expired"
    elif revoked:
        laptop_status = "Revoked"
    laptop_paired = "12 Mar, 21:40 UTC" if expired else "12 Sep, 21:40 UTC"
    rows = [(DESKTOP, "Today, 14:02 UTC", "Active"), (LAPTOP, laptop_paired, laptop_status)]
    items: list[Item] = card(CONTENT_X, top, CONTENT_W, head_h + row_h * len(rows) + 8)
    items.append(Group("Table header", [
        label(f"{heading.title()} heading", CONTENT_X + columns[key], top + 27, heading)
        for key, heading in (("computer", "COMPUTER"), ("paired", "PAIRED"), ("upload", "LAST UPLOAD"),
                             ("status", "STATUS"))]))
    for index, (computer, paired, status) in enumerate(rows):
        y = top + head_h + index * row_h
        row: list[Item] = [
            Rect("Divider", CONTENT_X, y, CONTENT_W, 1, DIVIDER),
            text("Computer", CONTENT_X + columns["computer"], y + 41, computer, 14, 600),
            text("Paired", CONTENT_X + columns["paired"], y + 41, paired, 13, 400, SECONDARY),
            text("Last upload", CONTENT_X + columns["upload"], y + 41, NO_UPLOAD, 13, 400, MUTED),
            badge("Status badge", CONTENT_X + columns["status"], y + 24, status,
                  "success" if status == "Active" else "neutral"),
        ]
        if status == "Revoked":
            row.append(text("Revoked at", CONTENT_X + columns["action"], y + 41, "Revoked today, 17:20 UTC", 13, 400,
                            MUTED))
        elif status == "Expired":
            row.append(text("Expired note", CONTENT_X + columns["action"], y + 41, "Unused for 180 days", 13, 400, MUTED))
        else:
            target = Click("navigate", REVOKE) if linked and computer == LAPTOP else None
            row.append(button("Revoke button", CONTENT_X + columns["action"], y + 16, "Revoke", "secondary", 96, target))
        items.append(Group(f"Row {computer}", row))
    return Group("Companions table", items)


def toast(title: str, message: str, accent: str = ACCENT) -> Group:
    x = BOARD_W - 40 - 380
    return Group("Notification", [*card(x, 80, 380, 72, accent),
                                  text("Title", x + 24, 111, title, 14, 600),
                                  text("Message", x + 24, 133, message, 13, 400, SECONDARY)])


def companions_header() -> Group:
    return page_header("Companion", "Paired companions",
                       "These computers can upload your character data. Revoke one you no longer use.")


def companions(revoked: bool = False, linked: bool = True, expired: bool = False) -> list[Item]:
    return [
        companions_header(),
        companions_table(revoked, linked, expired),
        text("Pairing note", CONTENT_X, CONTENT_TOP + 120 + 44 + 2 * 72 + 44,
             "To pair another computer, start pairing in its companion.", 13, 400, MUTED),
    ]


def code_problem(title: str, message: str, tone: str, retry: bool = False) -> list[Item]:
    """The confirm page when the code can't be confirmed: a notice, and where to go next."""
    top, width = CONTENT_TOP + 120, 560
    items: list[Item] = [page_header("Companion", "Pair this companion", CONFIRM_SUBTITLE),
                         notice("Code notice", CONTENT_X, top, width, title, message, tone)]
    x = CONTENT_X
    if retry:
        items.append(button("Try again button", x, top + 96, "Try again", "primary", 112, Click("navigate", CONFIRM)))
        x += 120
    items.append(button("Companions button", x, top + 96, "See paired companions", "secondary", None,
                        Click("navigate", EXPIRED_LIST)))
    return items


def empty_companions() -> list[Item]:
    top = CONTENT_TOP + 120
    return [
        companions_header(),
        Group("Empty state", [
            *card(CONTENT_X, top, CONTENT_W, 136),
            text("Title", CONTENT_X, top + 56, "No paired computers yet", 18, 700, P["Text/primary"], CONTENT_W,
                 "center"),
            text("Message", CONTENT_X, top + 88, "To pair a computer, start pairing in its companion.", 14, 400,
                 SECONDARY, CONTENT_W, "center"),
        ]),
    ]


def companions_failed() -> list[Item]:
    top = CONTENT_TOP + 120
    return [
        companions_header(),
        notice("Error message", CONTENT_X, top, CONTENT_W, "We couldn't load your companions",
               "Nothing was revoked. Try again in a moment.", "danger"),
        button("Try again button", CONTENT_X, top + 96, "Try again", "primary", 112, Click("navigate", PAIRED_LIST)),
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
    danger = P["Status/danger"]

    def website(name: str, x: float, content: list[Item], y: float = 0) -> Board:
        return app_screen(name, x, y, PAGE, content, user=PLAYER)

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
        website(CODE_EXPIRED, 0, code_problem(
            "This code expired", "Codes last 10 minutes. Get a new code in the companion.", "warning"), 2 * row),
        website(CODE_USED, column, code_problem(
            "This code was already confirmed", "If you confirmed it, the companion is paired. If not, get a new code.",
            "info"), 2 * row),
        website(CODE_UNKNOWN, 2 * column, code_problem(
            "No companion is waiting for this code", "Check the code on your companion, or get a new one there.",
            "warning"), 2 * row),
        website(NO_CODE, 3 * column, code_problem(
            "Start pairing in the companion", "It shows a code and opens this page with it.", "info"), 2 * row),
        website(CODE_FAILED, 0, code_problem(
            "We couldn't check this code", "Nothing was paired. Try again in a moment.", "danger", retry=True), 3 * row),
        website(EXPIRED_LIST, column, [*companions(linked=False, expired=True),
                                       toast(f"{DESKTOP} wasn't revoked", TRY_AGAIN, danger)], 3 * row),
        website(EMPTY_LIST, 2 * column, empty_companions(), 3 * row),
        website(LIST_FAILED, 3 * column, companions_failed(), 3 * row),
        website(CONFIRM_FAILED, 0, [*confirm(), toast(f"{DESKTOP} wasn't paired", TRY_AGAIN, danger)], 4 * row),
    ]


def main(repository: Path = REPOSITORY) -> Path:
    return write_mockup(repository, "companion-pairing", "Companion pairing", boards(),
                        flows={"Pair a companion": WAITING, "Revoke a companion": PAIRED_LIST,
                               "Expired code": EXPIRED, "Revoked companion": UNPAIRED,
                               "Code problems on the website": CODE_EXPIRED,
                               "Companions can't be loaded": LIST_FAILED})


if __name__ == "__main__":
    print("Wrote", main())
