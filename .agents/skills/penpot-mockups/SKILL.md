---
name: penpot-mockups
description: >-
  Generate RaidManager UI mockups as native Penpot files and render their SVGs: write a screen script under
  scripts/mockups/ with the dark design system and app shell, produce docs/mockups/<screen>.penpot with editable
  text, a shared colour and typography library, a clickable prototype and WCAG AA contrast, render docs/mockups/<screen>.svg from it, commit both, and link the SVG
  in the user story. Use whenever a story, task or pull request needs a mockup, a screen is designed or changed, or a
  .penpot or mockup SVG file is created, edited, reviewed or moved.
---

# Penpot mockups

Every screen users see is designed before it is built (ADR-0017), as a Penpot file generated in the repository and
an SVG rendered from it (ADR-0018). Read `raidmanager-conventions` §10 first. The generator writes files Penpot
imports as-is, so no Penpot API, MCP server, plugin or access token is needed.

## Rules

- **Only in `docs/mockups/`.** A `.penpot` file and its SVG live in `docs/mockups/`, named `<screen>.penpot` and
  `<screen>.svg` with the same lowercase, hyphenated name. Never write one to the repository root, `src/`, or any other
  folder you might commit; work files stay in your scratchpad. `validate_docs.py` fails on any `.penpot` outside
  `docs/mockups/` and any `.svg` outside `docs/` or a web project's `wwwroot/`.
- **The SVG is rendered, never drawn or exported by hand.** Run
  `python scripts/penpot_render.py docs/mockups/<screen>.penpot`. The docs check fails when an SVG isn't the current
  rendering of its `.penpot`.
- **The `.penpot` is the source of truth.** A screen script, `scripts/mockups/<screen_name>.py`, generates the first
  version. Once the owner edits the design in Penpot and downloads it, the downloaded file replaces the generated one
  and the script is no longer run; say so in the script's docstring.
- **Link the mockup where the work is tracked.** The user story (or improvement or bug), its UI task, and the pull
  request show the committed SVG:
  `![<Screen> mockup](https://github.com/AnnabiGihed/RaidManager/blob/main/docs/mockups/<screen>.svg)`. Once the SVG
  is on `main`, that clears the
  `needs-mockup` label (ADR-0017).
- **Never decide the product in the design.** Show only fields the API or domain provides, with realistic values.
  When a screen depends on an open question (what happens next, who acts), ask it on the task first.

## Design system in the file

The generator turns the design into a small design system that a designer keeps working with:

- **Colours come only from the palette.** `HOUSE_PALETTE` in `scripts/penpot_scene.py` is RaidManager's dark
  design system (ADR-0019), one name per colour, grouped as `Surface/`, `Line/`, `Brand/`, `Text/`, `Accent/`,
  `Status/`, `Avatar/` and `Community/`. `Brand/accent` (teal) marks the primary action and the active page only.
  Screen-specific colours are added under their own group, for example `{**HOUSE_PALETTE, "WoW class/Mage": "#3FC7EB"}`. The generator refuses a colour the palette doesn't name, and
  refuses two names for one colour. Every palette colour a screen uses becomes a library colour, and layers reference
  it, so the Assets panel restyles the whole screen.
- **Text styles come from the type scale, in Open Sans.** `TYPE_SCALE` names each size, weight and spacing
  (`Heading/Page title` 32/700, `Heading/Section title` 18/700, `Body/Default` 14/400, `Label/Button` 13/700,
  `Label/Eyebrow` 12/700/1.5, `Label/Section` 11/700/1.2, `Caption/Default` 12/400, `Icon/Default` 16/800). Uppercase
  labels are typed in capitals. Each becomes a library typography that text layers reference. A style outside the scale is named `Other/...`; add it to
  the scale instead of leaving it there.
- **Contrast is checked before writing.** Text needs 4.5:1 against the opaque shape behind it (3:1 for text of 24 px,
  or 18.66 px bold); icon glyphs made of text (`icon=True`) need 3:1. The generator stops with the failing layers. Fix
  the colour in the palette (for example `Status/warning text` on `Status/warning background`); never lower the check.
- **Status is never colour alone.** Badges carry a word (`Pending`, `Conflict`), and errors carry an icon and a sentence.
- **The app shell is on every website screen.** Build each board with
  `app_screen(name, x, y, page, content, section=, user=, links=)` from `scripts/penpot_components.py`: a
  1440 x 900 board with the sidebar (logo, community card, Player navigation, Officer navigation for officers only,
  user card) and the top bar (breadcrumb, realm status, avatar, Sign out; `sign_out=` links it). `page` is the active navigation entry; pass
  `user=PLAYER` for a player's view, and `community=None` for a user who has no community yet. `links` also takes
  `"Community"` to make the community card open a board. Content starts at `CONTENT_X`, `CONTENT_TOP` and is `CONTENT_W` wide.
- **Signed-out pages have no shell.** Sign-in and its failure pages use `public_screen(name, x, y, content)`: the
  logo above a centred column at `PUBLIC_X`, `PUBLIC_W` wide (ADR-0019).
- **Use the shared components** instead of drawing your own: `page_header` (teal eyebrow, 32 px title, subtitle),
  `card` (12 px radius, `Line/card border` outline, optional accent bar), `button` (40 px, 8 px radius: `primary`,
  `secondary`, `danger`), `badge` (a word on a tinted pill), `notice` (icon, title, one line) and `avatar`. Add a new
  shared component there when two screens need it.
- **Layout:** 24 px card padding, 16 px between cards, 40 px margins around the content. Keep positions and sizes on
  a 4 px grid (8 px for spacing between blocks). Companion windows, addon frames and Discord messages don't use the
  website shell; they use the palette and type scale. Draw a desktop companion screen with
  `companion_window(name, x, y, content)`: a 480 x 600 window with its title bar, content from `WINDOW_PADDING`.
  Draw the bot's Discord messages with `discord_screen`, `discord_message`, `discord_embed` and `discord_button`, and
  pass `{**HOUSE_PALETTE, **DISCORD_PALETTE}`; use only the listed button styles, whose labels pass WCAG AA.

## Structure and naming

- **One file per screen, one page, one board per state:** filled, empty, loading when it matters, error, confirmation
  dialogs and notifications. Name boards `N · <state>` and place them left to right, then top to bottom, in the order
  a user meets them, 80 px apart.
- **Function-based names, never appearance-based:** `Approve button`, `Row Arthasdk`, `Waiting notice`,
  `Reject dialog`; never `Rectangle 12` or `Blue box`. Inside a group, name the parts by role (`Background`, `Label`,
  `Icon`, `Icon mark`). Sibling layers need distinct names; the generator stops otherwise.
- **Group what a developer builds as one component:** a button, a badge, a table row, a dialog, a notification. Name
  it after the Radzen or Blazor component when there is one. The shell's groups (`Sidebar`, `Top bar`,
  `<Page> link`) keep the names `app_screen` gives them.

## Prototype

- **Link every control that changes the state** to the board it leads to with `Click(action, board_name)` on its
  group: `navigate` for another state, `open-overlay` for a dialog drawn on its own board, `close-overlay` to close it,
  `prev-screen` to go back.
- **Add a flow per journey** with `flows={"<Journey name>": "<first board>"}`, for example
  `{"Review new characters": "1 · Pending claims"}`. The owner then clicks through it in Penpot's View mode.
- The generator refuses a link or flow to a board that doesn't exist.

## Workflow

1. Confirm the item is labelled `ui` and has a design task under its story, improvement, bug or spike, in Delivery
   Stage Functional Analysis (work management specification §22), and that the task passes the active-sprint gate
   (`work-sprint-planning-and-eligibility`).
2. Read the API contract and the story's criteria; list the states the screen shows and the journeys between them.
3. Write `scripts/mockups/<screen_name>.py`:
   - `app_screen(...)` for each state of a website screen (`Board(name, x, y, width, height, fill, children)`
     otherwise), with the shared components from `scripts/penpot_components.py`;
   - `Rect`, `Circle`, `Group(name, children, on_click)`, and
     `text(name, x, baseline, value, size, weight, color, width, align, spacing, icon)`;
   - finish with `write_mockup(repository_root, "<screen>", "<Page name>", boards, palette, flows)`, which checks
     colours and contrast, writes `docs/mockups/<screen>.penpot`, and renders `docs/mockups/<screen>.svg`.
4. Run `python scripts/mockups/<screen_name>.py`. Open the SVG in the browser and fix overlaps, clipped or overflowing
   text and alignment before anyone else sees it.
5. Give the owner the `.penpot` to import in Penpot (**Import Penpot files** on the dashboard) and check: texts are
   editable, the Assets panel lists the colours and typographies, and View mode plays each flow.
6. When the owner edits the design and downloads it, save it over `docs/mockups/<screen>.penpot` and run
   `python scripts/penpot_render.py docs/mockups/<screen>.penpot`.
7. Commit the `.penpot`, its SVG and the screen script in the design task's pull request. Show the SVG in the pull
   request description, and link it in the story and task bodies.

## Limits

- The generator writes one-line text layers, rectangles, circles, groups and boards, with library colours,
  typographies, click interactions and flows. It doesn't write components, variants, flex or grid layout, design
  tokens, paths or images; a designer adds those in Penpot. Draw icons from circles and text marked `icon=True`.
- The renderer draws boards, groups, rectangles, circles and text. Paths, booleans, images and raw SVG become labelled
  placeholders with a warning; the Penpot file still holds them.
- Generated text boxes have estimated widths; Penpot measures the real text when the file opens.
- The format is pinned to Penpot `PENPOT_VERSION` in `scripts/penpot_scene.py` (2.18.0). When penpot.app upgrades and
  an import fails, update `FILE_VERSION`, `FEATURES` and `MIGRATIONS` from that release's source
  (`common/src/app/common/files/defaults.cljc`, `features.cljc`, `files/migrations.cljc`), then import a generated file
  again.
- The Penpot MCP server edits an open file live through a plugin, a few shapes per call. RaidManager doesn't use it for
  generation; a designer may use it inside Penpot, and the downloaded result replaces the generated file as usual.

## Completion criteria

- `docs/mockups/<screen>.penpot` and `docs/mockups/<screen>.svg` exist, and nothing was written anywhere else.
- `python scripts/validate_docs.py` passes, so the SVG is the current rendering.
- Every state has a board, every control that changes state is linked, and each journey has a flow.
- Colours come from the palette, text styles from the type scale, and every layer and group has a function-based name.
- Every website board is an `app_screen` with the right active page and user role, built from the shared components.
- The owner imported the `.penpot` and confirmed it, and the story, task and pull request show the SVG.
