"""Community linking and officer permissions (story #14, design story #178, task #213).

Six boards in RaidManager's design system (ADR-0019), following the owner's decisions on #14: a community is linked
by adding the RaidManager bot from the website, and Discord roles map to RaidManager roles.

1. No community yet. Add RaidManager to a Discord server opens Discord's install page, which returns to 2.
2. Choosing the Warmane realm. Finish linking shows 4; Cancel goes back to 1.
3. The server is already linked: another member added the bot again, and the existing community opens.
4. Community settings: server, realm, Administrator and the Discord roles mapped to Officer and Raid leader.
   View members opens 5.
5. Members and the RaidManager role each one gets from their Discord roles. The community card goes back to 4.
6. A raid change refused because the member's Discord officer role was removed; the raid and its history stay.
7. Board 1 after adding the bot didn't finish: the reason, such as a cancel on Discord's page, above the steps (#288).

8. The Administrator changing the officer roles: a Discord role picked for Officer, each mapping with a × to remove it,
   a mapped role deleted in Discord shown as missing, and the confirmation after saving (#289).

Boards 2 and 3 aren't sidebar entries, so no navigation entry is highlighted there, as in the website's shell.

Run from the repository root: python scripts/mockups/community_settings.py
Once the owner edits the design in Penpot, the downloaded file replaces docs/mockups/community-settings.penpot and
this script is no longer run.
"""

from __future__ import annotations

import sys
from pathlib import Path

REPOSITORY = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(REPOSITORY / "scripts"))

from penpot_components import (  # noqa: E402
    BOARD_GAP, BOARD_H, BOARD_W, CONTENT_TOP, CONTENT_W, CONTENT_X, PLAYER, User, app_screen, avatar, badge,
    button, card, notice, page_header,
)
from penpot_scene import HOUSE_PALETTE, Board, Circle, Click, Group, Item, Rect, text, write_mockup  # noqa: E402

P = HOUSE_PALETTE
ACCENT, SECONDARY, MUTED, DIVIDER = P["Brand/accent"], P["Text/secondary"], P["Text/muted"], P["Line/divider"]
SERVER, OWNER = "Citadel Vanguard", "Gihed Annabi"
NEWCOMER = User(OWNER, "Player", "GA", "purple")
ADMINISTRATOR = User(OWNER, "Administrator", "GA", "purple")
DEMOTED = User("Tomas Hale", "Player", "TH", "red")
REALMS = ("Icecrown", "Lordaeron", "Blackrock", "Onyxia")
# The Discord roles this community maps, and the RaidManager role name used in several places.
OFFICER_ROLE, RAID_LEAD_ROLE, RAID_LEADER = "@Officer", "@Raid Lead", "Raid leader"

NO_COMMUNITY = "1 · No community yet"
NOT_FINISHED = "7 · Adding RaidManager didn't finish"
MAPPING = "8 · Changing the officer roles"
CHOOSE_REALM = "2 · Choose the realm"
ALREADY_LINKED = "3 · Server already linked"
SETTINGS = "4 · Community settings"
MEMBERS = "5 · Members and roles"
REFUSED = "6 · Change refused after role loss"
SECTION = "Community settings"
ADD_SECTION = "Add RaidManager"


def section_title(name: str, x: float, y: float, value: str, caption: str | None = None) -> list[Item]:
    items: list[Item] = [text(name, x, y, value, 16, 600)]
    if caption:
        items.append(text(f"{name} caption", x, y + 22, caption, 13, 400, SECONDARY))
    return items


def step(index: int, x: float, y: float, title: str, detail: str) -> Group:
    return Group(f"Step {index}", [
        Circle("Number circle", x + 16, y + 16, 16, P["Surface/raised"]),
        text("Number", x, y + 21, str(index), 14, 700, ACCENT, 32, "center"),
        text("Title", x + 48, y + 14, title, 14, 600),
        text("Detail", x + 48, y + 34, detail, 13, 400, SECONDARY),
    ])


def no_community(failure: tuple[str, str] | None = None) -> list[Item]:
    top, width = CONTENT_TOP + 120, 720
    items: list[Item] = [
        page_header("Get started", "Link your Discord community",
                    "RaidManager plans raids inside a Discord server. Add the bot to your server to start."),
    ]
    if failure:
        items.append(notice("Failure notice", CONTENT_X, top, width, failure[0], failure[1], "danger"))
        top += 96
    return items + [
        Group("Steps card", [
            *card(CONTENT_X, top, width, 288),
            step(1, CONTENT_X + 24, top + 24, "Add RaidManager to your Discord server",
                 "Discord asks which server. You need the Manage Server permission there."),
            step(2, CONTENT_X + 24, top + 88, "Choose your Warmane realm",
                 "Characters, raids and lockouts are checked against this realm."),
            step(3, CONTENT_X + 24, top + 152, "Map your officer roles",
                 "Pick the Discord roles that make someone an Officer or a Raid leader."),
            button("Add bot button", CONTENT_X + 24, top + 224, "Add RaidManager to a Discord server", "primary",
                   300, Click("navigate", CHOOSE_REALM)),
        ]),
        text("Member note", CONTENT_X, top + 324,
             "Is your server already linked? Its community appears here once you're a member of the server.",
             13, 400, MUTED),
    ]


def realm_option(realm: str, x: float, y: float, width: float, selected: bool) -> Group:
    fill = P["Surface/selected"] if selected else P["Surface/card"]
    items: list[Item] = [Rect("Area", x, y, width, 44, fill, 1, 8, ACCENT if selected else DIVIDER)]
    if selected:
        items += [Circle("Radio", x + 24, y + 22, 8, ACCENT), Circle("Radio dot", x + 24, y + 22, 3, P["Brand/on accent"])]
    else:
        items += [Circle("Radio", x + 24, y + 22, 8, DIVIDER), Circle("Radio hole", x + 24, y + 22, 6, P["Surface/card"])]
    items.append(text("Label", x + 44, y + 27, realm, 14, 600 if selected else 400,
                      ACCENT if selected else P["Text/primary"]))
    return Group(f"{realm} option", items)


def choose_realm() -> list[Item]:
    top, width = CONTENT_TOP + 120, 560
    x = CONTENT_X + 24
    options = [realm_option(realm, x, top + 148 + index * 52, width - 48, realm == "Icecrown")
               for index, realm in enumerate(REALMS)]
    return [
        page_header("Almost done", f"Set up {SERVER}",
                    "RaidManager was added to your Discord server. Choose the Warmane realm your community plays on."),
        Group("Realm card", [
            *card(CONTENT_X, top, width, 460),
            text("Server label", x, top + 36, "DISCORD SERVER", 11, 700, MUTED, None, "left", 1.2),
            text("Server", x, top + 62, SERVER, 16, 600),
            text("Added by", x, top + 82, f"Added by {OWNER}, who becomes its Administrator", 13, 400, SECONDARY),
            text("Realm label", x, top + 132, "WARMANE REALM", 11, 700, MUTED, None, "left", 1.2),
            Group("Realm options", list(options)),
            button("Finish button", x, top + 396, "Finish linking", "primary", 140, Click("navigate", SETTINGS)),
            button("Cancel button", x + 148, top + 396, "Cancel", "secondary", 96, Click("navigate", NO_COMMUNITY)),
        ]),
    ]


def already_linked() -> list[Item]:
    top = CONTENT_TOP + 120
    return [
        page_header(ADD_SECTION, f"{SERVER} is already linked",
                    "Each Discord server links to one RaidManager community."),
        notice("Linked notice", CONTENT_X, top, 720, "You're a member of this community",
               f"Its Administrator, {OWNER}, manages the realm and the officer roles.", "info"),
        button("Open overview button", CONTENT_X, top + 96, "Open the overview", "primary", 160),
    ]


def role_row(y: float, role: str, discord_roles: list[str], members: int, x: float, width: float,
             editable: bool) -> Group:
    items: list[Item] = [Rect("Divider", x, y, width, 1, DIVIDER), text("Role", x + 24, y + 34, role, 14, 600)]
    chip_x = x + 184
    if discord_roles and discord_roles[0].startswith("@"):
        for index, discord_role in enumerate(discord_roles):
            items.append(badge(f"Discord role {index + 1}", chip_x, y + 16, discord_role, "info"))
            chip_x += len(discord_role) * 7 + 32
        if editable:
            items.append(text("Add role", chip_x, y + 33, "+ Add Discord role", 13, 600, ACCENT))
    else:
        items.append(text("Source", chip_x, y + 34, discord_roles[0], 13, 400, SECONDARY))
    items.append(text("Members", x + width - 104, y + 34, f"{members} member" + ("" if members == 1 else "s"), 13, 400, SECONDARY, 80, "right"))
    return Group(f"{role} row", items)


def community_summary_card(top: float, width: float) -> Group:
    """The community page's left card: the Discord server, the realm and the Administrator (boards 4 and 8)."""
    return Group("Community card", [
        *card(CONTENT_X, top, width, 264),
        *section_title("Heading", CONTENT_X + 24, top + 40, "Community"),
        text("Server label", CONTENT_X + 24, top + 80, "DISCORD SERVER", 11, 700, MUTED, None, "left", 1.2),
        text("Server", CONTENT_X + 24, top + 102, SERVER, 14),
        text("Realm label", CONTENT_X + 24, top + 142, "WARMANE REALM", 11, 700, MUTED, None, "left", 1.2),
        text("Realm", CONTENT_X + 24, top + 164, REALMS[0], 14),
        text("Owner label", CONTENT_X + 24, top + 204, "ADMINISTRATOR", 11, 700, MUTED, None, "left", 1.2),
        text("Owner", CONTENT_X + 24, top + 226, OWNER, 14),
    ])


def settings() -> list[Item]:
    top = CONTENT_TOP + 120
    left_w, right_x = 400, CONTENT_X + 416
    right_w = CONTENT_W - 416
    toast_x = BOARD_W - 40 - 380
    rows = [("Administrator", ["Added RaidManager to the server"], 1), ("Officer", [OFFICER_ROLE], 3),
            (RAID_LEADER, [RAID_LEAD_ROLE], 2), ("Member", ["Everyone in the Discord server"], 38)]
    return [
        page_header("Community", SERVER, f"Discord server linked to RaidManager on {REALMS[0]}."),
        community_summary_card(top, left_w),
        Group("Roles card", [
            *card(right_x, top, right_w, 432),
            *section_title("Heading", right_x + 24, top + 40, "Officer roles",
                           "Discord roles that give RaidManager permissions. Members get them at the next check."),
            *[role_row(top + 88 + index * 64, role, discord_roles, members, right_x, right_w, role in ("Officer", RAID_LEADER))
              for index, (role, discord_roles, members) in enumerate(rows)],
            button("Members button", right_x + 24, top + 88 + 4 * 64 + 24, "View members", "secondary", 136,
                   Click("navigate", MEMBERS)),
        ]),
        Group("Linked notification", [
            *card(toast_x, 80, 380, 72, ACCENT),
            text("Title", toast_x + 24, 111, f"{SERVER} is linked", 14, 600),
            text("Message", toast_x + 24, 133, "Members see it at their next sign-in.", 13, 400, SECONDARY),
        ]),
    ]


def mapping_roles() -> list[Item]:
    """Board 4 while the Administrator changes the roles: chips with ×, the role picker open, a deleted role, the confirmation."""
    top = CONTENT_TOP + 120
    left_w, right_x = 400, CONTENT_X + 416
    right_w = CONTENT_W - 416
    toast_x = BOARD_W - 40 - 380
    chip_x = right_x + 184

    def row_frame(y: float, role: str, members: int) -> list[Item]:
        return [Rect("Divider", right_x, y, right_w, 1, DIVIDER), text("Role", right_x + 24, y + 34, role, 14, 600),
                text("Members", right_x + right_w - 104, y + 34, f"{members} member" + ("" if members == 1 else "s"),
                     13, 400, SECONDARY, 80, "right")]

    officer_y, leader_y, member_y = top + 152, top + 264, top + 328
    picker_y = officer_y + 56
    return [
        page_header("Community", SERVER, f"Discord server linked to RaidManager on {REALMS[0]}."),
        community_summary_card(top, left_w),
        Group("Roles card", [
            *card(right_x, top, right_w, 416),
            *section_title("Heading", right_x + 24, top + 40, "Officer roles",
                           "Discord roles that give RaidManager permissions. Members get them at the next check."),
            Group("Administrator row", [*row_frame(top + 88, "Administrator", 1),
                                        text("Source", chip_x, top + 122, "Added RaidManager to the server", 13, 400, SECONDARY)]),
            Group("Officer row", [
                *row_frame(officer_y, "Officer", 3),
                badge("Discord role 1", chip_x, officer_y + 16, f"{OFFICER_ROLE}  ×", "info"),
                Rect("Picker", chip_x, picker_y, 220, 36, P["Surface/raised"], 1, 8, DIVIDER),
                text("Picker value", chip_x + 12, picker_y + 23, "@Veteran", 13, 400),
                text("Picker arrow", chip_x + 196, picker_y + 23, "▾", 13, 700, SECONDARY, None, "left", 0, icon=True),
                button("Add button", chip_x + 232, picker_y - 2, "Add", "primary", 64),
                button("Cancel picker button", chip_x + 304, picker_y - 2, "Cancel", "secondary", 80),
            ]),
            Group("Raid leader row", [
                *row_frame(leader_y, RAID_LEADER, 2),
                badge("Discord role 1", chip_x, leader_y + 16, f"{RAID_LEAD_ROLE}  ×", "info"),
                badge("Deleted role", chip_x + 124, leader_y + 16, "Deleted role  ×", "danger"),
                text("Add role", chip_x + 268, leader_y + 33, "+ Add Discord role", 13, 600, ACCENT),
            ]),
            Group("Member row", [*row_frame(member_y, "Member", 38),
                                 text("Source", chip_x, member_y + 34, "Everyone in the Discord server", 13, 400, SECONDARY)]),
        ]),
        Group("Saved notification", [
            *card(toast_x, 80, 380, 72, ACCENT),
            text("Title", toast_x + 24, 111, "Officer roles saved", 14, 600),
            text("Message", toast_x + 24, 133, "Members get them at their next check.", 13, 400, SECONDARY),
        ]),
    ]


MEMBER_ROWS = [
    (OWNER, "GA", "purple", [OFFICER_ROLE], "Administrator", "success"),
    ("Tomas Hale", "TH", "red", [OFFICER_ROLE], "Officer", "success"),
    ("Arvel Moss", "AM", "blue", [OFFICER_ROLE, RAID_LEAD_ROLE], "Officer", "success"),
    ("Bryn Valewood", "BV", "blue", [RAID_LEAD_ROLE], RAID_LEADER, "info"),
    ("Kiri Dawn", "KD", "purple", ["@Raider"], "Member", "neutral"),
    ("Selm Voss", "SV", "red", [], "Member", "neutral"),
]


def members() -> list[Item]:
    top, head_h, row_h = CONTENT_TOP + 120, 44, 64
    columns = {"member": 24, "discord": 400, "role": 760}
    table: list[Item] = card(CONTENT_X, top, CONTENT_W, head_h + row_h * len(MEMBER_ROWS) + 8)
    table.append(Group("Table header", [
        text(f"{label.title()} heading", CONTENT_X + columns[key], top + 27, label, 11, 700, MUTED, None, "left", 1.2)
        for key, label in (("member", "MEMBER"), ("discord", "DISCORD ROLES"), ("role", "RAIDMANAGER ROLE"))]))
    for index, (name, initials, tone, discord_roles, role, badge_tone) in enumerate(MEMBER_ROWS):
        y = top + head_h + index * row_h
        row: list[Item] = [Rect("Divider", CONTENT_X, y, CONTENT_W, 1, DIVIDER),
                           avatar("Avatar", CONTENT_X + 40, y + 32, initials, tone),
                           text("Name", CONTENT_X + 68, y + 37, name, 14, 600),
                           text("Discord roles", CONTENT_X + columns["discord"], y + 37,
                                ", ".join(discord_roles) or "No roles", 13, 400, SECONDARY),
                           badge("Role badge", CONTENT_X + columns["role"], y + 20, role, badge_tone)]
        table.append(Group(f"Row {name}", row))
    return [
        page_header("Community", "Members and roles",
                    "Roles follow Discord. The highest mapped role applies; everyone else in the server is a Member."),
        Group("Members table", table),
        text("Checked note", CONTENT_X, top + head_h + row_h * len(MEMBER_ROWS) + 40,
             "Last checked with Discord today at 18:40.", 13, 400, MUTED),
    ]


def refused() -> list[Item]:
    top = CONTENT_TOP + 120
    return [
        page_header("Raids", "Icecrown Citadel 25 Heroic", "Friday 2 October, 20:00 realm time"),
        notice("Refused notice", CONTENT_X, top, 720, "Only officers can change this raid",
               "Your Officer role was removed in Discord, so the change wasn't saved. The raid and its history stay.",
               "danger"),
        button("Back button", CONTENT_X, top + 96, "Back to raids", "secondary", 136),
    ]


def boards() -> list[Board]:
    column, row = BOARD_W + BOARD_GAP, BOARD_H + BOARD_GAP
    return [
        app_screen(NO_COMMUNITY, 0, 0, "Overview", no_community(), community=None, user=NEWCOMER),
        app_screen(CHOOSE_REALM, column, 0, ADD_SECTION, choose_realm(), community=None, user=NEWCOMER),
        app_screen(ALREADY_LINKED, 2 * column, 0, ADD_SECTION, already_linked(), user=PLAYER),
        app_screen(SETTINGS, 0, row, SECTION, settings(), user=ADMINISTRATOR),
        app_screen(MEMBERS, column, row, SECTION, members(), section="Members", user=ADMINISTRATOR,
                   links={"Community": SETTINGS}),
        app_screen(REFUSED, 2 * column, row, "Raids", refused(), user=DEMOTED),
        app_screen(NOT_FINISHED, 0, 2 * row, "Overview",
                   no_community(("RaidManager wasn't added", "You cancelled on Discord's page. Nothing was linked.")),
                   community=None, user=NEWCOMER),
        app_screen(MAPPING, column, 2 * row, SECTION, mapping_roles(), user=ADMINISTRATOR),
    ]


def main(repository: Path = REPOSITORY) -> Path:
    return write_mockup(repository, "community-settings", "Community settings", boards(),
                        flows={"Link a community": NO_COMMUNITY, "Server already linked": ALREADY_LINKED,
                               "Change refused after role loss": REFUSED})


if __name__ == "__main__":
    print("Wrote", main())
