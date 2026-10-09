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

Custom roles (story #308, task #310), following the owner's decisions on #308:

9. The roles card with created roles: each row names what the role allows. Create role opens 10, Edit on Veteran
   opens 11, View members opens 13.
10. Creating a role: a name and the five permissions. Cancel and Create role go back to 9.
11. Editing Veteran: the same form with Delete, which opens 12. Cancel and Save go back to 9.
12. Deleting Veteran: Cancel goes back to 11, Delete role to 9.
13. Members with every role they have, one badge each.
14. The card as a member whose role grants Manage community roles sees it: they change every other role, but only the
    Administrator changes a role that grants Manage community roles, their own included. In their role form, Manage
    community roles can't be ticked, so nobody widens their own rights.
The Administrator's name moves into the page's subtitle on boards 9 to 15, because the roles card takes the full width.
15. The card as a Member sees it: read-only.

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
    button, card, checkbox, form_field, notice, page_header, toast,
)
from penpot_scene import HOUSE_PALETTE, Board, Circle, Click, Group, Item, Rect, text, write_mockup  # noqa: E402

P = HOUSE_PALETTE
ACCENT, SECONDARY, MUTED, DIVIDER = P["Brand/accent"], P["Text/secondary"], P["Text/muted"], P["Line/divider"]
SERVER, OWNER = "Citadel Vanguard", "Gihed Annabi"
# Names and labels used on several boards.
TOMAS = "Tomas Hale"
SELM = "Selm Voss"
VETERAN_ROLE = "@Veteran"
CANCEL_BUTTON = "Cancel button"
ADD_ROLE = "Add role"
ADD_ROLE_LABEL = "+ Add Discord role"
ROLES_CARD = "Roles card"
NEWCOMER = User(OWNER, "Player", "GA", "purple")
ADMINISTRATOR = User(OWNER, "Administrator", "GA", "purple")
DEMOTED = User(TOMAS, "Player", "TH", "red")
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
ROLES = "9 · Roles with created roles"
CREATE_ROLE = "10 · Create a role"
EDIT_ROLE = "11 · Edit a role"
DELETE_ROLE = "12 · Delete a role"
MEMBER_ROLES = "13 · Members with several roles"
ROLE_MANAGER = "14 · Roles as a role manager"
READ_ONLY = "15 · Roles as a member"
ROLE_MANAGER_USER = User(TOMAS, "Council", "TH", "red")
MEMBER_USER = User(SELM, "Player", "SV", "red")
# The permissions a role can allow (owner decision on #308), with the short word each row lists.
PERMISSIONS = [
    ("Manage raids", "Create and edit raids, templates and recurrence; lock or reopen signups.", "Raids"),
    ("Build rosters", "Select and swap participants, record exceptions, publish, set boss assignments.", "Rosters"),
    ("Run raid night", "Record attendance and export the roster to the addon.", "Raid night"),
    ("Review conflicts", "Decide character claims another player already owns.", "Conflicts"),
    ("Manage community roles", "Create, edit and delete roles and map Discord roles to them.", "Roles"),
]
# (role, Discord roles, permissions allowed by index, members, created by the Administrator)
CUSTOM_ROWS = [
    ("Officer", [OFFICER_ROLE], [0, 1, 2, 3], 3),
    (RAID_LEADER, [RAID_LEAD_ROLE], [0, 1, 2], 2),
    ("Council", ["@Council"], [0, 1, 2, 3, 4], 1),
    ("Veteran", [VETERAN_ROLE], [2], 6),
]
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
            button(CANCEL_BUTTON, x + 148, top + 396, "Cancel", "secondary", 96, Click("navigate", NO_COMMUNITY)),
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
            items.append(text(ADD_ROLE, chip_x, y + 33, ADD_ROLE_LABEL, 13, 600, ACCENT))
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
    rows = [("Administrator", ["Added RaidManager to the server"], 1), ("Officer", [OFFICER_ROLE], 3),
            (RAID_LEADER, [RAID_LEAD_ROLE], 2), ("Member", ["Everyone in the Discord server"], 38)]
    return [
        page_header("Community", SERVER, f"Discord server linked to RaidManager on {REALMS[0]}."),
        community_summary_card(top, left_w),
        Group(ROLES_CARD, [
            *card(right_x, top, right_w, 432),
            *section_title("Heading", right_x + 24, top + 40, "Officer roles",
                           "Discord roles that give RaidManager permissions. Members get them at the next check."),
            *[role_row(top + 88 + index * 64, role, discord_roles, members, right_x, right_w, role in ("Officer", RAID_LEADER))
              for index, (role, discord_roles, members) in enumerate(rows)],
            button("Members button", right_x + 24, top + 88 + 4 * 64 + 24, "View members", "secondary", 136,
                   Click("navigate", MEMBERS)),
        ]),
        toast("Linked notification", f"{SERVER} is linked", "Members see it at their next sign-in."),
    ]


def mapping_roles() -> list[Item]:
    """Board 4 while the Administrator changes the roles: chips with ×, the role picker open, a deleted role, the confirmation."""
    top = CONTENT_TOP + 120
    left_w, right_x = 400, CONTENT_X + 416
    right_w = CONTENT_W - 416
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
        Group(ROLES_CARD, [
            *card(right_x, top, right_w, 416),
            *section_title("Heading", right_x + 24, top + 40, "Officer roles",
                           "Discord roles that give RaidManager permissions. Members get them at the next check."),
            Group("Administrator row", [*row_frame(top + 88, "Administrator", 1),
                                        text("Source", chip_x, top + 122, "Added RaidManager to the server", 13, 400, SECONDARY)]),
            Group("Officer row", [
                *row_frame(officer_y, "Officer", 3),
                badge("Discord role 1", chip_x, officer_y + 16, f"{OFFICER_ROLE}  ×", "info"),
                Rect("Picker", chip_x, picker_y, 220, 36, P["Surface/raised"], 1, 8, DIVIDER),
                text("Picker value", chip_x + 12, picker_y + 23, VETERAN_ROLE, 13, 400),
                text("Picker arrow", chip_x + 196, picker_y + 23, "▾", 13, 700, SECONDARY, None, "left", 0, icon=True),
                button("Add button", chip_x + 232, picker_y - 2, "Add", "primary", 64),
                button("Cancel picker button", chip_x + 304, picker_y - 2, "Cancel", "secondary", 80),
            ]),
            Group("Raid leader row", [
                *row_frame(leader_y, RAID_LEADER, 2),
                badge("Discord role 1", chip_x, leader_y + 16, f"{RAID_LEAD_ROLE}  ×", "info"),
                badge("Deleted role", chip_x + 124, leader_y + 16, "Deleted role  ×", "danger"),
                text(ADD_ROLE, chip_x + 268, leader_y + 33, ADD_ROLE_LABEL, 13, 600, ACCENT),
            ]),
            Group("Member row", [*row_frame(member_y, "Member", 38),
                                 text("Source", chip_x, member_y + 34, "Everyone in the Discord server", 13, 400, SECONDARY)]),
        ]),
        toast("Saved notification", "Officer roles saved", "Members get them at their next check."),
    ]


MEMBER_ROWS = [
    (OWNER, "GA", "purple", [OFFICER_ROLE], "Administrator", "success"),
    (TOMAS, "TH", "red", [OFFICER_ROLE], "Officer", "success"),
    ("Arvel Moss", "AM", "blue", [OFFICER_ROLE, RAID_LEAD_ROLE], "Officer", "success"),
    ("Bryn Valewood", "BV", "blue", [RAID_LEAD_ROLE], RAID_LEADER, "info"),
    ("Kiri Dawn", "KD", "purple", ["@Raider"], "Member", "neutral"),
    (SELM, "SV", "red", [], "Member", "neutral"),
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


def allows(permissions: list[int]) -> str:
    return "Allows " + ", ".join(PERMISSIONS[index][2].lower() for index in permissions)


def custom_role_row(y: float, role: str, discord_roles: list[str], permissions: list[int], members: int, x: float,
                    width: float, mode: str, on_edit: Click | None = None) -> Group:
    """A role row on the roles card: `edit`able, `locked` for a role manager, or `read` only."""
    chip_x = x + 200
    items: list[Item] = [Rect("Divider", x, y, width, 1, DIVIDER), text("Role", x + 24, y + 40, role, 14, 600)]
    for index, discord_role in enumerate(discord_roles):
        label = f"{discord_role}  ×" if mode == "edit" else discord_role
        items.append(badge(f"Discord role {index + 1}", chip_x, y + 16, label, "info"))
        chip_x += len(label) * 7 + 32
    if mode == "edit":
        items.append(text(ADD_ROLE, chip_x, y + 33, ADD_ROLE_LABEL, 13, 600, ACCENT))
    items.append(text("Permissions", x + 200, y + 62, allows(permissions), 12, 400, MUTED))
    items.append(text("Members", x + width - 176, y + 40, f"{members} member" + ("" if members == 1 else "s"), 13, 400,
                      SECONDARY, 80, "right"))
    if mode == "edit":
        items.append(Group("Edit link", [text("Label", x + width - 64, y + 40, "Edit", 13, 600, ACCENT)], on_edit))
    elif mode == "locked":
        items.append(text("Locked note", x + width - 64, y + 40, "Locked", 13, 600, MUTED))
    return Group(f"{role} row", items)


def roles_card(mode: str, top: float, x: float, width: float, links: bool = False) -> Group:
    """The roles card with created roles: `admin` edits all, `manager` all but roles that grant Manage community roles."""
    row_h = 80
    items: list[Item] = [*card(x, top, width, 104 + row_h * 6 + 72)]
    items += section_title("Heading", x + 24, top + 40, "Roles",
                           "Discord roles give RaidManager roles. Members get them at the next check.")
    if mode != "member":
        items.append(button("Create role button", x + width - 24 - 120, top + 20, "Create role", "primary", 120,
                            Click("navigate", CREATE_ROLE) if links else None))
    y = top + 88
    items.append(Group("Administrator row", [
        Rect("Divider", x, y, width, 1, DIVIDER), text("Role", x + 24, y + 40, "Administrator", 14, 600),
        text("Source", x + 200, y + 40, "Added RaidManager to the server; allows everything", 13, 400, SECONDARY),
        text("Members", x + width - 176, y + 40, "1 member", 13, 400, SECONDARY, 80, "right")]))
    for index, (role, discord_roles, permissions, members) in enumerate(CUSTOM_ROWS):
        row_mode = {"admin": "edit", "member": "read"}.get(mode, "locked" if 4 in permissions else "edit")
        on_edit = Click("navigate", EDIT_ROLE) if links and role == "Veteran" else None
        items.append(custom_role_row(y + row_h * (index + 1), role, discord_roles, permissions, members, x, width,
                                     row_mode, on_edit))
    member_y = y + row_h * 5
    items.append(Group("Member row", [
        Rect("Divider", x, member_y, width, 1, DIVIDER), text("Role", x + 24, member_y + 40, "Member", 14, 600),
        text("Source", x + 200, member_y + 40, "Everyone in the Discord server; allows signing up", 13, 400, SECONDARY),
        text("Members", x + width - 176, member_y + 40, "38 members", 13, 400, SECONDARY, 80, "right")]))
    items.append(button("Members button", x + 24, member_y + row_h + 16, "View members", "secondary", 136,
                        Click("navigate", MEMBER_ROLES) if links else None))
    return Group(ROLES_CARD, items)


def custom_roles(mode: str = "admin", links: bool = True) -> list[Item]:
    top = CONTENT_TOP + 120
    items: list[Item] = [
        page_header("Community", SERVER, f"Discord server linked to RaidManager on {REALMS[0]}. Administrator: {OWNER}."),
        roles_card(mode, top, CONTENT_X, CONTENT_W, links),
    ]
    if mode == "manager":
        items.insert(1, notice("Locked notice", CONTENT_X + CONTENT_W - 560, CONTENT_TOP + 12, 560,
                               "Some roles are locked", "Only the Administrator changes a role that manages roles.", "info"))
    return items


def role_dialog(title: str, name: str, ticked: list[int], editing: bool) -> list[Item]:
    w = 560
    h = 168 + 56 * len(PERMISSIONS) + 96
    x, y = (BOARD_W - w) / 2, (BOARD_H - h) / 2
    items: list[Item] = [*card(x, y, w, h), text("Title", x + 24, y + 44, title, 18, 700),
                         form_field("Name field", x + 24, y + 84, w - 48, "Name", name),
                         text("Permissions label", x + 24, y + 168, "PERMISSIONS", 11, 700, MUTED, None, "left", 1.2)]
    for index, (label, detail, _) in enumerate(PERMISSIONS):
        row_y = y + 184 + index * 56
        items.append(Group(f"{label} option", [*checkbox(x + 24, row_y + 4, index in ticked),
                                                text("Name", x + 56, row_y + 18, label, 14, 600),
                                                text("Detail", x + 56, row_y + 38, detail, 12, 400, SECONDARY)]))
    buttons_y = y + h - 64
    if editing:
        items.append(button("Delete button", x + 24, buttons_y, "Delete role", "danger", 120, Click("navigate", DELETE_ROLE)))
    save = "Save" if editing else "Create role"
    save_w = 80 if editing else 120
    items += [button(CANCEL_BUTTON, x + w - 24 - save_w - 8 - 96, buttons_y, "Cancel", "secondary", 96, Click("navigate", ROLES)),
              button("Save button", x + w - 24 - save_w, buttons_y, save, "primary", save_w, Click("navigate", ROLES))]
    return [Rect("Dim overlay", 0, 0, BOARD_W, BOARD_H, P["Neutral/black"], 0.6), Group("Role dialog", items)]


def delete_dialog() -> list[Item]:
    w, h = 480, 184
    x, y = (BOARD_W - w) / 2, (BOARD_H - h) / 2
    return [Rect("Dim overlay", 0, 0, BOARD_W, BOARD_H, P["Neutral/black"], 0.6), Group("Delete dialog", [
        *card(x, y, w, h),
        text("Title", x + 24, y + 44, "Delete Veteran?", 18, 700),
        text("Body line 1", x + 24, y + 80, "6 members lose what it allows. The @Veteran role stays in", 14, 400, SECONDARY),
        text("Body line 2", x + 24, y + 100, "Discord, and its raid history stays in RaidManager.", 14, 400, SECONDARY),
        button(CANCEL_BUTTON, x + w - 24 - 120 - 8 - 96, y + h - 64, "Cancel", "secondary", 96, Click("navigate", EDIT_ROLE)),
        button("Confirm delete button", x + w - 24 - 120, y + h - 64, "Delete role", "danger", 120, Click("navigate", ROLES)),
    ])]


SEVERAL_ROLE_ROWS = [
    (OWNER, "GA", "purple", [OFFICER_ROLE], [("Administrator", "success")]),
    (TOMAS, "TH", "red", [OFFICER_ROLE, "@Council"], [("Officer", "success"), ("Council", "info")]),
    ("Arvel Moss", "AM", "blue", [OFFICER_ROLE, RAID_LEAD_ROLE], [("Officer", "success"), (RAID_LEADER, "info")]),
    ("Bryn Valewood", "BV", "blue", [RAID_LEAD_ROLE, VETERAN_ROLE], [(RAID_LEADER, "info"), ("Veteran", "info")]),
    ("Kiri Dawn", "KD", "purple", [VETERAN_ROLE], [("Veteran", "info")]),
    (SELM, "SV", "red", [], [("Member", "neutral")]),
]


def several_roles() -> list[Item]:
    top, head_h, row_h = CONTENT_TOP + 120, 44, 64
    columns = {"member": 24, "discord": 400, "role": 720}
    table: list[Item] = card(CONTENT_X, top, CONTENT_W, head_h + row_h * len(SEVERAL_ROLE_ROWS) + 8)
    table.append(Group("Table header", [
        text(f"{label.title()} heading", CONTENT_X + columns[key], top + 27, label, 11, 700, MUTED, None, "left", 1.2)
        for key, label in (("member", "MEMBER"), ("discord", "DISCORD ROLES"), ("role", "RAIDMANAGER ROLES"))]))
    for index, (name, initials, tone, discord_roles, roles) in enumerate(SEVERAL_ROLE_ROWS):
        y = top + head_h + index * row_h
        row: list[Item] = [Rect("Divider", CONTENT_X, y, CONTENT_W, 1, DIVIDER),
                           avatar("Avatar", CONTENT_X + 40, y + 32, initials, tone),
                           text("Name", CONTENT_X + 68, y + 37, name, 14, 600),
                           text("Discord roles", CONTENT_X + columns["discord"], y + 37,
                                ", ".join(discord_roles) or "No roles", 13, 400, SECONDARY)]
        badge_x = CONTENT_X + columns["role"]
        for badge_index, (role, tone_name) in enumerate(roles):
            row.append(badge(f"Role badge {badge_index + 1}", badge_x, y + 20, role, tone_name))
            badge_x += len(role) * 7 + 32
        table.append(Group(f"Row {name}", row))
    return [
        page_header("Community", "Members and roles",
                    "Roles follow Discord. A member has every role their Discord roles give; everyone else is a Member."),
        Group("Members table", table),
        text("Checked note", CONTENT_X, top + head_h + row_h * len(SEVERAL_ROLE_ROWS) + 40,
             "Last checked with Discord today at 18:40 UTC.", 13, 400, MUTED),
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
        app_screen(ROLES, 2 * column, 2 * row, SECTION, custom_roles(), user=ADMINISTRATOR),
        app_screen(CREATE_ROLE, 0, 3 * row, SECTION,
                   custom_roles(links=False) + role_dialog("Create a role", "Veteran", [2], editing=False),
                   user=ADMINISTRATOR),
        app_screen(EDIT_ROLE, column, 3 * row, SECTION,
                   custom_roles(links=False) + role_dialog("Edit Veteran", "Veteran", [2], editing=True),
                   user=ADMINISTRATOR),
        app_screen(DELETE_ROLE, 2 * column, 3 * row, SECTION, custom_roles(links=False) + delete_dialog(),
                   user=ADMINISTRATOR),
        app_screen(MEMBER_ROLES, 0, 4 * row, SECTION, several_roles(), section="Members", user=ADMINISTRATOR,
                   links={"Community": ROLES}),
        app_screen(ROLE_MANAGER, column, 4 * row, SECTION, custom_roles("manager", links=False), user=ROLE_MANAGER_USER),
        app_screen(READ_ONLY, 2 * column, 4 * row, SECTION, custom_roles("member", links=False), user=MEMBER_USER),
    ]


def main(repository: Path = REPOSITORY) -> Path:
    return write_mockup(repository, "community-settings", "Community settings", boards(),
                        flows={"Link a community": NO_COMMUNITY, "Server already linked": ALREADY_LINKED,
                               "Change refused after role loss": REFUSED, "Create and delete a role": ROLES})


if __name__ == "__main__":
    print("Wrote", main())
