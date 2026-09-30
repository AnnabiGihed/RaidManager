"""Character profiles (story #19, design story #182, task #219).

Four boards in RaidManager's design system (ADR-0019), seen by a player, linked as a clickable prototype:

1. My characters: realm, class, level, primary loadout, raid saves and sync freshness. The Arthasdk row opens 2.
2. Arthasdk's profile: data sources and last syncs, professions, note and visibility, loadouts, raid saves and the
   primary loadout's equipment. Edit profile opens 3.
3. Editing: visibility (Community or Officers only, owner decision on #19), note, loadout labels, a profession, and
   a raid save the player reports. Save shows 4; Cancel goes back to 2.
4. The profile after saving: player-reported data is marked, and the synced raid saves are unchanged.

The data comes from the domain (`Character`, `Loadout`, `GearItem`, `RaidLockout`, `CharacterDataSource`), plus the
professions, note and visibility the story adds.

Run from the repository root: python scripts/mockups/character_profile.py
Once the owner edits the design in Penpot, the downloaded file replaces docs/mockups/character-profile.penpot and
this script is no longer run.
"""

from __future__ import annotations

import sys
from pathlib import Path

REPOSITORY = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(REPOSITORY / "scripts"))

from penpot_components import (  # noqa: E402
    BOARD_GAP, BOARD_H, BOARD_W, CONTENT_TOP, CONTENT_W, CONTENT_X, PLAYER, app_screen, badge, button, card,
    form_field, page_header,
)
from penpot_scene import HOUSE_PALETTE, Board, Circle, Click, Group, Item, Rect, text, write_mockup  # noqa: E402

P = HOUSE_PALETTE
ACCENT, SECONDARY, MUTED, DIVIDER = P["Brand/accent"], P["Text/secondary"], P["Text/muted"], P["Line/divider"]
CLASS_COLOURS = {"Death Knight": "#C41E3A", "Mage": "#3FC7EB", "Shaman": "#0070DD"}
PALETTE = {**HOUSE_PALETTE, **{f"WoW class/{name}": colour for name, colour in CLASS_COLOURS.items()}}

PAGE = "My characters"
LAST_SYNC = "Today, 14:05"
FROM_ADDON = "From the addon"
PLAYER_REPORTED = "Player-reported"
RESETS = "Resets Wed 7 Oct, 04:00"
FROST, BLOOD, RAID_SAVES = "Frost DPS", "Blood Tank", "Raid saves"
CARD = P["Surface/card"]
TWENTY_FIVE = "25 players"

CHARACTERS = "1 · My characters"
PROFILE = "2 · Profile"
EDIT = "3 · Edit profile"
SAVED = "4 · Profile after saving"

TOP = CONTENT_TOP + 120
COLUMN_W, COLUMN_GAP = 360, 20
COLUMNS = [CONTENT_X + index * (COLUMN_W + COLUMN_GAP) for index in range(3)]


def label(name: str, x: float, y: float, value: str) -> Item:
    return text(name, x, y, value, 11, 700, MUTED, None, "left", 1.2)


def card_title(x: float, y: float, title: str, caption: str | None = None) -> list[Item]:
    items: list[Item] = [text("Title", x + 20, y + 34, title, 16, 600)]
    if caption:
        items.append(text("Caption", x + 20, y + 54, caption, 12, 400, MUTED))
    return items


# 1 · My characters
ROWS = [
    ("Arthasdk", "Death Knight", "Icecrown", 80, "Frost DPS · GearScore 5,712", "2 this week", LAST_SYNC, "success"),
    ("Jainaice", "Mage", "Lordaeron", 80, "Fire DPS · GearScore 5,340", "1 this week", "5 days ago", "warning"),
    ("Thrallsham", "Shaman", "Icecrown", 78, "Restoration · GearScore 4,120", "None", LAST_SYNC, "success"),
]


def characters() -> list[Item]:
    head_h, row_h = 44, 72
    columns = {"character": 24, "level": 260, "loadout": 340, "saves": 640, "sync": 820}
    table: list[Item] = card(CONTENT_X, TOP, CONTENT_W, head_h + row_h * len(ROWS) + 8)
    table.append(Group("Table header", [
        label(f"{heading.title()} heading", CONTENT_X + columns[key], TOP + 27, heading)
        for key, heading in (("character", "CHARACTER"), ("level", "LEVEL"), ("loadout", "PRIMARY LOADOUT"),
                             ("saves", "RAID SAVES"), ("sync", "LAST SYNC"))]))
    for index, (name, wow_class, realm, level, loadout, saves, synced, tone) in enumerate(ROWS):
        y = TOP + head_h + index * row_h
        table.append(Group(f"Row {name}", [
            Rect("Area", CONTENT_X, y, CONTENT_W, row_h, CARD),
            Rect("Divider", CONTENT_X, y, CONTENT_W, 1, DIVIDER),
            Circle("Class colour", CONTENT_X + 29, y + 36, 5, CLASS_COLOURS[wow_class]),
            text("Name", CONTENT_X + 42, y + 32, name, 14, 600),
            text("Class and realm", CONTENT_X + 42, y + 52, f"{wow_class} · {realm}", 12, 400, MUTED),
            text("Level", CONTENT_X + columns["level"], y + 41, str(level), 14),
            text("Loadout", CONTENT_X + columns["loadout"], y + 41, loadout, 13, 400, SECONDARY),
            text(RAID_SAVES, CONTENT_X + columns["saves"], y + 41, saves, 13, 400, SECONDARY),
            badge("Sync badge", CONTENT_X + columns["sync"], y + 24, synced, tone),
        ], Click("navigate", PROFILE) if name == "Arthasdk" else None))
    return [
        page_header("Characters", "My characters", "Your approved characters. Open one to see its profile."),
        Group("Characters table", table),
        text("Freshness note", CONTENT_X, TOP + head_h + row_h * len(ROWS) + 40,
             "Data older than 3 days may be out of date: log in with that character so the companion syncs it.",
             13, 400, MUTED),
    ]


# 2 and 4 · Profile
def sources(x: float, y: float) -> Group:
    rows = [("WoW addon", f"Last sync {LAST_SYNC.lower()}"), ("Warmane Armory", "Last sync yesterday, 09:12"),
            (RAID_SAVES, f"Complete scan {LAST_SYNC.lower()}")]
    items: list[Item] = [*card(x, y, COLUMN_W, 60 + 44 * len(rows) + 8), *card_title(x, y, "Data sources")]
    for index, (source, detail) in enumerate(rows):
        ry = y + 60 + index * 44
        items += [text(f"{source} name", x + 20, ry + 16, source, 13, 600),
                  text(f"{source} detail", x + 20, ry + 34, detail, 12, 400, SECONDARY)]
    return Group("Data sources", items)


def professions(x: float, y: float, edited: bool) -> Group:
    rows = [("Blacksmithing", "450", False), ("Mining", "450", False)]
    if edited:
        rows.append(("Jewelcrafting", "450", True))
    items: list[Item] = [*card(x, y, COLUMN_W, 60 + 40 * len(rows) + 8), *card_title(x, y, "Professions")]
    for index, (profession, skill, reported) in enumerate(rows):
        ry = y + 60 + index * 40
        items += [text(f"{profession} name", x + 20, ry + 22, f"{profession} {skill}", 13, 600)]
        if reported:
            items.append(badge(f"{profession} badge", x + COLUMN_W - 20 - 129, ry + 6, PLAYER_REPORTED, "warning"))
        else:
            items.append(text(f"{profession} source", x + COLUMN_W - 160, ry + 22, FROM_ADDON, 12, 400, MUTED, 140,
                              "right"))
    return Group("Professions", items)


def note(x: float, y: float, edited: bool) -> Group:
    value = "Main. Prefers Frost; can tank Blood." if edited else "Main. Prefers Frost."
    visibility = "Officers only" if edited else "Community"
    return Group("Note and visibility", [
        *card(x, y, COLUMN_W, 148), *card_title(x, y, "Note and visibility"),
        text("Note", x + 20, y + 76, value, 13),
        label("Visibility label", x + 20, y + 112, "VISIBLE TO"),
        text("Visibility", x + 20, y + 132, visibility, 13, 600),
    ])


LOADOUTS = [(FROST, True, "Melee damage", "5,712", "0/53/18"), (BLOOD, False, "Tank", "5,480", "51/10/10")]


def loadouts(x: float, y: float) -> Group:
    items: list[Item] = [*card(x, y, COLUMN_W, 60 + 88 * len(LOADOUTS) + 8), *card_title(x, y, "Loadouts")]
    for index, (name, primary, role, gear_score, talents) in enumerate(LOADOUTS):
        ly = y + 60 + index * 88
        block: list[Item] = [Rect("Divider", x, ly, COLUMN_W, 1, DIVIDER),
                             text("Name", x + 20, ly + 28, name, 14, 600),
                             text("Role", x + 20, ly + 50, f"{role} · GearScore {gear_score}", 13, 400, SECONDARY),
                             text("Talents", x + 20, ly + 70, f"Talents {talents} · {FROM_ADDON.lower()}", 12, 400,
                                  MUTED)]
        if primary:
            block.append(badge("Primary badge", x + COLUMN_W - 20 - 73, ly + 12, "Primary", "success"))
        items.append(Group(f"Loadout {name}", block))
    return Group("Loadouts", items)


def raid_saves(x: float, y: float, edited: bool) -> Group:
    rows = [("Icecrown Citadel", f"{TWENTY_FIVE}, heroic", f"{RESETS} · ID 43127", False),
            ("Vault of Archavon", TWENTY_FIVE, RESETS, False)]
    if edited:
        rows.append(("Ruby Sanctum", TWENTY_FIVE, "Doesn't replace synced saves", True))
    items: list[Item] = [*card(x, y, COLUMN_W, 76 + 56 * len(rows) + 8),
                         *card_title(x, y, RAID_SAVES, f"Complete scan {LAST_SYNC.lower()}")]
    for index, (instance, difficulty, detail, reported) in enumerate(rows):
        ry = y + 76 + index * 56
        row: list[Item] = [Rect("Divider", x, ry, COLUMN_W, 1, DIVIDER),
                           text("Instance", x + 20, ry + 24, f"{instance} · {difficulty}", 13, 600),
                           text("Detail", x + 20, ry + 44, detail, 12, 400, SECONDARY)]
        if reported:
            row.append(badge("Reported badge", x + COLUMN_W - 20 - 129, ry + 10, PLAYER_REPORTED, "warning"))
        items.append(Group(f"Save {instance}", row))
    return Group(RAID_SAVES, items)


EQUIPMENT = [("Head", "Sanctified Scourgelord Helmet"), ("Neck", "Bone Sentinel's Amulet"),
             ("Shoulder", "Sanctified Scourgelord Pauldrons"), ("Chest", "Sanctified Scourgelord Battleplate"),
             ("Hands", "Sanctified Scourgelord Gauntlets"), ("Legs", "Sanctified Scourgelord Legplates"),
             ("Main hand", "Havoc's Call"), ("Off hand", "Frozen Bonespike"), ("Trinket", "Deathbringer's Will"),
             ("Trinket 2", "Whispering Fanged Skull")]


def equipment(x: float, y: float) -> Group:
    row_h = 34
    items: list[Item] = [*card(x, y, COLUMN_W, 76 + row_h * len(EQUIPMENT) + 44),
                         *card_title(x, y, "Equipment", "Primary loadout, Frost DPS")]
    for index, (slot, item) in enumerate(EQUIPMENT):
        ry = y + 76 + index * row_h
        items.append(Group(f"Slot {slot}", [
            text("Slot", x + 20, ry + 20, slot.replace(" 2", ""), 12, 400, MUTED),
            text("Item", x + 96, ry + 20, item, 13),
            text("Item level", x + COLUMN_W - 60, ry + 20, "264", 13, 600, SECONDARY, 40, "right"),
        ]))
    items.append(text("More slots", x + 20, y + 76 + row_h * len(EQUIPMENT) + 24, "7 more slots", 12, 400, MUTED))
    return Group("Equipment", items)


def profile(edited: bool) -> list[Item]:
    x1, x2, x3 = COLUMNS
    items: list[Item] = [
        page_header("Death Knight · Level 80", "Arthasdk", "Icecrown · Alliance · <Citadel Vanguard>"),
        button("Edit button", CONTENT_X + CONTENT_W - 128, CONTENT_TOP + 28, "Edit profile", "secondary", 128,
               Click("navigate", EDIT)),
        sources(x1, TOP), professions(x1, TOP + 212, edited), note(x1, TOP + 212 + (200 if edited else 160), edited),
        loadouts(x2, TOP), raid_saves(x2, TOP + 256, edited), equipment(x3, TOP),
    ]
    if edited:
        tx, ty = BOARD_W - 40 - 380, BOARD_H - 40 - 72
        items.append(Group("Saved notification", [*card(tx, ty, 380, 72, ACCENT),
                                                  text("Title", tx + 24, ty + 31, "Profile saved", 14, 600),
                                                  text("Message", tx + 24, ty + 53, "Your changes are marked player-reported.",
                                                       13, 400, SECONDARY)]))
    return items


# 3 · Edit
def radio(name: str, x: float, y: float, title: str, detail: str, selected: bool) -> Group:
    items: list[Item] = [Rect("Area", x, y, 340, 60, P["Surface/selected"] if selected else CARD, 1, 8,
                              ACCENT if selected else DIVIDER)]
    if selected:
        items += [Circle("Radio", x + 24, y + 30, 8, ACCENT), Circle("Radio dot", x + 24, y + 30, 3, P["Brand/on accent"])]
    else:
        items += [Circle("Radio", x + 24, y + 30, 8, DIVIDER), Circle("Radio hole", x + 24, y + 30, 6, CARD)]
    items += [text("Title", x + 44, y + 26, title, 14, 600, ACCENT if selected else P["Text/primary"]),
              text("Detail", x + 44, y + 45, detail, 12, 400, SECONDARY)]
    return Group(name, items)


def edit() -> list[Item]:
    x1, x2 = CONTENT_X, CONTENT_X + 560
    left: list[Item] = [
        *card(x1, TOP, 540, 512), *card_title(x1, TOP, "Profile"),
        text("Visibility heading", x1 + 20, TOP + 76, "Visible to", 12, 600, SECONDARY),
        radio("Community option", x1 + 20, TOP + 88, "Community", "Every member of Citadel Vanguard", False),
        radio("Officers option", x1 + 20, TOP + 156, "Officers only", "Officers and raid leaders", True),
        text("Visibility note", x1 + 20, TOP + 240, "Officers always see characters that sign up for their raids.", 12,
             400, MUTED),
        form_field("Note field", x1 + 20, TOP + 276, 500, "Note", "Main. Prefers Frost; can tank Blood."),
        form_field("Frost label field", x1 + 20, TOP + 356, 500, "Label for loadout 1 (Frost, 0/53/18)", "Frost DPS"),
        form_field("Blood label field", x1 + 20, TOP + 436, 500, "Label for loadout 2 (Blood, 51/10/10)", BLOOD),
    ]
    right: list[Item] = [
        *card(x2, TOP, 560, 324), *card_title(x2, TOP, "Add what the addon can't see",
                                             "Your additions are marked player-reported."),
        form_field("Profession field", x2 + 20, TOP + 92, 360, "Profession", "Jewelcrafting"),
        form_field("Skill field", x2 + 396, TOP + 92, 144, "Skill", "450"),
        form_field("Instance field", x2 + 20, TOP + 188, 360, "Raid save", "Ruby Sanctum"),
        form_field("Difficulty field", x2 + 396, TOP + 188, 144, "Difficulty", TWENTY_FIVE),
        text("Save rule line 1", x2 + 20, TOP + 280, "A reported raid save never replaces or removes one the companion", 12,
             400, MUTED),
        text("Save rule line 2", x2 + 20, TOP + 298, "synced, and it doesn't make a character available for a raid.", 12,
             400, MUTED),
    ]
    return [
        page_header("Death Knight · Level 80", "Edit Arthasdk", "Synced data can't be edited here; it updates on the next sync."),
        Group("Profile form", left), Group("Additions form", right),
        button("Save button", x2, TOP + 348, "Save", "primary", 104, Click("navigate", SAVED)),
        button("Cancel button", x2 + 112, TOP + 348, "Cancel", "secondary", 104, Click("navigate", PROFILE)),
    ]


def boards() -> list[Board]:
    right, below = BOARD_W + BOARD_GAP, BOARD_H + BOARD_GAP

    def screen(name: str, x: float, y: float, content: list[Item]) -> Board:
        return app_screen(name, x, y, PAGE, content, user=PLAYER, links={PAGE: CHARACTERS})

    return [screen(CHARACTERS, 0, 0, characters()), screen(PROFILE, right, 0, profile(False)),
            screen(EDIT, 0, below, edit()), screen(SAVED, right, below, profile(True))]


def main(repository: Path = REPOSITORY) -> Path:
    return write_mockup(repository, "character-profile", "Character profile", boards(), PALETTE,
                        {"View a profile": CHARACTERS, "Edit a profile": PROFILE})


if __name__ == "__main__":
    print("Wrote", main())
