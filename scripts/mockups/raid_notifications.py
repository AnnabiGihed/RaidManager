"""Raid reminders and change notifications (story #24, design story #187, task #229).

Following the owner's decision on #24, reminders and changes reach players as Discord direct messages, one channel,
so nothing is sent twice; the website holds the preferences and shows when a message couldn't be delivered. Four
boards, linked as a clickable prototype:

1. Direct messages: the reminder before signups close, and the one before the raid starts.
2. Direct messages: material changes, a new start time, a signup changed on the website, a roster place given and
   a move to the bench.
3. Website: the notification preferences.
4. Website: a direct message couldn't be delivered, with how to fix it and Retry; nothing about the signup is lost.
   Retry and Notification settings open 3.

Run from the repository root: python scripts/mockups/raid_notifications.py
Once the owner edits the design in Penpot, the downloaded file replaces docs/mockups/raid-notifications.penpot and
this script is no longer run.
"""

from __future__ import annotations

import sys
from pathlib import Path

REPOSITORY = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(REPOSITORY / "scripts"))

from penpot_components import (  # noqa: E402
    BOARD_GAP, BOARD_H, CONTENT_TOP, CONTENT_X, DISCORD_MUTED, DISCORD_PALETTE, DISCORD_TEXT, DISCORD_W,
    DISCORD_WHITE, PLAYER, app_screen, badge, button, card, checkbox, discord_button, discord_embed, discord_message,
    discord_screen, form_field, notice, page_header,
)
from penpot_scene import HOUSE_PALETTE, Board, Click, Group, Item, Rect, bounds, text, write_mockup  # noqa: E402

P = HOUSE_PALETTE
PALETTE = {**HOUSE_PALETTE, **DISCORD_PALETTE}
ACCENT, MUTED, DIVIDER = P["Brand/accent"], P["Text/muted"], P["Line/divider"]
AMBER, DANGER = P["Accent/amber"], DISCORD_PALETTE["Discord/danger button"]
RAID = "Icecrown Citadel"
STARTS = "Friday 9 October 2026 20:00"
OPEN_RAID = "Open the raid ↗"
SECTION = "Notifications"

REMINDERS = "1 · Discord: reminders"
CHANGES = "2 · Discord: changes"
SETTINGS = "3 · Website: notification settings"
FAILED = "4 · Website: message not delivered"

X, EMBED_W = 72, 600


def dm_message(name: str, y: float, when: str, title: str, lines: list[str], accent: str,
               buttons: list[tuple[str, str]]) -> Group:
    height = 44 + 22 * len(lines)
    embed_y = y + 32
    content: list[Item] = [
        *discord_embed(X, embed_y, EMBED_W, height, accent),
        text("Embed title", X + 16, embed_y + 30, title, 16, 700, DISCORD_WHITE),
        *[text(f"Embed line {index + 1}", X + 16, embed_y + 56 + index * 22, line, 14, 400, DISCORD_TEXT)
          for index, line in enumerate(lines)],
    ]
    bx: float = X
    for index, (label, style) in enumerate(buttons):
        button_layer = discord_button(f"Button {index + 1}", bx, embed_y + height + 12, label, style)
        content.append(button_layer)
        bx += bounds(button_layer)[2] + 8
    return discord_message(name, y, when, content)


def reminders() -> list[Item]:
    return [
        dm_message("Signup reminder", 72, "Wed 7 Oct at 18:00", "Signups close in 24 hours",
                   [f"{RAID} · {STARTS}.", "You haven't replied yet."], AMBER,
                   [("Sign up", "success"), ("Decline", "secondary"), (OPEN_RAID, "secondary")]),
        dm_message("Start reminder", 300, "Fri 9 Oct at 19:00", f"{RAID} starts in 1 hour",
                   ["You're on the roster with Arthasdk · Frost DPS.", "Your boss assignments are on the raid page."],
                   ACCENT, [(OPEN_RAID, "secondary")]),
        text("Once note", X, 520, "Each reminder is sent once, here, and only while you can still act on it.", 12, 400,
             DISCORD_MUTED),
    ]


def changes() -> list[Item]:
    return [
        dm_message("Time change", 72, "Thu 8 Oct at 21:15", f"{RAID} moved to 21:00",
                   ["Friday 9 October 2026 21:00, one hour later.", "Your signup is kept; readiness is checked again."],
                   AMBER, [("Change my signup", "secondary")]),
        dm_message("Signup change", 272, "Thu 8 Oct at 21:30", "Your signup changed on the website",
                   ["You're now Tentative for Icecrown Citadel."], ACCENT, []),
        dm_message("Roster place", 412, "Fri 9 Oct at 12:00", "You're on the roster",
                   ["Arthasdk · Frost DPS for Icecrown Citadel on Friday 9 October."], ACCENT,
                   [(OPEN_RAID, "secondary")]),
        dm_message("Bench move", 604, "Fri 9 Oct at 17:40", "You were moved to the bench",
                   ["An officer moved Arthasdk to the bench for Icecrown Citadel.", "You may still be called in."],
                   DANGER, [(OPEN_RAID, "secondary")]),
    ]


def preference(name: str, x: float, y: float, title: str, detail: str, on: bool) -> Group:
    return Group(name, [*checkbox(x, y + 3, on), text("Title", x + 30, y + 17, title, 14, 600),
                        text("Detail", x + 30, y + 37, detail, 12, 400, MUTED)])


def settings() -> list[Item]:
    top = CONTENT_TOP + 120
    left, right = CONTENT_X, CONTENT_X + 560
    reminder_card: list[Item] = [
        *card(left, top, 540, 236), text("Title", left + 20, top + 34, "Reminders", 16, 600),
        preference("Signup reminder", left + 20, top + 56, "Before signups close", "Only while you haven't replied", True),
        form_field("Signup lead field", left + 300, top + 62, 220, "When", "24 hours before", True),
        preference("Start reminder", left + 20, top + 142, "Before the raid starts", "Only when you're on the roster", True),
        form_field("Start lead field", left + 300, top + 148, 220, "When", "1 hour before", True),
    ]
    change_card: list[Item] = [
        *card(right, top, 560, 236), text("Title", right + 20, top + 34, "Changes", 16, 600),
        preference("Schedule change", right + 20, top + 56, "Raid time or targets change", "For raids you signed up for", True),
        preference("Roster change", right + 20, top + 112, "My roster place changes", "Selected, swapped or benched", True),
        preference("Signup change", right + 20, top + 168, "My signup changes elsewhere",
                   "For example on the website after replying in Discord", False),
    ]
    delivery_top = top + 252
    delivery: list[Item] = [
        *card(left, delivery_top, 1120, 104), text("Title", left + 20, delivery_top + 34, "Delivery", 16, 600),
        text("Channel", left + 20, delivery_top + 62, f"Discord direct message to {PLAYER.name}", 14),
        text("Last", left + 20, delivery_top + 84, "Last message delivered Fri 9 Oct, 19:00", 12, 400, MUTED),
        badge("Status badge", left + 1120 - 20 - 92, delivery_top + 26, "Delivering", "success"),
    ]
    return [
        page_header("Settings", SECTION, "Reminders and changes reach you once, by Discord direct message."),
        Group("Reminders card", reminder_card), Group("Changes card", change_card), Group("Delivery card", delivery),
        button("Save button", CONTENT_X, delivery_top + 128, "Save", "primary", 104),
    ]


def failed() -> list[Item]:
    top = CONTENT_TOP + 120
    missed: list[Item] = [*card(CONTENT_X, top + 96, 720, 172),
                          text("Title", CONTENT_X + 20, top + 130, "While we couldn't reach you", 16, 600)]
    for index, (when, what) in enumerate((("Thu 8 Oct, 21:15", f"{RAID} moved to Friday 9 October, 21:00"),
                                          ("Fri 9 Oct, 12:00", "You're on the roster with Arthasdk · Frost DPS"))):
        y = top + 150 + index * 52
        missed.append(Group(f"Missed {index + 1}", [Rect("Divider", CONTENT_X, y, 720, 1, DIVIDER),
                                                    text("What", CONTENT_X + 20, y + 24, what, 14),
                                                    text("When", CONTENT_X + 20, y + 42, when, 12, 400, MUTED)]))
    return [
        page_header("Raids", "Your raids", "Upcoming raids you signed up for."),
        notice("Delivery notice", CONTENT_X, top, 1120, "We couldn't send you a Discord message",
               "Allow direct messages from Citadel Vanguard members in Discord's privacy settings, then retry.",
               "danger"),
        Group("Missed messages", missed),
        text("Kept note", CONTENT_X, top + 300, "Your signup and roster place are saved; only the messages were missed.",
             13, 400, MUTED),
        button("Retry button", CONTENT_X, top + 324, "Retry", "primary", 104, Click("navigate", SETTINGS)),
        button("Settings button", CONTENT_X + 112, top + 324, "Notification settings", "secondary", 200,
               Click("navigate", SETTINGS)),
    ]


def boards() -> list[Board]:
    column = DISCORD_W + BOARD_GAP
    row = BOARD_H + BOARD_GAP

    def dm(name: str, index: int, content: list[Item]) -> Board:
        return discord_screen(name, index * column, 0, content, height=BOARD_H, channel="RaidManager", icon="@")

    return [dm(REMINDERS, 0, reminders()), dm(CHANGES, 1, changes()),
            app_screen(SETTINGS, 0, row, SECTION, settings(), user=PLAYER),
            app_screen(FAILED, 1440 + BOARD_GAP, row, "Raids", failed(), user=PLAYER)]


def main(repository: Path = REPOSITORY) -> Path:
    return write_mockup(repository, "raid-notifications", "Raid notifications", boards(), PALETTE,
                        {"Reminders and changes": REMINDERS, "Message not delivered": FAILED})


if __name__ == "__main__":
    print("Wrote", main())
