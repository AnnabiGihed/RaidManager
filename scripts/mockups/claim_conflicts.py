"""Character ownership conflicts (story #171, design story #197, task #249).

Five boards in RaidManager's design system (ADR-0019), linked as a clickable prototype. The conflict continues the
character review mockup, where Bryn Valewood's claim on Sylvanash went to officer review:

1. An officer's list of open conflicts: the character, the current owner, the claiming player and when each
   claimed it, with recently resolved decisions kept for audit. Resolve on Sylvanash opens 2.
2. Resolving: keep the owner or transfer, with a required reason; the other claim is rejected. Record decision
   opens 3.
3. The outcome Bryn Valewood sees on the character review page: the claim was rejected, with the reason.
4. The outcome Tomas Hale sees: Sylvanash is still theirs.
5. The access error a player who isn't an officer gets.

Run from the repository root: python scripts/mockups/claim_conflicts.py
Once the owner edits the design in Penpot, the downloaded file replaces docs/mockups/claim-conflicts.penpot and this
script is no longer run.
"""

from __future__ import annotations

import sys
from pathlib import Path

REPOSITORY = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(REPOSITORY / "scripts"))

from penpot_components import (  # noqa: E402
    BOARD_GAP, BOARD_H, BOARD_W, CONTENT_TOP, CONTENT_W, CONTENT_X, PLAYER, User, app_screen, badge, button, card,
    form_field, notice, page_header,
)
from penpot_scene import HOUSE_PALETTE, Board, Circle, Click, Group, Item, Rect, text, write_mockup  # noqa: E402
from roster_data import CLASS_COLOURS, PALETTE_ADDITIONS  # noqa: E402

P = HOUSE_PALETTE
PALETTE = {**HOUSE_PALETTE, **PALETTE_ADDITIONS}
ACCENT, SECONDARY, MUTED, DIVIDER = P["Brand/accent"], P["Text/secondary"], P["Text/muted"], P["Line/divider"]
TEXT, RAISED, SELECTED = P["Text/primary"], P["Surface/raised"], P["Surface/selected"]
PAGE = "Conflicts"
SECTION = "Ownership conflicts"
MY_CHARACTERS = "My characters"
CHARACTER, OWNER, CLAIMANT = "Sylvanash", "Tomas Hale", "Bryn Valewood"
REASON = "Tomas has played it since August; Bryn agreed in Discord."
DECIDED = "Decided by Gihed Annabi on Thu 1 Oct at 19:20"
OWNER_USER = User(OWNER, "Player", "TH", "red")

CONFLICTS = "1 · Open conflicts"
RESOLVE = "2 · Resolve a conflict"
CLAIMANT_VIEW = "3 · What the claiming player sees"
OWNER_VIEW = "4 · What the owner sees"
DENIED = "5 · Not an officer"

TOP = CONTENT_TOP + 120
# character, realm, class, level, owner, owned since, claimant, claimed at
OPEN = [
    (CHARACTER, "Icecrown", "Hunter", 80, OWNER, "Wed 2 Sep", CLAIMANT, "Tue 29 Sep, 21:40"),
    ("Grimtusk", "Lordaeron", "Shaman", 80, "Kiri Dawn", "Wed 12 Aug", "Doran Pike", "Wed 30 Sep, 10:12"),
]


def label(name: str, x: float, y: float, value: str) -> Item:
    return text(name, x, y, value, 11, 700, MUTED, None, "left", 1.2)


def conflicts() -> list[Item]:
    head_h, row_h = 44, 72
    columns = {"character": 24, "owner": 330, "claimant": 600, "action": 980}
    table: list[Item] = [*card(CONTENT_X, TOP, CONTENT_W, head_h + row_h * len(OPEN) + 8)]
    table.append(Group("Table header", [
        label(f"{heading.title()} heading", CONTENT_X + columns[key], TOP + 27, heading)
        for key, heading in (("character", "CHARACTER"), ("owner", "CURRENT OWNER"), ("claimant", "CLAIMED BY"))]))
    for index, (character, realm, wow_class, level, owner, since, claimant, claimed) in enumerate(OPEN):
        y = TOP + head_h + index * row_h
        table.append(Group(f"Row {character}", [
            Rect("Divider", CONTENT_X, y, CONTENT_W, 1, DIVIDER),
            Circle("Class colour", CONTENT_X + 29, y + 30, 5, CLASS_COLOURS[wow_class]),
            text("Character", CONTENT_X + 42, y + 33, character, 14, 600),
            text("Detail", CONTENT_X + 42, y + 52, f"{realm} · {wow_class} · level {level}", 12, 400, MUTED),
            text("Owner", CONTENT_X + columns["owner"], y + 33, owner, 14),
            text("Since", CONTENT_X + columns["owner"], y + 52, f"Owner since {since}", 12, 400, MUTED),
            text("Claimant", CONTENT_X + columns["claimant"], y + 33, claimant, 14),
            text("Claimed", CONTENT_X + columns["claimant"], y + 52, f"Claimed {claimed}", 12, 400, MUTED),
            button("Resolve button", CONTENT_X + columns["action"], y + 16, "Resolve", "primary", 104,
                   Click("navigate", RESOLVE) if character == CHARACTER else None),
        ]))
    resolved_y = TOP + head_h + row_h * len(OPEN) + 32
    resolved: list[Item] = [
        *card(CONTENT_X, resolved_y, CONTENT_W, 100),
        text("Title", CONTENT_X + 20, resolved_y + 34, "Recently resolved", 16, 600),
        text("Decision", CONTENT_X + 20, resolved_y + 62, "Moonhollow (Icecrown) transferred to Arvel Moss", 14),
        text("Audit", CONTENT_X + 20, resolved_y + 82,
             "Decided by Tomas Hale on Mon 28 Sep at 22:05 · reason recorded · the other claim was rejected", 12,
             400, MUTED),
    ]
    return [page_header("Community", SECTION, "Characters claimed by two players of Citadel Vanguard"),
            Group("Open conflicts", table), Group("Recently resolved", resolved)]


def side(name: str, x: float, y: float, width: float, role: str, player: str, facts: list[str]) -> Group:
    items: list[Item] = [*card(x, y, width, 52 + 22 * len(facts) + 20),
                         label("Role", x + 20, y + 30, role), text("Player", x + 20, y + 54, player, 16, 600)]
    items += [text(f"Fact {index + 1}", x + 20, y + 80 + index * 22, fact, 13, 400, SECONDARY)
              for index, fact in enumerate(facts)]
    return Group(name, items)


def option(name: str, x: float, y: float, title: str, selected: bool) -> Group:
    items: list[Item] = [Rect("Area", x, y, 540, 44, SELECTED if selected else P["Surface/card"], 1, 8,
                              ACCENT if selected else DIVIDER)]
    if selected:
        items += [Circle("Radio", x + 24, y + 22, 8, ACCENT), Circle("Radio dot", x + 24, y + 22, 3, P["Brand/on accent"])]
    else:
        items += [Circle("Radio", x + 24, y + 22, 8, DIVIDER), Circle("Radio hole", x + 24, y + 22, 6, P["Surface/card"])]
    items.append(text("Title", x + 44, y + 27, title, 14, 600, ACCENT if selected else TEXT))
    return Group(name, items)


def resolve() -> list[Item]:
    half = (CONTENT_W - 20) / 2
    decision_y = TOP + 168
    return [
        page_header("Community", f"Resolve {CHARACTER}", "Icecrown · Hunter · level 80"),
        side("Owner side", CONTENT_X, TOP, half, "CURRENT OWNER", OWNER,
             ["Claimed Wed 2 Sep, approved the same day", "Last uploaded by their companion on Wed 30 Sep",
              "On 4 rosters since August"]),
        side("Claimant side", CONTENT_X + half + 20, TOP, half, "CLAIMED BY", CLAIMANT,
             ["Found by their companion on Tue 29 Sep, 21:40", "Approved the claim, so it went to officer review"]),
        Group("Decision", [
            *card(CONTENT_X, decision_y, CONTENT_W, 288),
            text("Title", CONTENT_X + 20, decision_y + 34, "Your decision", 16, 600),
            option("Keep option", CONTENT_X + 20, decision_y + 52, f"Keep {OWNER} as the owner", True),
            option("Transfer option", CONTENT_X + 20, decision_y + 104, f"Transfer to {CLAIMANT}", False),
            form_field("Reason field", CONTENT_X + 580, decision_y + 66, CONTENT_W - 600, "Reason (required)", REASON),
            text("Effect", CONTENT_X + 580, decision_y + 142, "The other claim is rejected. Both players see the decision,",
                 12, 400, MUTED),
            text("Effect 2", CONTENT_X + 580, decision_y + 160, "and it's kept with your name and the time.", 12, 400,
                 MUTED),
            button("Record button", CONTENT_X + 20, decision_y + 228, "Record decision", "primary", 168,
                   Click("navigate", CLAIMANT_VIEW)),
            button("Cancel button", CONTENT_X + 196, decision_y + 228, "Cancel", "secondary", 96,
                   Click("navigate", CONFLICTS)),
        ]),
    ]


def outcome(title: str, body: str, tone: str, remaining: list[str]) -> list[Item]:
    items: list[Item] = [
        page_header(MY_CHARACTERS, "Your characters", "Decisions about your claims appear here."),
        notice("Outcome notice", CONTENT_X, TOP, CONTENT_W, title, body, tone),
        Group("Decision detail", [
            *card(CONTENT_X, TOP + 96, CONTENT_W, 116),
            label("Reason label", CONTENT_X + 20, TOP + 126, "REASON"),
            text("Reason", CONTENT_X + 20, TOP + 150, REASON, 14),
            text("Decided", CONTENT_X + 20, TOP + 176, DECIDED, 12, 400, MUTED),
        ]),
    ]
    items += [badge(f"Remaining {index + 1}", CONTENT_X + index * 180, TOP + 236, value, "success")
              for index, value in enumerate(remaining)]
    return items


def denied() -> list[Item]:
    return [
        page_header("Community", SECTION, "Citadel Vanguard"),
        notice("Denied notice", CONTENT_X, TOP, CONTENT_W, "Only officers can see ownership conflicts",
               "Ask an officer of Citadel Vanguard if a character you play is claimed by someone else.", "danger"),
        button("Back button", CONTENT_X, TOP + 96, "Back to my characters", "secondary", 196),
    ]


def boards() -> list[Board]:
    right, below = BOARD_W + BOARD_GAP, BOARD_H + BOARD_GAP
    third = 2 * BOARD_W + 2 * BOARD_GAP
    return [
        app_screen(CONFLICTS, 0, 0, PAGE, conflicts(), section=SECTION),
        app_screen(RESOLVE, right, 0, PAGE, resolve(), section=SECTION),
        app_screen(CLAIMANT_VIEW, 0, below, MY_CHARACTERS, outcome(
            f"{CHARACTER} stays with another player", "An officer reviewed the conflict and rejected your claim.",
            "info", ["Arthasdk", "Jainaice", "Thrallsham"]), section="Review new characters", user=PLAYER),
        app_screen(OWNER_VIEW, right, below, MY_CHARACTERS, outcome(
            f"{CHARACTER} is still yours", "An officer reviewed the other claim and kept you as the owner.", "success",
            [CHARACTER, "Lothar", "Tomasdk"]), section="Review new characters", user=OWNER_USER),
        app_screen(DENIED, third, below, PAGE, denied(), section=SECTION, user=PLAYER),
    ]


def main(repository: Path = REPOSITORY) -> Path:
    return write_mockup(repository, "claim-conflicts", "Claim conflicts", boards(), PALETTE,
                        {"Resolve a conflict": CONFLICTS, "Not an officer": DENIED})


if __name__ == "__main__":
    print("Wrote", main())
