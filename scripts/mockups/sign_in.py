"""Discord sign-in and the session (story #13, design story #177, task #211).

Four boards in RaidManager's design system (ADR-0019), linked as a clickable prototype:

1. Signed out: a centred page without the app shell (owner decision, 2026-09-30). Sign in with Discord leads to 2.
2. Signed in: the app shell with the player's Discord name and avatar; Sign out in the top bar leads back to 1.
3. Sign-in cancelled: the player chose Cancel on Discord's consent page. Try again leads to 2, Back to home to 1.
4. Sign-in did not complete: any other failure, including an expired or revoked authorization. Same actions.

The wording follows the website's current pages (`Home.razor`, `SignInFailedViewModel`). The page shows only what
Discord sign-in provides: the player's name and avatar.

Run from the repository root: python scripts/mockups/sign_in.py
Once the owner edits the design in Penpot, the downloaded file replaces docs/mockups/sign-in.penpot and this script
is no longer run.
"""

from __future__ import annotations

import sys
from pathlib import Path

REPOSITORY = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(REPOSITORY / "scripts"))

from penpot_components import (  # noqa: E402
    BOARD_GAP, BOARD_H, BOARD_W, CONTENT_TOP, CONTENT_X, PLAYER, PUBLIC_W, PUBLIC_X, app_screen, avatar, button,
    card, page_header, public_screen,
)
from penpot_scene import HOUSE_PALETTE, Board, Click, Group, Item, text, write_mockup  # noqa: E402

P = HOUSE_PALETTE
SECONDARY, MUTED = P["Text/secondary"], P["Text/muted"]
SIGNED_OUT = "1 · Signed out"
SIGNED_IN = "2 · Signed in"
CANCELLED = "3 · Sign-in cancelled"
FAILED = "4 · Sign-in did not complete"
CARD_TOP, PADDING = 312, 24


def lines(x: float, first_baseline: float, rows: list[str], name: str = "Message") -> list[Item]:
    """Body text broken into lines, 20 px apart."""
    return [text(f"{name} line {index + 1}", x, first_baseline + index * 20, row, 14, 400, SECONDARY)
            for index, row in enumerate(rows)]


def signed_out() -> list[Item]:
    height = 200
    return [
        text("Title", 0, 236, "Plan raids with your characters", 32, 700, P["Text/primary"], BOARD_W, "center"),
        text("Subtitle", 0, 268, "Raid signups, rosters and readiness for your Discord community.", 15,
             400, SECONDARY, BOARD_W, "center"),
        Group("Sign-in card", [
            *card(PUBLIC_X, CARD_TOP, PUBLIC_W, height),
            text("Heading", PUBLIC_X + PADDING, CARD_TOP + 44, "Sign in to continue", 18, 700),
            *lines(PUBLIC_X + PADDING, CARD_TOP + 76, ["RaidManager uses your Discord account. It gets your Discord",
                                                       "name and avatar, never your password."]),
            button("Sign in with Discord button", PUBLIC_X + PADDING, CARD_TOP + height - 64, "Sign in with Discord",
                   "primary", PUBLIC_W - 2 * PADDING, Click("navigate", SIGNED_IN)),
        ]),
        text("First sign-in note", PUBLIC_X, CARD_TOP + height + 36,
             "Your first sign-in creates your RaidManager account.", 12, 400, MUTED, PUBLIC_W, "center"),
    ]


def signed_in() -> list[Item]:
    top = CONTENT_TOP + 120
    return [
        page_header("Signed in", "Welcome, Bryn Valewood", "Your characters and raids will appear here."),
        Group("Account card", [
            *card(CONTENT_X, top, 480, 88),
            avatar("Avatar", CONTENT_X + 48, top + 44, PLAYER.initials, PLAYER.tone, 20),
            text("Name", CONTENT_X + 84, top + 40, PLAYER.name, 16, 600),
            text("Source", CONTENT_X + 84, top + 60, "Signed in with Discord", 13, 400, SECONDARY),
        ]),
    ]


def failure(title: str, message: list[str], accent: str) -> list[Item]:
    height = 96 + 20 * len(message) + 64
    return [Group("Failure card", [
        *card(PUBLIC_X, CARD_TOP, PUBLIC_W, height, accent),
        text("Title", PUBLIC_X + PADDING + 8, CARD_TOP + 44, title, 18, 700),
        *lines(PUBLIC_X + PADDING + 8, CARD_TOP + 76, message),
        button("Try again button", PUBLIC_X + PADDING + 8, CARD_TOP + height - 64, "Try again", "primary", 120,
               Click("navigate", SIGNED_IN)),
        button("Back to home button", PUBLIC_X + PADDING + 8 + 128, CARD_TOP + height - 64, "Back to home",
               "secondary", 136, Click("navigate", SIGNED_OUT)),
    ])]


def boards() -> list[Board]:
    right, below = BOARD_W + BOARD_GAP, BOARD_H + BOARD_GAP
    return [
        public_screen(SIGNED_OUT, 0, 0, signed_out()),
        app_screen(SIGNED_IN, right, 0, "Overview", signed_in(), user=PLAYER, sign_out=SIGNED_OUT),
        public_screen(CANCELLED, 0, below, failure("Sign-in cancelled", [
            "Discord did not share your account with RaidManager, so",
            "you are not signed in. Sign in again and choose Authorize",
            "to continue."], P["Accent/amber"])),
        public_screen(FAILED, right, below, failure("Sign-in did not complete", [
            "Something went wrong while signing you in with Discord,",
            "and you are not signed in. This is usually temporary;",
            "please try again."], P["Status/danger"])),
    ]


def main(repository: Path = REPOSITORY) -> Path:
    return write_mockup(repository, "sign-in", "Sign-in", boards(),
                        flows={"Sign in with Discord": SIGNED_OUT, "Sign-in cancelled": CANCELLED,
                               "Sign-in did not complete": FAILED})


if __name__ == "__main__":
    print("Wrote", main())
