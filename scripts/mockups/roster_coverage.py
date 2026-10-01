"""Coverage and composition warnings (story #31, design story #191, task #237).

Three boards in RaidManager's design system (ADR-0019), seen by an officer, linked as a clickable prototype. The
drafts come from `roster_data.py`, shared with the other roster mockups:

1. Draft A's raid buffs and debuffs, after Shadestep went to the bench: covered with the characters providing them,
   covered only if a placeholder is filled, and missing. The Draft B warnings link opens 2.
2. Draft B's warnings (Draft B was copied from an old raid): a duplicate player, an overlapping raid, a participant
   who declined, unknown data, and roles still held by placeholders. Inspect on the overlapping raid opens 3.
3. One warning inspected: the overlapping raid, with the officer's choices. Warnings never change or publish a
   roster on their own. Back opens 2.

Coverage follows WotLK 3.3.5a raid buffs and debuffs; talent-dependent effects use the synced talent configuration.

Run from the repository root: python scripts/mockups/roster_coverage.py
Once the owner edits the design in Penpot, the downloaded file replaces docs/mockups/roster-coverage.penpot and this
script is no longer run.
"""

from __future__ import annotations

import sys
from pathlib import Path

REPOSITORY = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(REPOSITORY / "scripts"))

from penpot_components import (  # noqa: E402
    BOARD_GAP, BOARD_H, BOARD_W, CONTENT_TOP, CONTENT_W, CONTENT_X, app_screen, badge, button, card, notice,
    page_header,
)
from penpot_scene import HOUSE_PALETTE, Board, Click, Group, Item, Rect, text, write_mockup  # noqa: E402
from roster_data import PALETTE_ADDITIONS, WHEN  # noqa: E402

P = HOUSE_PALETTE
PALETTE = {**HOUSE_PALETTE, **PALETTE_ADDITIONS}
ACCENT, SECONDARY, MUTED, DIVIDER = P["Brand/accent"], P["Text/secondary"], P["Text/muted"], P["Line/divider"]
PAGE = "Roster builder"
COVERED, PLACEHOLDER, MISSING = "Covered", "Placeholder", "Missing"
TONES = {COVERED: "success", PLACEHOLDER: "info", MISSING: "danger"}
OVERLAP = "Overlapping raid"

COVERAGE = "1 · Draft A coverage"
WARNINGS = "2 · Draft B warnings"
DETAIL = "3 · Inspect a warning"

TOP = CONTENT_TOP + 120
# effect, how it's provided, providers in the draft, status
BUFFS = [
    ("Stats +10%", "Blessing of Kings", "Lightwarden, Crusader", COVERED),
    ("Mark of the Wild", "Druid", "Moonveil, Starfall", COVERED),
    ("Stamina", "Power Word: Fortitude", "Voidcaller, Kirilight", COVERED),
    ("Intellect", "Arcane Intellect", "Frostweave, Pyrelight", COVERED),
    ("Attack power +10%", "Trueshot Aura, Unleashed Rage", "Swiftshot, Tidefist", COVERED),
    ("Melee haste +20%", "Windfury Totem, Improved Icy Talons", "Tidefist, Arthasdk", COVERED),
    ("Spell power", "Totem of Wrath, Flametongue Totem", "Sparkcaller", COVERED),
    ("Critical strike +5%", "Leader of the Pack, Rampage", "Brookpaw", COVERED),
    ("Replenishment", "Vampiric Touch, Judgements, Hunting Party", "Voidcaller, Crusader, Longstride", COVERED),
    ("Bloodlust or Heroism", "Shaman", "Stormcall, Tidefist, Sparkcaller", COVERED),
]
DEBUFFS = [
    ("Armor -20%", "Sunder Armor, Expose Armor", "Stonebrow, Bladewind", COVERED),
    ("Spell damage taken +13%", "Curse of the Elements, Ebon Plague", "Emberlock, Wintergrave", COVERED),
    ("Physical damage taken +4%", "Blood Frenzy, Savage Combat", "Fill the melee placeholder with an Arms warrior or Combat rogue",
     PLACEHOLDER),
    ("Spell critical taken +5%", "Improved Scorch, Winter's Chill",
     "Pyrelight's synced talents don't include Improved Scorch", MISSING),
    ("Spell hit taken +3%", "Misery, Improved Faerie Fire", "Voidcaller", COVERED),
    ("Attack speed slowed", "Thunder Clap, Frost Fever", "Stonebrow, Wintergrave", COVERED),
]


def label(name: str, x: float, y: float, value: str) -> Item:
    return text(name, x, y, value, 11, 700, MUTED, None, "left", 1.2)


def coverage_card(name: str, x: float, y: float, width: float, rows: list[tuple[str, str, str, str]]) -> Group:
    """Buffs or debuffs, one row each; a row that isn't covered explains why on a third line."""
    heights = [52 if status == COVERED else 72 for *_, status in rows]
    items: list[Item] = [*card(x, y, width, 52 + sum(heights) + 8), text("Title", x + 20, y + 34, name, 16, 600)]
    ry = y + 52
    for (effect, source, providers, status), height in zip(rows, heights):
        row: list[Item] = [
            Rect("Divider", x, ry, width, 1, DIVIDER),
            text("Effect", x + 20, ry + 22, effect, 14, 600),
            text("Source", x + 20, ry + 40, source, 12, 400, MUTED),
            badge("Status", x + width - 20 - 100, ry + 14, status, TONES[status]),
        ]
        if status == COVERED:
            row.append(text("Providers", x + 250, ry + 30, providers, 12, 400, SECONDARY))
        else:
            row += [text("Providers", x + 250, ry + 30, "None in the draft", 12, 400, MUTED),
                    text("Why", x + 20, ry + 60, providers, 12, 400, P["Text/primary"])]
        items.append(Group(f"{effect} row", row))
        ry += height
    return Group(name, items)


def coverage() -> list[Item]:
    half = (CONTENT_W - 16) / 2
    link = Group("Warnings link", [Rect("Area", CONTENT_X + CONTENT_W - 220, TOP - 2, 220, 28, P["Surface/page"]),
                                   text("Label", CONTENT_X + CONTENT_W - 220, TOP + 17, "Draft B has 5 warnings →", 13,
                                        600, ACCENT, 220, "right")], Click("navigate", WARNINGS))
    counts = [badge(f"{status} count", CONTENT_X + index * 140, TOP, f"{sum(1 for r in BUFFS + DEBUFFS if r[3] == status)} "
                    f"{status.lower()}", TONES[status]) for index, status in enumerate(TONES)]
    return [
        page_header(PAGE, "Draft A coverage", WHEN),
        Group("Coverage summary", [*counts, link]),
        coverage_card("Raid buffs", CONTENT_X, TOP + 40, half, BUFFS),
        coverage_card("Debuffs on the boss", CONTENT_X + half + 16, TOP + 40, half, DEBUFFS),
        text("Advisory note", CONTENT_X + half + 16, TOP + 40 + 52 + 52 * 4 + 72 * 2 + 32,
             "Coverage is advisory; it never changes the draft.", 12, 400, MUTED),
    ]


WARNING_ROWS = [
    ("Duplicate player", "Tomas Hale is in Draft B twice: Tomasdk in group 1 and Lothar in group 2.", "danger"),
    (OVERLAP, "Pyrelight (Faye Lorn) is on the published roster of Ulduar 25 alt run, same evening.", "danger"),
    ("Unavailable participant", "Mira Quell declined; Dawnsong is still in group 5 from the copied roster.", "warning"),
    ("Unknown data", "Frostweave (Ilsa Brand) needs a fresh sync, so its readiness is unknown.", "warning"),
    ("Missing roles", "Two places are still placeholders: one healer and one melee.", "info"),
]


def warnings() -> list[Item]:
    row_h = 72
    items: list[Item] = [*card(CONTENT_X, TOP, CONTENT_W, 52 + row_h * len(WARNING_ROWS) + 8),
                         text("Title", CONTENT_X + 20, TOP + 34, f"{len(WARNING_ROWS)} warnings", 16, 600)]
    for index, (kind, detail, tone) in enumerate(WARNING_ROWS):
        y = TOP + 52 + index * row_h
        row: list[Item] = [Rect("Divider", CONTENT_X, y, CONTENT_W, 1, DIVIDER),
                           badge("Kind", CONTENT_X + 20, y + 24, kind, tone),
                           text("Detail", CONTENT_X + 230, y + 41, detail, 14)]
        row.append(button("Inspect button", CONTENT_X + CONTENT_W - 20 - 104, y + 16, "Inspect", "secondary", 104,
                          Click("navigate", DETAIL) if kind == OVERLAP else None))
        items.append(Group(f"Warning {index + 1}", row))
    return [
        page_header(PAGE, "Draft B warnings", "Copied from the raid on Fri 25 Sep · " + WHEN),
        Group("Warnings", items),
        text("Advisory note", CONTENT_X, TOP + 52 + row_h * len(WARNING_ROWS) + 40,
             "Warnings don't block saving a draft; publishing shows any that are still open.", 13, 400, MUTED),
    ]


def detail() -> list[Item]:
    left_w = 720
    facts = [("THIS RAID", "Icecrown Citadel · Fri 9 Oct, 21:00 to 00:30"),
             ("OTHER RAID", "Ulduar 25 alt run · Fri 9 Oct, 20:00 to 23:30 · roster published"),
             ("PLAYER", "Faye Lorn, with Pyrelight in both"),
             ("OVERLAP", "21:00 to 23:30")]
    evidence: list[Item] = [*card(CONTENT_X, TOP + 96, left_w, 52 + 56 * len(facts) + 8),
                            text("Title", CONTENT_X + 20, TOP + 130, "What overlaps", 16, 600)]
    for index, (name, value) in enumerate(facts):
        y = TOP + 148 + index * 56
        evidence += [label(f"{name.title()} label", CONTENT_X + 20, y + 20, name),
                     text(f"{name.title()} value", CONTENT_X + 20, y + 42, value, 14)]
    decision_x = CONTENT_X + 740
    decision: list[Item] = [
        *card(decision_x, TOP + 96, 380, 276),
        text("Title", decision_x + 20, TOP + 130, "Your choice", 16, 600),
        button("Remove button", decision_x + 20, TOP + 152, "Remove Pyrelight from Draft B", "secondary", 340),
        button("Open button", decision_x + 20, TOP + 204, "Open Ulduar 25 alt run", "secondary", 340),
        button("Keep button", decision_x + 20, TOP + 256, "Keep and decide later", "secondary", 340),
        text("Note", decision_x + 20, TOP + 332, "Nothing changes until you choose.", 12, 400, MUTED),
    ]
    return [
        page_header(PAGE, OVERLAP, "Draft B · Pyrelight (Faye Lorn)"),
        notice("Overlap notice", CONTENT_X, TOP, CONTENT_W, "Pyrelight can't be in two raids at once",
               "Faye Lorn is on the published roster of another raid that overlaps this one by two and a half hours.",
               "danger"),
        Group("Evidence", evidence), Group("Decision", decision),
        button("Back button", CONTENT_X, TOP + 396, "Back to warnings", "secondary", 168, Click("navigate", WARNINGS)),
    ]


def boards() -> list[Board]:
    right, below = BOARD_W + BOARD_GAP, BOARD_H + BOARD_GAP

    def screen(name: str, x: float, y: float, content: list[Item], section: str) -> Board:
        return app_screen(name, x, y, PAGE, content, section=section)

    return [screen(COVERAGE, 0, 0, coverage(), "Coverage"), screen(WARNINGS, right, 0, warnings(), "Warnings"),
            screen(DETAIL, 0, below, detail(), "Warnings")]


def main(repository: Path = REPOSITORY) -> Path:
    return write_mockup(repository, "roster-coverage", "Roster coverage", boards(), PALETTE,
                        {"Review coverage and warnings": COVERAGE, "Inspect a warning": WARNINGS})


if __name__ == "__main__":
    print("Wrote", main())
