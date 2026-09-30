"""Sample raid data shared by the roster mockups (#189 to #192), so they show the same players and characters.

The raid is Icecrown Citadel 25 heroic with Ruby Sanctum 25 on Friday 9 October, as in the readiness mockup. Names are
invented; classes, specs and verdicts follow the domain (`WowClass`, `CharacterRole`, `RaidAvailability`,
`ReadinessVerdict`).
"""

from __future__ import annotations

from dataclasses import dataclass

RAID = "Icecrown Citadel"
WHEN = "Fri 9 Oct, 21:00 · 19:00 UTC · Icecrown Citadel 25 heroic + Ruby Sanctum 25"

CLASS_COLOURS = {"Death Knight": "#C41E3A", "Paladin": "#F48CBA", "Priest": "#FFFFFF", "Druid": "#FF7C0A",
                 "Rogue": "#FFF468", "Mage": "#3FC7EB", "Warrior": "#C69B6D", "Shaman": "#0070DD",
                 "Hunter": "#AAD372", "Warlock": "#8788EE"}
PALETTE_ADDITIONS = {f"WoW class/{name}": colour for name, colour in CLASS_COLOURS.items()}

AVAILABLE, RESETS, STALE, LOCKED = "Available", "Resets before raid", "Needs fresh sync", "Locked through raid"
VERDICT_TONES = {AVAILABLE: "success", RESETS: "info", STALE: "warning", LOCKED: "danger"}
AVAILABILITY_TONES = {"Confirmed": "success", "Tentative": "info", "Late": "warning", "Declined": "neutral"}
TANK, HEALER, MELEE, RANGED = "Tank", "Healer", "Melee damage", "Ranged damage"
TODAY = "today"


@dataclass(frozen=True)
class Option:
    character: str
    wow_class: str
    spec: str
    role: str
    verdict: str
    gear_score: str


@dataclass(frozen=True)
class Candidate:
    player: str
    options: list[Option]
    availability: str
    freshness: str
    assignment: str = "Unassigned"
    note: str = ""
    arrival: str = ""

    @property
    def preferred(self) -> Option:
        return self.options[0]


DEATH_KNIGHT, PALADIN = "Death Knight", "Paladin"

CANDIDATES = [
    Candidate("Tomas Hale", [Option("Lothar", PALADIN, "Protection", TANK, LOCKED, "5,600"),
                             Option("Tomasdk", DEATH_KNIGHT, "Blood", TANK, AVAILABLE, "5,310")],
              "Confirmed", TODAY, "Draft A · Tank", "Take Tomasdk if Lothar is saved."),
    Candidate("Bryn Valewood", [Option("Arthasdk", DEATH_KNIGHT, "Frost", MELEE, AVAILABLE, "5,712"),
                                Option("Arthasdk", DEATH_KNIGHT, "Blood", TANK, AVAILABLE, "5,480"),
                                Option("Jainaice", "Mage", "Fire", RANGED, STALE, "5,340")],
              "Confirmed", TODAY, "Draft A · Melee", "Can swap to Blood if we're short on tanks."),
    Candidate("Kiri Dawn", [Option("Kirilight", "Priest", "Holy", HEALER, RESETS, "5,420"),
                            Option("Kirishadow", "Priest", "Shadow", RANGED, AVAILABLE, "5,050")],
              "Tentative", "yesterday", "Bench"),
    Candidate("Arvel Moss", [Option("Moonveil", "Druid", "Restoration", HEALER, AVAILABLE, "5,580")],
              "Confirmed", TODAY, "Draft A · Healer"),
    Candidate("Selm Voss", [Option("Shadestep", "Rogue", "Combat", MELEE, AVAILABLE, "5,490")],
              "Late", TODAY, arrival="21:30"),
    Candidate("Ilsa Brand", [Option("Frostweave", "Mage", "Arcane", RANGED, STALE, "5,610")],
              "Confirmed", "6 days ago"),
    Candidate("Doran Pike", [Option("Stonebrow", "Warrior", "Protection", TANK, AVAILABLE, "5,650"),
                             Option("Stonebrow", "Warrior", "Fury", MELEE, AVAILABLE, "5,520")],
              "Confirmed", TODAY, "Draft A · Tank"),
    Candidate("Mira Quell", [Option("Dawnsong", PALADIN, "Holy", HEALER, AVAILABLE, "5,470")],
              "Declined", TODAY),
]
TOTAL_PLAYERS, TOTAL_OPTIONS = 27, 41
# Offered options per role across all 27 players, and how many players prefer that role.
ROLE_COUNTS = {TANK: (6, 3), HEALER: (9, 7), MELEE: (12, 8), RANGED: (14, 9)}


@dataclass(frozen=True)
class Slot:
    """One roster place: a character and its loadout, or a placeholder for a role (`character` empty)."""

    player: str
    character: str
    wow_class: str
    spec: str
    role: str


def placeholder(role: str) -> Slot:
    return Slot("", "", "", "", role)


# Draft A for the raid: five groups of five, in group order. Two places are placeholders.
DRAFT_A: list[list[Slot]] = [
    [Slot("Doran Pike", "Stonebrow", "Warrior", "Protection", TANK),
     Slot("Tomas Hale", "Tomasdk", DEATH_KNIGHT, "Blood", TANK),
     Slot("Bryn Valewood", "Arthasdk", DEATH_KNIGHT, "Frost", MELEE),
     Slot("Selm Voss", "Shadestep", "Rogue", "Combat", MELEE),
     Slot("Hale Brook", "Brookpaw", "Druid", "Feral", MELEE)],
    [Slot("Arvel Moss", "Moonveil", "Druid", "Restoration", HEALER),
     Slot("Nell Ashby", "Lightwarden", PALADIN, "Holy", HEALER),
     Slot("Oren Tull", "Stormcall", "Shaman", "Restoration", HEALER),
     placeholder(HEALER),
     Slot("Ivo Marsh", "Bladewind", "Warrior", "Fury", MELEE)],
    [Slot("Ilsa Brand", "Frostweave", "Mage", "Arcane", RANGED),
     Slot("Pell Carrow", "Emberlock", "Warlock", "Destruction", RANGED),
     Slot("Wren Halden", "Swiftshot", "Hunter", "Marksmanship", RANGED),
     Slot("Tavi Orm", "Voidcaller", "Priest", "Shadow", RANGED),
     Slot("Cass Rowe", "Starfall", "Druid", "Balance", RANGED)],
    [Slot("Juno Fell", "Crusader", PALADIN, "Retribution", MELEE),
     Slot("Bram Oakes", "Tidefist", "Shaman", "Enhancement", MELEE),
     Slot("Lark Venn", "Nightshade", "Rogue", "Assassination", MELEE),
     Slot("Rook Dale", "Wintergrave", DEATH_KNIGHT, "Unholy", MELEE),
     placeholder(MELEE)],
    [Slot("Faye Lorn", "Pyrelight", "Mage", "Fire", RANGED),
     Slot("Gus Teller", "Soulreaver", "Warlock", "Affliction", RANGED),
     Slot("Hedda Voss", "Longstride", "Hunter", "Survival", RANGED),
     Slot("Sera Winn", "Brightvow", PALADIN, "Holy", HEALER),
     Slot("Quin Yarrow", "Sparkcaller", "Shaman", "Elemental", RANGED)],
]
BENCH = [Slot("Kiri Dawn", "Kirilight", "Priest", "Holy", HEALER),
         Slot("Eli Brant", "Ironhide", "Warrior", "Protection", TANK)]
