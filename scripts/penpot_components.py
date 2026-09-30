"""RaidManager's app shell and shared components for mockups, drawn with the scene model (ADR-0019).

Every website screen is an `app_screen`: a 1440 x 900 board with the sidebar (logo, community card, the Player and
Officer navigation, the signed-in user), the top bar (breadcrumb, realm status, avatar, Sign out) and the screen's
own content in the content area. Signed-out pages (sign-in and its failures) are a `public_screen` instead: the logo
above a centred column, without the shell. The components (`page_header`, `card`, `button`, `badge`, `avatar`,
`notice`) draw the same parts the same way on every screen. All colours come from `HOUSE_PALETTE`.

    from penpot_components import CONTENT_X, CONTENT_TOP, app_screen, button, page_header
    board = app_screen("1 · Filled", 0, 0, "My characters", [page_header(...), button(...)])
"""

from __future__ import annotations

from dataclasses import dataclass

from penpot_scene import HOUSE_PALETTE, Board, Circle, Click, Group, Item, Rect, text, text_width

P = HOUSE_PALETTE
# The palette colours the shell and components use most, named once.
ACCENT = P["Brand/accent"]
ON_ACCENT = P["Brand/on accent"]
TEXT = P["Text/primary"]
SECONDARY = P["Text/secondary"]
MUTED = P["Text/muted"]
DIVIDER = P["Line/divider"]
BLUE = P["Accent/blue"]
DANGER_TEXT = P["Status/danger text"]
PAGE_BACKGROUND = P["Surface/page"]
RAISED = P["Surface/raised"]
BOARD_W, BOARD_H = 1440, 900
SIDEBAR_W, TOP_BAR_H = 240, 64
CONTENT_X, CONTENT_TOP = SIDEBAR_W + 40, TOP_BAR_H + 40
CONTENT_W = BOARD_W - CONTENT_X - 40
BOARD_GAP = 80
PLAYER_PAGES = ("Overview", "Raids", "My characters", "Readiness", "Companion & sync")
OFFICER_PAGES = ("Schedule", "Roster builder", "Raid night")
NAV_TOP, NAV_ITEM_H, NAV_ITEM_GAP = 176, 36, 4
SIGN_OUT_W = 96
# Signed-out pages (sign-in and its failures) are centred, without the app shell (ADR-0019).
PUBLIC_W = 480
PUBLIC_X = (BOARD_W - PUBLIC_W) / 2
# (fill, text) per tone; the text passes WCAG AA on the fill.
BADGE_TONES = {
    "warning": (P["Status/warning background"], P["Status/warning title"]),
    "danger": (P["Status/danger background"], DANGER_TEXT),
    "success": (P["Status/success background"], ACCENT),
    "info": (P["Status/info background"], BLUE),
    "neutral": (RAISED, SECONDARY),
}
# (background, border, title, body, icon, icon mark) per tone.
NOTICE_TONES = {
    "warning": (P["Status/warning background"], P["Status/warning border"], P["Status/warning title"],
                P["Status/warning text"], P["Accent/amber"], ON_ACCENT, "!"),
    "danger": (P["Status/danger background"], P["Status/danger border"], DANGER_TEXT,
               DANGER_TEXT, P["Status/danger"], P["Status/on danger"], "!"),
    "info": (P["Status/info background"], P["Status/info border"], TEXT, SECONDARY,
             BLUE, ON_ACCENT, "i"),
    "success": (P["Status/success background"], P["Status/success border"], TEXT, SECONDARY,
                ACCENT, ON_ACCENT, "✓"),
}
AVATAR_TONES = {
    "blue": (P["Avatar/blue"], BLUE),
    "red": (P["Avatar/red"], P["Avatar/red text"]),
    "purple": (P["Avatar/purple"], P["Avatar/purple text"]),
}
# (fill, border, text) per button style.
BUTTON_STYLES = {
    "primary": (ACCENT, None, ON_ACCENT),
    "secondary": (RAISED, DIVIDER, TEXT),
    "danger": (P["Status/danger"], None, P["Status/on danger"]),
}


@dataclass(frozen=True)
class Community:
    name: str = "Citadel Vanguard"
    realm: str = "Icecrown"
    initials: str = "CV"


@dataclass(frozen=True)
class User:
    name: str = "Gihed Annabi"
    role: str = "Community officer"
    initials: str = "GA"
    tone: str = "purple"

    @property
    def officer(self) -> bool:
        return self.role != "Player"


OFFICER = User()
PLAYER = User("Bryn Valewood", "Player", "BV", "blue")


def avatar(name: str, cx: float, cy: float, initials: str, tone: str = "purple", radius: float = 16) -> Group:
    fill, colour = AVATAR_TONES[tone]
    return Group(name, [Circle("Circle", cx, cy, radius, fill),
                        text("Initials", cx - radius, cy + 4, initials, 12, 800, colour, 2 * radius, "center")])


def navigation_item(label: str, y: float, active: bool, target: str | None) -> Group:
    colour = ACCENT if active else SECONDARY
    items: list[Item] = [Rect("Area", 12, y, SIDEBAR_W - 24, NAV_ITEM_H,
                              P["Surface/selected"] if active else P["Surface/sidebar"], 1, 8)]
    if active:
        items.append(Rect("Active mark", 12, y + 8, 3, NAV_ITEM_H - 16, ACCENT, 1, 2))
    items += [Circle("Icon", 34, y + NAV_ITEM_H / 2, 4, ACCENT if active else MUTED),
              text("Label", 50, y + 23, label, 14, 600 if active else 400, colour)]
    return Group(f"{label} link", items, Click("navigate", target) if target else None)


def community_card(community: Community | None, target: str | None) -> Group:
    """The sidebar's community card; `None` is a signed-in user who hasn't joined or linked a community yet."""
    items: list[Item] = [Rect("Background", 16, 68, SIDEBAR_W - 32, 60, P["Surface/card"], 1, 10,
                              P["Line/card border"])]
    if community is None:
        items += [Rect("Icon", 28, 82, 32, 32, RAISED, 1, 8, DIVIDER),
                  text("Icon mark", 28, 104, "+", 16, 800, SECONDARY, 32, "center", icon=True),
                  text("Name", 70, 95, "No community yet", 14, 600),
                  text("Realm", 70, 114, "Link a Discord server", 12, 400, MUTED)]
    else:
        items += [Rect("Icon", 28, 82, 32, 32, P["Community/icon"], 1, 8),
                  text("Initials", 28, 102, community.initials, 12, 800, PAGE_BACKGROUND, 32, "center"),
                  text("Name", 70, 95, community.name, 14, 600),
                  text("Realm", 70, 114, f"{community.realm} · Community", 12, 400, MUTED)]
    return Group("Community card", items, Click("navigate", target) if target else None)


def sidebar(active: str, community: Community | None, user: User, links: dict[str, str]) -> Group:
    """The left sidebar: logo, community card, navigation by role, and the signed-in user at the bottom.

    `links` maps navigation entries, and "Community" for the community card, to the boards they open.
    """
    items: list[Item] = [
        Rect("Background", 0, 0, SIDEBAR_W, BOARD_H, P["Surface/sidebar"]),
        Rect("Edge", SIDEBAR_W - 1, 0, 1, BOARD_H, DIVIDER),
        Group("Logo", [Rect("Tile", 20, 16, 32, 32, P["Brand/logo tile"], 1, 8),
                       Rect("Mark", 28, 24, 16, 16, ACCENT, 1, 4),
                       text("Raid", 62, 38, "RAID", 16, 800, TEXT, None, "left", 0.5),
                       text("Manager", 104, 38, "MANAGER", 16, 800, ACCENT, None, "left", 0.5)]),
        community_card(community, links.get("Community")),
    ]
    sections = [("Player", PLAYER_PAGES)] + ([("Officer", OFFICER_PAGES)] if user.officer else [])
    y = NAV_TOP - 12
    for section, pages in sections:
        nav: list[Item] = [text("Heading", 24, y, section.upper(), 11, 700, MUTED, None, "left", 1.2)]
        y += 12
        for page in pages:
            nav.append(navigation_item(page, y, page == active, links.get(page)))
            y += NAV_ITEM_H + NAV_ITEM_GAP
        items.append(Group(f"{section} navigation", nav))
        y += 28
    items.append(Group("User card", [
        Rect("Divider", 0, BOARD_H - 72, SIDEBAR_W - 1, 1, DIVIDER),
        avatar("Avatar", 36, BOARD_H - 36, user.initials, user.tone),
        text("Name", 62, BOARD_H - 40, user.name, 14, 600),
        text("Role", 62, BOARD_H - 21, user.role, 12, 400, MUTED),
    ]))
    return Group("Sidebar", items)


def top_bar(community: Community | None, page: str, user: User, sign_out: Click | None = None) -> Group:
    """The top bar: breadcrumb (community / page), realm status, the user's avatar and Sign out."""
    width = BOARD_W - SIDEBAR_W
    crumb = text("Community", CONTENT_X, 37, (community.name if community else "RaidManager").upper(), 11, 700, MUTED, None, "left", 1.2)
    slash = text("Separator", CONTENT_X + text_width(crumb) + 10, 37, "/", 11, 700, MUTED, None, "left", 1.2)
    current = text("Page", slash.x + 16, 37, page.upper(), 11, 700, TEXT, None, "left", 1.2)
    sign_out_x = BOARD_W - 40 - SIGN_OUT_W
    avatar_x = sign_out_x - 16 - 16
    status_x = avatar_x - 16 - 132
    return Group("Top bar", [
        Rect("Background", SIDEBAR_W, 0, width, TOP_BAR_H, P["Surface/top bar"]),
        Rect("Divider", SIDEBAR_W, TOP_BAR_H - 1, width, 1, DIVIDER),
        Group("Breadcrumb", [crumb, slash, current]),
        Group("Realm status", [Circle("Dot", status_x, 32, 4, ACCENT),
                               text("Label", status_x + 12, 37, "REALM ONLINE", 11, 700, ACCENT, None,
                                    "left", 1.2)]),
        avatar("Avatar", avatar_x, 32, user.initials, user.tone),
        button("Sign out button", sign_out_x, 12, "Sign out", "secondary", SIGN_OUT_W, sign_out),
    ])


def app_screen(name: str, x: float, y: float, page: str, content: list[Item], *, section: str | None = None,
               community: Community | None = Community(), user: User = OFFICER,
               links: dict[str, str] | None = None, sign_out: str | None = None) -> Board:
    """A website screen: the app shell with `page` active in the navigation, and `content` in the content area.

    `section` names the page in the breadcrumb when it differs from the navigation entry; `community=None` is a user
    without a community; `links` maps navigation entries (and "Community" for the community card) to boards, and `sign_out` names the board Sign out leads to, for the prototype.
    """
    sign_out_click = Click("navigate", sign_out) if sign_out else None
    shell: list[Item] = [sidebar(page, community, user, links or {}),
                         top_bar(community, section or page, user, sign_out_click)]
    return Board(name, x, y, BOARD_W, BOARD_H, PAGE_BACKGROUND, shell + content)


def page_header(eyebrow: str, title: str, subtitle: str, x: float = CONTENT_X, y: float = CONTENT_TOP) -> Group:
    """The eyebrow (teal, uppercase), the page title and one line of subtitle; 96 px tall."""
    return Group("Page header", [
        text("Eyebrow", x, y + 14, eyebrow.upper(), 12, 700, ACCENT, None, "left", 1.5),
        text("Title", x, y + 56, title, 32, 700),
        text("Subtitle", x, y + 86, subtitle, 15, 400, SECONDARY),
    ])


def card(x: float, y: float, w: float, h: float, accent: str | None = None, fill: str | None = None) -> list[Item]:
    """A card's background with its outline, and an optional coloured bar on its left edge."""
    items: list[Item] = [Rect("Card", x, y, w, h, fill or P["Surface/card"], 1, 12, P["Line/card border"])]
    if accent:
        items.append(Rect("Accent", x, y + 12, 4, h - 24, accent, 1, 2))
    return items


def button(name: str, x: float, y: float, label: str, style: str = "primary", width: float | None = None,
           on_click: Click | None = None) -> Group:
    """A 40 px button with an 8 px radius: `primary` (teal), `secondary` (raised, outlined) or `danger`."""
    fill, border, colour = BUTTON_STYLES[style]
    label_layer = text("Label", x, y + 25, label, 13, 700, colour)
    width = width or round(text_width(label_layer) + 40)
    label_layer.width, label_layer.align = width, "center"
    return Group(name, [Rect("Background", x, y, width, 40, fill, 1, 8, border), label_layer], on_click)


def badge(name: str, x: float, y: float, label: str, tone: str = "neutral") -> Group:
    """A 24 px pill with a word; status is never colour alone."""
    fill, colour = BADGE_TONES[tone]
    width = round(len(label) * 7 + 24)
    return Group(name, [Rect("Background", x, y, width, 24, fill, 1, 12),
                        text("Label", x, y + 16, label, 12, 600, colour, width, "center")])


def notice(name: str, x: float, y: float, w: float, title: str, body: str, tone: str = "warning") -> Group:
    """A 72 px message box with an icon, a title and one line of body text."""
    background, border, title_colour, body_colour, icon, mark, glyph = NOTICE_TONES[tone]
    return Group(name, [
        Rect("Background", x, y, w, 72, background, 1, 10, border),
        Circle("Icon", x + 32, y + 36, 12, icon),
        text("Icon mark", x + 20, y + 42, glyph, 16, 800, mark, 24, "center", icon=True),
        text("Title", x + 60, y + 31, title, 14, 600, title_colour),
        text("Body", x + 60, y + 53, body, 13, 400, body_colour),
    ])


def public_screen(name: str, x: float, y: float, content: list[Item]) -> Board:
    """A signed-out page: the logo centred above a 480 px column, without the app shell (ADR-0019).

    `content` is placed from `PUBLIC_X` and 200 px down, at most `PUBLIC_W` wide.
    """
    logo_x = (BOARD_W - 184) / 2
    logo = Group("Logo", [Rect("Tile", logo_x, 120, 32, 32, P["Brand/logo tile"], 1, 8),
                          Rect("Mark", logo_x + 8, 128, 16, 16, ACCENT, 1, 4),
                          text("Raid", logo_x + 42, 142, "RAID", 16, 800, TEXT, None, "left", 0.5),
                          text("Manager", logo_x + 84, 142, "MANAGER", 16, 800, ACCENT, None, "left", 0.5)])
    return Board(name, x, y, BOARD_W, BOARD_H, PAGE_BACKGROUND, [logo, *content])
