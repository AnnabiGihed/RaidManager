"""The Discord signup flow (story #23, design story #186, task #227).

Six boards of the bot's messages in Discord's dark theme, with RaidManager's accent (ADR-0019), linked as a
clickable prototype. Discord edits the same raid-and-user signup as the website, with the same rules:

1. The raid post: targets, start and deadline in the reader's time zone, signup totals, and its buttons.
   Sign up opens 2.
2. The private signup message: approved characters and specs (the select open), the preferred one, and one button
   per availability. Confirmed opens 4; Late opens 3.
3. The arrival time Discord asks for a late signup. Submit opens 4.
4. The confirmation: what was offered, and that pressing again updates the same signup.
5. Refused: signups are closed.
6. Refused: a chosen character became locked through the raid; the other choices were saved. Change characters
   opens 2.

Discord shows each time in the reader's own time zone. Characters locked through the raid aren't listed.

Run from the repository root: python scripts/mockups/discord_signup.py
Once the owner edits the design in Penpot, the downloaded file replaces docs/mockups/discord-signup.penpot and this
script is no longer run.
"""

from __future__ import annotations

import sys
from pathlib import Path

REPOSITORY = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(REPOSITORY / "scripts"))

from penpot_components import (  # noqa: E402
    BOARD_GAP, DISCORD_MUTED, DISCORD_PALETTE, DISCORD_TEXT, DISCORD_W, DISCORD_WHITE, discord_button, discord_embed,
    discord_message, discord_screen,
)
from penpot_scene import HOUSE_PALETTE, Board, Click, Group, Item, Rect, text, write_mockup  # noqa: E402

PALETTE = {**HOUSE_PALETTE, **DISCORD_PALETTE}
INPUT, DIVIDER = DISCORD_PALETTE["Discord/input"], DISCORD_PALETTE["Discord/divider"]
DANGER = DISCORD_PALETTE["Discord/danger button"]
ACCENT = HOUSE_PALETTE["Brand/accent"]
RAID = "Icecrown Citadel"
STARTS = "Friday 9 October 2026 20:00"
CLOSES = "Friday 9 October 2026 18:00"
NOW = "Today at 18:40"
WEBSITE = "Full form on the website ↗"
WEBSITE_BUTTON = "Website button"
FROST = "Arthasdk · Frost DPS"
BLOOD = "Arthasdk · Blood Tank"

POST = "1 · Raid post"
CHOOSE = "2 · Choose characters and availability"
LATE = "3 · Late: arrival time"
CONFIRMED = "4 · Signed up"
CLOSED = "5 · Refused: signups closed"
LOCKED = "6 · Refused: character locked"

X = 72


def field(name: str, x: float, y: float, title: str, value: str) -> list[Item]:
    return [text(f"{name} title", x, y, title, 12, 700, DISCORD_WHITE),
            text(f"{name} value", x, y + 20, value, 14, 400, DISCORD_TEXT)]


def post() -> list[Item]:
    y = 72
    embed_y, w = y + 56, 600
    content: list[Item] = [
        text("Mention", X, y + 40, "@Raiders Signups are open for this week's Icecrown Citadel.", 14, 400, DISCORD_TEXT),
        *discord_embed(X, embed_y, w, 250),
        text("Embed title", X + 16, embed_y + 30, RAID, 16, 700, DISCORD_WHITE),
        text("Embed description", X + 16, embed_y + 54, "Weekly progression, then Halion if time allows.", 14, 400,
             DISCORD_TEXT),
        *field("Targets", X + 16, embed_y + 90, "Targets", "Icecrown Citadel 25 heroic · Ruby Sanctum 25"),
        *field("Starts", X + 16, embed_y + 138, "Starts", f"{STARTS} (in 2 days)"),
        *field("Closes", X + 316, embed_y + 138, "Signups close", CLOSES),
        *field("Signups", X + 16, embed_y + 186, "Signups", "18 confirmed · 4 tentative · 2 late · 3 declined"),
        text("Embed footer", X + 16, embed_y + 238, "Times show in your time zone · Citadel Vanguard", 12, 400,
             DISCORD_MUTED),
        discord_button("Sign up button", X, embed_y + 262, "Sign up", "success", None, Click("navigate", CHOOSE)),
        discord_button("Decline button", X + 104, embed_y + 262, "Decline"),
        discord_button("Withdraw button", X + 200, embed_y + 262, "Withdraw"),
        discord_button(WEBSITE_BUTTON, X + 306, embed_y + 262, "Open on the website ↗"),
    ]
    return [discord_message("Raid post", y, "Today at 12:00", content)]


def select_option(index: int, y: float, value: str, detail: str, chosen: bool) -> Group:
    items: list[Item] = [Rect("Row", X, y, 520, 52, INPUT), text("Label", X + 16, y + 22, value, 14, 600, DISCORD_WHITE),
                         text("Description", X + 16, y + 41, detail, 12, 400, DISCORD_MUTED)]
    if chosen:
        items += [Rect("Tick box", X + 488, y + 17, 18, 18, DISCORD_PALETTE["Discord/primary button"], 1, 4),
                  text("Tick", X + 488, y + 31, "✓", 12, 800, DISCORD_WHITE, 18, "center", icon=True)]
    else:
        items.append(Rect("Tick box", X + 488, y + 17, 18, 18, INPUT, 1, 4, DISCORD_MUTED))
    return Group(f"Option {index + 1}", items)


def choose() -> list[Item]:
    y = 72
    options = [(FROST, "Available · GS 5,712", True), (BLOOD, "Available · GS 5,480", True),
               ("Jainaice · Fire DPS", "Needs fresh sync · GS 5,340", False),
               ("Thrallsham · Restoration", "Resets before raid · GS 4,120", False)]
    menu_y = y + 104
    content: list[Item] = [
        text("Intro", X, y + 40, f"Sign up for {RAID} · {STARTS}", 14, 600, DISCORD_WHITE),
        text("Step", X, y + 62, "Choose the characters and specs you'd play, then your availability.", 14, 400,
             DISCORD_TEXT),
        Group("Characters select", [
            Rect("Select", X, y + 76, 520, 40, INPUT, 1, 4, DIVIDER),
            text("Selected", X + 12, y + 101, f"{FROST}, {BLOOD}", 14, 400, DISCORD_TEXT),
            *[select_option(index, menu_y + 16 + index * 52, value, detail, chosen)
              for index, (value, detail, chosen) in enumerate(options)],
            text("Menu note", X + 16, menu_y + 16 + 4 * 52 + 22, "Lothar isn't listed: it's locked through the raid.",
                 12, 400, DISCORD_MUTED),
        ]),
        Group("Preferred select", [
            Rect("Select", X, menu_y + 272, 520, 40, INPUT, 1, 4, DIVIDER),
            text("Selected", X + 12, menu_y + 297, f"Preferred: {FROST}", 14, 400, DISCORD_TEXT),
        ]),
        discord_button("Confirmed button", X, menu_y + 328, "Confirmed", "success", None, Click("navigate", CONFIRMED)),
        discord_button("Tentative button", X + 120, menu_y + 328, "Tentative"),
        discord_button("Late button", X + 232, menu_y + 328, "Late…", "secondary", None,
                       Click("navigate", LATE)),
        discord_button("Declined button", X + 316, menu_y + 328, "Declined"),
        discord_button(WEBSITE_BUTTON, X + 424, menu_y + 328, WEBSITE),
    ]
    return [discord_message("Private signup", y, NOW, content, private=True)]


def late() -> list[Item]:
    w, h = 440, 260
    x, y = (DISCORD_W - w) / 2, 140
    return [
        *choose(),
        Rect("Dim overlay", 0, 0, DISCORD_W, 720, DISCORD_PALETTE["Discord/input"], 0.85),
        Group("Arrival modal", [
            Rect("Modal", x, y, w, h, DISCORD_PALETTE["Discord/chat"], 1, 8),
            text("Title", x + 16, y + 40, "Arriving late", 20, 700, DISCORD_WHITE),
            text("Field label", x + 16, y + 84, "ARRIVAL TIME, IN YOUR TIME ZONE", 12, 700, DISCORD_TEXT),
            Rect("Input", x + 16, y + 94, w - 32, 40, INPUT, 1, 4),
            text("Value", x + 28, y + 119, "20:30", 14, 400, DISCORD_TEXT),
            text("Help", x + 16, y + 156, "Officers see it in their own time zone.", 12, 400, DISCORD_MUTED),
            Rect("Footer", x, y + h - 64, w, 64, DISCORD_PALETTE["Discord/embed"], 1, 0),
            text("Cancel", x + w - 212, y + h - 27, "Cancel", 14, 600, DISCORD_WHITE, 80, "center"),
            discord_button("Submit button", x + w - 16 - 104, y + h - 48, "Submit", "primary", 104,
                           Click("navigate", CONFIRMED)),
        ]),
    ]


def reply(title: str, lines: list[str], accent: str, buttons: list[Item], height: float) -> list[Item]:
    y = 72
    embed_y = y + 32
    content: list[Item] = [
        *discord_embed(X, embed_y, 600, height, accent),
        text("Embed title", X + 16, embed_y + 30, title, 16, 700, DISCORD_WHITE),
        *[text(f"Embed line {index + 1}", X + 16, embed_y + 56 + index * 22, line, 14, 400, DISCORD_TEXT)
          for index, line in enumerate(lines)],
        *buttons,
    ]
    return [discord_message("Private reply", y, NOW, content, private=True)]


def confirmed() -> list[Item]:
    lines = [f"{FROST} (preferred): Available", f"{BLOOD}: Available", "",
             f"Edit or withdraw until {CLOSES}.",
             "Pressing a button again updates this signup; it never adds a second one."]
    buttons: list[Item] = [discord_button("Withdraw button", X, 104 + 184 + 12, "Withdraw", "danger"),
                           discord_button(WEBSITE_BUTTON, X + 104, 104 + 184 + 12, WEBSITE)]
    return reply(f"You're signed up for {RAID} · Confirmed", lines, ACCENT, buttons, 184)


def closed() -> list[Item]:
    lines = [f"Signups for {RAID} closed on {CLOSES}.", "Ask an officer if you still need a place."]
    return reply("Signups are closed", lines, DANGER,
                 [discord_button(WEBSITE_BUTTON, X, 104 + 118 + 12, "Open the raid ↗")], 118)


def locked() -> list[Item]:
    lines = ["Jainaice was saved to Icecrown Citadel 25 heroic this evening,",
             "so it's locked through the raid. Your other choices were saved."]
    return reply("Jainaice can't be offered", lines, DANGER,
                 [discord_button("Change button", X, 104 + 118 + 12, "Change characters", "secondary", None,
                                 Click("navigate", CHOOSE))], 118)



def boards() -> list[Board]:
    column, row = DISCORD_W + BOARD_GAP, 720 + BOARD_GAP

    def screen(name: str, index: int, content: list[Item]) -> Board:
        return discord_screen(name, (index % 3) * column, (index // 3) * row, content, height=720)

    return [screen(POST, 0, post()), screen(CHOOSE, 1, choose()), screen(LATE, 2, late()),
            screen(CONFIRMED, 3, confirmed()), screen(CLOSED, 4, closed()), screen(LOCKED, 5, locked())]


def main(repository: Path = REPOSITORY) -> Path:
    return write_mockup(repository, "discord-signup", "Discord signup", boards(), PALETTE,
                        {"Sign up in Discord": POST, "Late signup": CHOOSE, "Signups closed": CLOSED,
                         "Character locked": LOCKED})


if __name__ == "__main__":
    print("Wrote", main())
