"""RaidManager's app shell and shared components for mockups, drawn with the scene model (ADR-0019).

Every website screen is an `app_screen`: a 1440 x 900 board with the sidebar (logo, community card, the Player and
Officer navigation, the signed-in user), the top bar (breadcrumb, realm status, avatar) and the screen's own content
in the content area. The components (`page_header`, `card`, `button`, `badge`, `avatar`, `notice`) draw the same
parts the same way on every screen. All colours come from `HOUSE_PALETTE`.

    from penpot_components import CONTENT_X, CONTENT_TOP, app_screen, button, page_header
    board = app_screen("1 · Filled", 0, 0, "My characters", [page_header(...), button(...)])
"""

from __future__ import annotations

from dataclasses import dataclass

from penpot_scene import HOUSE_PALETTE, Board, Circle, Click, Group, Item, Rect, text, text_width

P = HOUSE_PALETTE
BOARD_W, BOARD_H = 1440, 900
SIDEBAR_W, TOP_BAR_H = 240, 64
CONTENT_X, CONTENT_TOP = SIDEBAR_W + 40, TOP_BAR_H + 40
CONTENT_W = BOARD_W - CONTENT_X - 40
BOARD_GAP = 80
PLAYER_PAGES = ("Overview", "Raids", "My characters", "Readiness", "Companion & sync")
OFFICER_PAGES = ("Schedule", "Roster builder", "Raid night")
NAV_TOP, NAV_ITEM_H, NAV_ITEM_GAP = 176, 36, 4
# (fill, text) per tone; the text passes WCAG AA on the fill.
BADGE_TONES = {
    "warning": (P["Status/warning background"], P["Status/warning title"]),
    "danger": (P["Status/danger background"], P["Status/danger text"]),
    "success": (P["Status/success background"], P["Brand/accent"]),
    "info": (P["Status/info background"], P["Accent/blue"]),
    "neutral": (P["Surface/raised"], P["Text/secondary"]),
}
# (background, border, title, body, icon, icon mark) per tone.
NOTICE_TONES = {
    "warning": (P["Status/warning background"], P["Status/warning border"], P["Status/warning title"],
                P["Status/warning text"], P["Accent/amber"], P["Brand/on accent"], "!"),
    "danger": (P["Status/danger background"], P["Status/danger border"], P["Status/danger text"],
               P["Status/danger text"], P["Status/danger"], P["Status/on danger"], "!"),
    "info": (P["Status/info background"], P["Status/info border"], P["Text/primary"], P["Text/secondary"],
             P["Accent/blue"], P["Brand/on accent"], "i"),
    "success": (P["Status/success background"], P["Status/success border"], P["Text/primary"], P["Text/secondary"],
                P["Brand/accent"], P["Brand/on accent"], "✓"),
}
AVATAR_TONES = {
    "blue": (P["Avatar/blue"], P["Accent/blue"]),
    "red": (P["Avatar/red"], P["Avatar/red text"]),
    "purple": (P["Avatar/purple"], P["Avatar/purple text"]),
}
# (fill, border, text) per button style.
BUTTON_STYLES = {
    "primary": (P["Brand/accent"], None, P["Brand/on accent"]),
    "secondary": (P["Surface/raised"], P["Line/divider"], P["Text/primary"]),
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
    colour = P["Brand/accent"] if active else P["Text/secondary"]
    items: list[Item] = [Rect("Area", 12, y, SIDEBAR_W - 24, NAV_ITEM_H,
                              P["Surface/selected"] if active else P["Surface/sidebar"], 1, 8)]
    if active:
        items.append(Rect("Active mark", 12, y + 8, 3, NAV_ITEM_H - 16, P["Brand/accent"], 1, 2))
    items += [Circle("Icon", 34, y + NAV_ITEM_H / 2, 4, P["Brand/accent"] if active else P["Text/muted"]),
              text("Label", 50, y + 23, label, 14, 600 if active else 400, colour)]
    return Group(f"{label} link", items, Click("navigate", target) if target else None)


def sidebar(active: str, community: Community, user: User, links: dict[str, str]) -> Group:
    """The left sidebar: logo, community card, navigation by role, and the signed-in user at the bottom."""
    items: list[Item] = [
        Rect("Background", 0, 0, SIDEBAR_W, BOARD_H, P["Surface/sidebar"]),
        Rect("Edge", SIDEBAR_W - 1, 0, 1, BOARD_H, P["Line/divider"]),
        Group("Logo", [Rect("Tile", 20, 16, 32, 32, P["Brand/logo tile"], 1, 8),
                       Rect("Mark", 28, 24, 16, 16, P["Brand/accent"], 1, 4),
                       text("Raid", 62, 38, "RAID", 16, 800, P["Text/primary"], None, "left", 0.5),
                       text("Manager", 104, 38, "MANAGER", 16, 800, P["Brand/accent"], None, "left", 0.5)]),
        Group("Community card", [
            Rect("Background", 16, 68, SIDEBAR_W - 32, 60, P["Surface/card"], 1, 10, P["Line/card border"]),
            Rect("Icon", 28, 82, 32, 32, P["Community/icon"], 1, 8),
            text("Initials", 28, 102, community.initials, 12, 800, P["Surface/page"], 32, "center"),
            text("Name", 70, 95, community.name, 14, 600),
            text("Realm", 70, 114, f"{community.realm} · Community", 12, 400, P["Text/muted"]),
        ]),
    ]
    sections = [("Player", PLAYER_PAGES)] + ([("Officer", OFFICER_PAGES)] if user.officer else [])
    y = NAV_TOP - 12
    for section, pages in sections:
        nav: list[Item] = [text("Heading", 24, y, section.upper(), 11, 700, P["Text/muted"], None, "left", 1.2)]
        y += 12
        for page in pages:
            nav.append(navigation_item(page, y, page == active, links.get(page)))
            y += NAV_ITEM_H + NAV_ITEM_GAP
        items.append(Group(f"{section} navigation", nav))
        y += 28
    items.append(Group("User card", [
        Rect("Divider", 0, BOARD_H - 72, SIDEBAR_W - 1, 1, P["Line/divider"]),
        avatar("Avatar", 36, BOARD_H - 36, user.initials, user.tone),
        text("Name", 62, BOARD_H - 40, user.name, 14, 600),
        text("Role", 62, BOARD_H - 21, user.role, 12, 400, P["Text/muted"]),
    ]))
    return Group("Sidebar", items)


def top_bar(community: Community, page: str, user: User) -> Group:
    """The top bar: breadcrumb (community / page), realm status and the user's avatar."""
    width = BOARD_W - SIDEBAR_W
    crumb = text("Community", CONTENT_X, 37, community.name.upper(), 11, 700, P["Text/muted"], None, "left", 1.2)
    slash = text("Separator", CONTENT_X + text_width(crumb) + 4, 37, "/", 11, 700, P["Text/muted"], None, "left", 1.2)
    current = text("Page", slash.x + 16, 37, page.upper(), 11, 700, P["Text/primary"], None, "left", 1.2)
    status_x = BOARD_W - 196
    return Group("Top bar", [
        Rect("Background", SIDEBAR_W, 0, width, TOP_BAR_H, P["Surface/top bar"]),
        Rect("Divider", SIDEBAR_W, TOP_BAR_H - 1, width, 1, P["Line/divider"]),
        Group("Breadcrumb", [crumb, slash, current]),
        Group("Realm status", [Circle("Dot", status_x, 32, 4, P["Brand/accent"]),
                               text("Label", status_x + 12, 37, "REALM ONLINE", 11, 700, P["Brand/accent"], None,
                                    "left", 1.2)]),
        avatar("Avatar", BOARD_W - 56, 32, user.initials, user.tone),
    ])


def app_screen(name: str, x: float, y: float, page: str, content: list[Item], *, section: str | None = None,
               community: Community = Community(), user: User = OFFICER,
               links: dict[str, str] | None = None) -> Board:
    """A website screen: the app shell with `page` active in the navigation, and `content` in the content area.

    `section` names the page in the breadcrumb when it differs from the navigation entry; `links` maps navigation
    entries to boards, for the prototype.
    """
    shell: list[Item] = [sidebar(page, community, user, links or {}), top_bar(community, section or page, user)]
    return Board(name, x, y, BOARD_W, BOARD_H, P["Surface/page"], shell + content)


def page_header(eyebrow: str, title: str, subtitle: str, x: float = CONTENT_X, y: float = CONTENT_TOP) -> Group:
    """The eyebrow (teal, uppercase), the page title and one line of subtitle; 96 px tall."""
    return Group("Page header", [
        text("Eyebrow", x, y + 14, eyebrow.upper(), 12, 700, P["Brand/accent"], None, "left", 1.5),
        text("Title", x, y + 56, title, 32, 700),
        text("Subtitle", x, y + 86, subtitle, 15, 400, P["Text/secondary"]),
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
