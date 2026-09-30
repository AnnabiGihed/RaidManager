"""Companion folder discovery and sync status (story #17, design story #180, task #217).

The desktop companion watches the SavedVariables of selected World of Warcraft accounts and uploads complete
snapshots (docs/explanation/architecture.md). Five companion windows in RaidManager's design system (ADR-0019),
linked as a clickable prototype:

1. Watched folders: the discovered installations and their account folders, one excluded. Save and sync shows 2.
2. Syncing: last success, queued uploads, an upload in progress, and a character waiting for WoW to finish
   writing. Pause sync shows 3; Watched folders shows 1.
3. Paused. Resume sync shows 2.
4. Offline: uploads wait in the queue. Retry now shows 2.
5. An incomplete SavedVariables write, with how to fix it and a retry. Retry shows 2.

Run from the repository root: python scripts/mockups/companion_sync.py
Once the owner edits the design in Penpot, the downloaded file replaces docs/mockups/companion-sync.penpot and this
script is no longer run.
"""

from __future__ import annotations

import sys
from pathlib import Path

REPOSITORY = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(REPOSITORY / "scripts"))

from penpot_components import (  # noqa: E402
    BOARD_GAP, PLAYER, TITLE_BAR_H, WINDOW_PADDING, badge, button, card, checkbox, companion_window, notice,
)
from penpot_scene import HOUSE_PALETTE, Board, Circle, Click, Group, Item, Rect, text, write_mockup  # noqa: E402

P = HOUSE_PALETTE
ACCENT, SECONDARY, MUTED, DIVIDER = P["Brand/accent"], P["Text/secondary"], P["Text/muted"], P["Line/divider"]
WIDTH, HEIGHT = 560, 680
INNER_W = WIDTH - 2 * WINDOW_PADDING
X = WINDOW_PADDING
COMPUTER = "BRYN-DESKTOP"
LAST_SUCCESS = "Today, 14:05"
SYNC_TITLE = "Sync"

FOLDERS = "1 · Watched folders"
SYNCING = "2 · Syncing"
PAUSED = "3 · Paused"
OFFLINE = "4 · Offline"
INCOMPLETE = "5 · Incomplete snapshot"

INSTALLATIONS = [
    (r"C:\Games\Warmane\World of Warcraft", [("ARTHASACC", 3, True), ("JAINAACC", 2, True), ("ALTACC", 4, False)]),
    (r"D:\WoW\Warmane", [("THRALLACC", 1, True)]),
]


def label(name: str, x: float, y: float, value: str) -> Item:
    return text(name, x, y, value, 11, 700, MUTED, None, "left", 1.2)


def heading(title: str, lines: list[str], status: tuple[str, str] | None = None) -> list[Item]:
    top = TITLE_BAR_H + 52
    items: list[Item] = [text("Heading", X, top, title, 18, 700)]
    if status:
        items.append(badge("Status badge", X + INNER_W - (len(status[0]) * 7 + 24), top - 18, status[0], status[1]))
    items += [text(f"Intro line {index + 1}", X, top + 28 + index * 20, line, 14, 400, SECONDARY)
              for index, line in enumerate(lines)]
    return items


def footer() -> Group:
    y = HEIGHT - 44
    return Group("Pairing footer", [
        Rect("Divider", 0, y, WIDTH, 1, DIVIDER),
        Circle("Dot", X + 4, y + 22, 4, ACCENT),
        text("Label", X + 16, y + 27, f"Paired with {PLAYER.name} \u00b7 {COMPUTER}", 12, 400, SECONDARY),
    ])


def installation(index: int, y: float, path: str, accounts: list[tuple[str, int, bool]]) -> Group:
    row_h = 44
    height = 64 + row_h * len(accounts) + 8
    items: list[Item] = [*card(X, y, INNER_W, height), label("Installation label", X + 20, y + 28, "INSTALLATION"),
                         text("Path", X + 20, y + 50, path, 13, 600)]
    for row, (account, characters, watched) in enumerate(accounts):
        ry = y + 64 + row * row_h
        count = f"{characters} character" + ("" if characters == 1 else "s")
        items.append(Group(f"Account {account}", [
            Rect("Divider", X, ry, INNER_W, 1, DIVIDER),
            *checkbox(X + 20, ry + 13, watched),
            text("Account", X + 50, ry + 27, account, 14, 600 if watched else 400,
                 P["Text/primary"] if watched else MUTED),
            text("Characters", X + INNER_W - 140, ry + 27, count if watched else f"Excluded \u00b7 {count}", 12, 400,
                 MUTED, 120, "right"),
        ]))
    return Group(f"Installation {index}", items)


def folders() -> list[Item]:
    items = heading("Watched folders", ["RaidManager found these World of Warcraft folders. Clear",
                                        "an account to keep its characters out of RaidManager."])
    y = TITLE_BAR_H + 112
    for index, (path, accounts) in enumerate(INSTALLATIONS):
        items.append(installation(index + 1, y, path, accounts))
        y += 64 + 44 * len(accounts) + 8 + 16
    items += [button("Save button", X, y + 8, "Save and sync", "primary", 180, Click("navigate", SYNCING)),
              button("Find again button", X + 188, y + 8, "Find folders again", "secondary", 180), footer()]
    return items


def stats(y: float, queued: int) -> Group:
    column = INNER_W / 3
    values = (("LAST SUCCESS", LAST_SUCCESS), ("QUEUED", f"{queued} snapshots"), ("ACCOUNTS", "3 of 4 watched"))
    items: list[Item] = card(X, y, INNER_W, 84)
    for index, (name, value) in enumerate(values):
        cx = X + 20 + index * column
        items += [label(f"{name.title()} label", cx, y + 32, name), text(f"{name.title()} value", cx, y + 58, value, 15, 600)]
    return Group("Sync stats", items)


def activity(y: float) -> Group:
    row_h = 52
    rows: list[tuple[str, str, str]] = [
        ("Arthasdk", "Icecrown", "uploading"), ("Jainaice", "Lordaeron", "waiting"),
        ("Thrallsham", "Icecrown", "done"), ("Sylvanash", "Icecrown", "done")]
    items: list[Item] = [*card(X, y, INNER_W, 60 + row_h * len(rows) + 8),
                         text("Title", X + 20, y + 36, "Recent activity", 16, 600)]
    for index, (name, realm, state) in enumerate(rows):
        ry = y + 60 + index * row_h
        row: list[Item] = [Rect("Divider", X, ry, INNER_W, 1, DIVIDER),
                           text("Character", X + 20, ry + 23, name, 14, 600),
                           text("Realm", X + 20, ry + 41, realm, 12, 400, MUTED)]
        sx = X + 220
        if state == "uploading":
            row += [text("State", sx, ry + 23, "Uploading", 13, 600, ACCENT),
                    Rect("Track", sx, ry + 32, 240, 6, P["Surface/track"], 1, 3),
                    Rect("Progress", sx, ry + 32, 144, 6, ACCENT, 1, 3)]
        elif state == "waiting":
            row += [Circle("Dot", sx + 4, ry + 27, 4, P["Accent/amber"]),
                    text("State", sx + 16, ry + 32, "Waiting for WoW to finish writing", 13, 400, SECONDARY)]
        else:
            row.append(text("State", sx, ry + 32, f"Uploaded {LAST_SUCCESS.lower()}", 13, 400, MUTED))
        items.append(Group(f"Row {name}", row))
    return Group("Recent activity", items)


def syncing() -> list[Item]:
    top = TITLE_BAR_H + 96
    return [*heading(SYNC_TITLE, ["Snapshots upload when WoW finishes writing them."], ("Syncing", "success")),
            stats(top, 2), activity(top + 100),
            button("Pause button", X, top + 400, "Pause sync", "secondary", 140, Click("navigate", PAUSED)),
            button("Folders button", X + 148, top + 400, "Watched folders", "secondary", 160, Click("navigate", FOLDERS)),
            footer()]


def paused() -> list[Item]:
    top = TITLE_BAR_H + 96
    return [*heading(SYNC_TITLE, ["Nothing uploads until you resume."], ("Paused", "neutral")),
            notice("Paused notice", X, top, INNER_W, "Sync is paused",
                   "New snapshots wait here and upload when you resume.", "info"),
            stats(top + 88, 3),
            button("Resume button", X, top + 196, "Resume sync", "primary", 160, Click("navigate", SYNCING)),
            footer()]


def offline() -> list[Item]:
    top = TITLE_BAR_H + 96
    return [*heading(SYNC_TITLE, ["Uploads wait in the queue; nothing is lost."], ("Offline", "danger")),
            notice("Offline notice", X, top, INNER_W, "Can't reach RaidManager",
                   "You're offline, or the service is down. It retries on its own.", "danger"),
            stats(top + 88, 3),
            button("Retry button", X, top + 196, "Retry now", "primary", 140, Click("navigate", SYNCING)),
            footer()]


def incomplete() -> list[Item]:
    top = TITLE_BAR_H + 96
    steps = ["To fix it, log in with Jainaice in WoW and type /reload, or",
             "log out. Then retry. Your other characters keep syncing."]
    return [*heading(SYNC_TITLE, ["One snapshot needs your help."], ("Needs attention", "warning")),
            notice("Incomplete notice", X, top, INNER_W, "Jainaice's snapshot is incomplete",
                   "WoW didn't finish writing it, perhaps after a crash.", "warning"),
            *[text(f"Fix line {index + 1}", X, top + 108 + index * 20, line, 14, 400, SECONDARY)
              for index, line in enumerate(steps)],
            button("Retry button", X, top + 152, "Retry Jainaice", "primary", 160, Click("navigate", SYNCING)),
            footer()]


def boards() -> list[Board]:
    step = WIDTH + BOARD_GAP

    def window(name: str, index: int, content: list[Item]) -> Board:
        return companion_window(name, index * step, 0, content, width=WIDTH, height=HEIGHT)

    return [window(FOLDERS, 0, folders()), window(SYNCING, 1, syncing()), window(PAUSED, 2, paused()),
            window(OFFLINE, 3, offline()), window(INCOMPLETE, 4, incomplete())]


def main(repository: Path = REPOSITORY) -> Path:
    return write_mockup(repository, "companion-sync", "Companion sync", boards(),
                        flows={"Choose folders": FOLDERS, "Pause and resume": SYNCING, "Offline": OFFLINE,
                               "Incomplete snapshot": INCOMPLETE})


if __name__ == "__main__":
    print("Wrote", main())
