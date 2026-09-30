"""Draft compositions and the bench (story #30, design story #190, task #235).

Three boards in RaidManager's design system (ADR-0019), seen by an officer, linked as a clickable prototype. The
players come from `roster_data.py`, shared with the other roster mockups:

1. Draft A: five groups of five (`RosterSelection` group and position), two placeholder places, and the bench.
   The draft tabs include Draft B, copied from the raid on Fri 25 Sep. Drafts stay unpublished until an officer
   chooses one. Selecting Shadestep opens 2; the Draft B tab opens 3.
2. Swapping: Shadestep and the bench's Kirilight are selected for a swap, a placeholder can be filled, and adding
   Jainaice is refused because Bryn Valewood is already in the draft with Arthasdk. Swap and Cancel open 1.
3. Draft B's balance: players per role, and classes with their specs. The Draft A tab opens 1.

Run from the repository root: python scripts/mockups/roster_composer.py
Once the owner edits the design in Penpot, the downloaded file replaces docs/mockups/roster-composer.penpot and this
script is no longer run.
"""

from __future__ import annotations

import sys
from collections import Counter
from pathlib import Path

REPOSITORY = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(REPOSITORY / "scripts"))

from penpot_components import (  # noqa: E402
    BOARD_GAP, BOARD_H, BOARD_W, CONTENT_TOP, CONTENT_W, CONTENT_X, app_screen, button, card, notice, page_header,
)
from penpot_scene import HOUSE_PALETTE, Board, Circle, Click, Group, Item, Rect, text, write_mockup  # noqa: E402
from roster_data import BENCH, CLASS_COLOURS, DRAFT_A, PALETTE_ADDITIONS, WHEN, Slot  # noqa: E402

P = HOUSE_PALETTE
PALETTE = {**HOUSE_PALETTE, **PALETTE_ADDITIONS}
ACCENT, SECONDARY, MUTED, DIVIDER = P["Brand/accent"], P["Text/secondary"], P["Text/muted"], P["Line/divider"]
RAISED, SELECTED, TEXT = P["Surface/raised"], P["Surface/selected"], P["Text/primary"]
PAGE = "Roster builder"
SECTION = "Drafts"
TITLE = "Icecrown Citadel drafts"
DRAFT_A_TAB = "Draft A"
SWAP_FROM, SWAP_TO = "Shadestep", "Kirilight"
ROLE_SHORT = {"Tank": "Tank", "Healer": "Heal", "Melee damage": "Melee", "Ranged damage": "Ranged"}

DRAFT = "1 · Draft A"
SWAP = "2 · Swap and placeholders"
BALANCE = "3 · Draft B balance"

TOP = CONTENT_TOP + 120
GROUP_W, GROUP_GAP, SLOT_H = 170, 12, 48
BENCH_X = CONTENT_X + 5 * GROUP_W + 4 * GROUP_GAP + 20
BENCH_W = CONTENT_X + CONTENT_W - BENCH_X


def tabs(active: str, linked: bool) -> Group:
    names = [(DRAFT_A_TAB, DRAFT), ("Draft B · copied from Fri 25 Sep", BALANCE)]
    items: list[Item] = [Rect("Divider", CONTENT_X, TOP + 36, CONTENT_W, 1, DIVIDER)]
    x: float = CONTENT_X
    for name, target in names:
        width = len(name) * 7.4 + 32
        selected = name == active
        tab: list[Item] = [Rect("Area", x, TOP, width, 36, P["Surface/page"]),
                           text("Label", x, TOP + 22, name, 14, 600 if selected else 400, ACCENT if selected else SECONDARY,
                                width, "center")]
        if selected:
            tab.append(Rect("Underline", x, TOP + 34, width, 3, ACCENT, 1, 2))
        items.append(Group(f"{name} tab", tab, Click("navigate", target) if linked and not selected else None))
        x += width + 8
    items += [text("New draft", x + 16, TOP + 22, "+ New draft", 13, 600, ACCENT),
              text("Copy raid", x + 130, TOP + 22, "Copy a past raid", 13, 600, ACCENT),
              text("Unpublished", CONTENT_X + CONTENT_W - 300, TOP + 22, "Drafts stay unpublished until you choose one.",
                   12, 400, MUTED, 300, "right")]
    return Group("Draft tabs", items)


def slot_card(name: str, x: float, y: float, slot: Slot, state: str = "", on_click: Click | None = None) -> Group:
    """A roster place: character with class colour, spec and role; a placeholder shows only its role."""
    width = GROUP_W - 16
    if not slot.character:
        return Group(name, [Rect("Place", x, y, width, SLOT_H - 6, P["Surface/card"], 1, 6, DIVIDER),
                            text("Placeholder", x + 12, y + 18, f"{slot.role} placeholder", 12, 600, SECONDARY),
                            text("Fill", x + 12, y + 35, "Fill from candidates", 11, 400, ACCENT)], on_click)
    border = ACCENT if state == "selected" else None
    fill = SELECTED if state == "selected" else RAISED
    return Group(name, [
        Rect("Place", x, y, width, SLOT_H - 6, fill, 1, 6, border),
        Circle("Class colour", x + 12, y + 14, 4, CLASS_COLOURS[slot.wow_class]),
        text("Character", x + 22, y + 18, slot.character, 13, 600, TEXT),
        text("Spec", x + 12, y + 35, f"{slot.spec} · {ROLE_SHORT[slot.role]}", 11, 400,
             SECONDARY if state == "selected" else MUTED),
    ], on_click)


def groups(linked: bool, selected: str = "") -> Group:
    items: list[Item] = []
    top = TOP + 56
    for index, members in enumerate(DRAFT_A):
        x = CONTENT_X + index * (GROUP_W + GROUP_GAP)
        group: list[Item] = [*card(x, top, GROUP_W, 40 + SLOT_H * len(members) + 4),
                             text("Title", x + 12, top + 26, f"Group {index + 1}", 13, 700, SECONDARY)]
        for position, slot in enumerate(members):
            state = "selected" if slot.character == selected else ""
            click = Click("navigate", SWAP) if linked and slot.character == SWAP_FROM else None
            group.append(slot_card(f"Place {position + 1}", x + 8, top + 40 + position * SLOT_H, slot, state, click))
        items.append(Group(f"Group {index + 1}", group))
    return Group("Groups", items)


def bench(selected: str = "") -> Group:
    top = TOP + 56
    items: list[Item] = [*card(BENCH_X, top, BENCH_W, 40 + SLOT_H * len(BENCH) + 64),
                         text("Title", BENCH_X + 12, top + 26, f"Bench · {len(BENCH)}", 13, 700, SECONDARY)]
    for index, slot in enumerate(BENCH):
        items.append(slot_card(f"Bench place {index + 1}", BENCH_X + 8, top + 40 + index * SLOT_H, slot,
                               "selected" if slot.character == selected else ""))
    items.append(text("Bench note", BENCH_X + 12, top + 40 + SLOT_H * len(BENCH) + 22, "Benched players are told", 11, 400,
                      MUTED))
    items.append(text("Bench note 2", BENCH_X + 12, top + 40 + SLOT_H * len(BENCH) + 38, "when you publish.", 11, 400, MUTED))
    return Group("Bench", items)


def summary_line() -> Item:
    return text("Summary", CONTENT_X, TOP + 56 + 40 + SLOT_H * 5 + 32,
                "23 players and 2 placeholders · one character per player · 25 places", 13, 400, SECONDARY)


def draft() -> list[Item]:
    return [page_header(PAGE, TITLE, WHEN), tabs(DRAFT_A_TAB, True), groups(True), bench(),
            summary_line(),
            button("Publish button", CONTENT_X + CONTENT_W - 184, CONTENT_TOP + 28, "Review and publish", "primary", 184)]


def swap() -> list[Item]:
    bar_y = TOP + 56 + 40 + SLOT_H * 5 + 20
    return [
        page_header(PAGE, TITLE, WHEN), tabs(DRAFT_A_TAB, False),
        groups(False, SWAP_FROM), bench(SWAP_TO),
        Group("Swap bar", [
            *card(CONTENT_X, bar_y, CONTENT_W, 64, ACCENT),
            text("Swap", CONTENT_X + 24, bar_y + 38,
                 f"Swap {SWAP_FROM} (Selm Voss, group 1) with {SWAP_TO} (Kiri Dawn, bench)?", 14, 600),
            button("Cancel button", CONTENT_X + CONTENT_W - 24 - 96 - 8 - 96, bar_y + 12, "Cancel", "secondary", 96,
                   Click("navigate", DRAFT)),
            button("Swap button", CONTENT_X + CONTENT_W - 24 - 96, bar_y + 12, "Swap", "primary", 96,
                   Click("navigate", DRAFT)),
        ]),
        notice("Refused notice", CONTENT_X, bar_y + 80, CONTENT_W, "Jainaice can't join this draft",
               "Bryn Valewood is already in it with Arthasdk. A draft takes one character per player; swap them instead.",
               "warning"),
    ]


def balance() -> list[Item]:
    slots = [slot for members in DRAFT_A for slot in members if slot.character]
    roles = Counter(slot.role for slot in slots)
    placeholders = Counter(slot.role for members in DRAFT_A for slot in members if not slot.character)
    classes: dict[str, Counter[str]] = {}
    for slot in slots:
        classes.setdefault(slot.wow_class, Counter())[slot.spec] += 1
    top = TOP + 56
    width = (CONTENT_W - 3 * 16) / 4
    items: list[Item] = []
    for index, role in enumerate(ROLE_SHORT):
        x = CONTENT_X + index * (width + 16)
        extra = f" + {placeholders[role]} placeholder" if placeholders[role] else ""
        items.append(Group(f"{role} tile", [
            *card(x, top, width, 84),
            text("Label", x + 20, top + 28, role.upper(), 11, 700, MUTED, None, "left", 1.2),
            text("Count", x + 20, top + 60, str(roles[role]), 28, 700),
            text("Extra", x + 64, top + 58, extra, 12, 400, SECONDARY)]))
    class_top = top + 104
    rows = sorted(classes.items(), key=lambda item: (-sum(item[1].values()), item[0]))
    column_w = CONTENT_W / 2
    classes_card: list[Item] = [*card(CONTENT_X, class_top, CONTENT_W, 60 + 34 * ((len(rows) + 1) // 2) + 8),
                                text("Title", CONTENT_X + 20, class_top + 36, "Classes and specs", 16, 600)]
    for index, (wow_class, specs) in enumerate(rows):
        x = CONTENT_X + 20 + (index % 2) * column_w
        y = class_top + 60 + (index // 2) * 34
        detail = ", ".join(f"{spec} {count}" for spec, count in sorted(specs.items()))
        classes_card.append(Group(f"{wow_class} row", [
            Circle("Class colour", x + 5, y + 12, 5, CLASS_COLOURS[wow_class]),
            text("Class", x + 18, y + 17, f"{wow_class} {sum(specs.values())}", 14, 600),
            text("Specs", x + 150, y + 17, detail, 13, 400, SECONDARY),
        ]))
    items.append(Group("Classes card", classes_card))
    return [page_header(PAGE, TITLE, WHEN), tabs("Draft B · copied from Fri 25 Sep", True),
            Group("Balance", items),
            text("Copied note", CONTENT_X, BOARD_H - 40,
                 "Copied from the raid on Fri 25 Sep; readiness is checked again for this raid.", 12, 400, MUTED)]




def boards() -> list[Board]:
    right, below = BOARD_W + BOARD_GAP, BOARD_H + BOARD_GAP

    def screen(name: str, x: float, y: float, content: list[Item]) -> Board:
        return app_screen(name, x, y, PAGE, content, section=SECTION)

    return [screen(DRAFT, 0, 0, draft()), screen(SWAP, right, 0, swap()), screen(BALANCE, 0, below, balance())]


def main(repository: Path = REPOSITORY) -> Path:
    return write_mockup(repository, "roster-composer", "Roster composer", boards(), PALETTE,
                        {"Swap a participant": DRAFT, "Draft balance": BALANCE})


if __name__ == "__main__":
    print("Wrote", main())
