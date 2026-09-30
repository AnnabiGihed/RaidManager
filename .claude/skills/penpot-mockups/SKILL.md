---
name: penpot-mockups
description: >-
  Generate RaidManager UI mockups as native Penpot files and render their SVGs: write a screen script under
  scripts/mockups/, produce docs/mockups/<screen>.penpot with editable text layers, render docs/mockups/<screen>.svg
  from it, commit both, and link the SVG in the user story. Use whenever a story, task or pull request needs a
  mockup, a screen is designed or changed, or a .penpot or mockup SVG file is created, edited, reviewed or moved.
---

# Penpot mockups

Every screen users see is designed before it is built (ADR-0017), as a Penpot file generated in the repository and
an SVG rendered from it (ADR-0018). Read `raidmanager-conventions` §10 first.

## Rules

- **Only in `docs/mockups/`.** A `.penpot` file and its SVG live in `docs/mockups/`, named `<screen>.penpot` and
  `<screen>.svg` with the same lowercase, hyphenated name. Never write one to the repository root, `src/`, a scratch
  folder you then commit, or anywhere else; `validate_docs.py` fails on any `.penpot` outside `docs/mockups/` and any
  `.svg` outside `docs/` or a web project's `wwwroot/`. Work files you don't commit go in your scratchpad.
- **The SVG is rendered, never drawn or exported by hand.** Run `python scripts/penpot_render.py
  docs/mockups/<screen>.penpot`. The docs check fails when an SVG isn't the current rendering of its `.penpot`.
- **The `.penpot` is the source of truth.** A screen script under `scripts/mockups/<screen_name>.py` generates the
  first version. Once the owner edits the design in Penpot and downloads it, the downloaded file replaces the
  generated one, and the script is no longer run for that screen; say so in the script's docstring.
- **Link the mockup where the work is tracked.** The user story (or improvement or bug), its UI task, and the pull
  request show the committed SVG:
  `![<Screen> mockup](https://github.com/AnnabiGihed/RaidManager/blob/main/docs/mockups/<screen>.svg)`. That clears
  the `needs-mockup` label (ADR-0017).
- **Only real data and the house look.** Show only fields the API or domain provides, with realistic values. Use the
  Radzen material look of the website (ADR-0012): primary `#4340D2`, page `#F5F5F5`, cards `#FFFFFF` with `#E0E0E0`
  borders, text `#424242` and `#616161`, warning `#FF9800`, danger `#F44336`, info `#2196F3`, success `#4CAF50`,
  radius 4, Roboto, uppercase button labels with 0.5 letter spacing.
- **One board per state.** Draw each state the screen can be in: filled, empty, loading when it matters, error,
  confirmation dialogs and notifications. Name boards `N · <state>`.
- **Name every layer and group.** Buttons, badges, rows and dialogs are named groups ("Approve button",
  "Row Arthasdk"). Sibling layers need distinct names; the generator stops otherwise.
- **Never design without the owner's decision.** Open product questions (what happens after a decision, who acts
  next) go on the task as questions; don't invent the answer in the design.

## Workflow

1. Confirm the item is labelled `ui` and has a design task under its story, improvement or bug.
2. Read the API contract and the story's criteria; list the states the screen must show.
3. Write `scripts/mockups/<screen_name>.py` with the scene model from `scripts/penpot_scene.py`:
   - `Board(name, x, y, width, height, fill, children)`, placed side by side with an 80 px gap;
   - `Rect`, `Circle`, `Group`, and `text(name, x, baseline, value, size, weight, color, width, align, spacing)`;
   - finish with `write_mockup(repository_root, "<screen>", "<Page name>", boards)`, which writes
     `docs/mockups/<screen>.penpot` and renders `docs/mockups/<screen>.svg`.
4. Run `python scripts/mockups/<screen_name>.py`. Open the SVG in the browser and fix overlaps, clipped or
   overflowing text, and alignment before anyone else sees it.
5. Hand the `.penpot` to the owner to import in Penpot (**Import Penpot files** on the dashboard) and check it: every
   text must be an editable text layer. If `PENPOT_ACCESS_TOKEN` is set as a user environment variable, you may also
   validate it through Penpot's `import-binfile` API; never print or store the token.
6. When the owner edits the design and downloads the `.penpot`, save it over `docs/mockups/<screen>.penpot` and run
   `python scripts/penpot_render.py docs/mockups/<screen>.penpot`.
7. Commit the `.penpot`, its SVG and the screen script in the design task's pull request, show the SVG in the pull
   request description, and link it in the story and task bodies.

## Limits

- The generator writes one-line text layers, rectangles, circles, groups and boards. For icons, draw them from
  circles and text, or leave them to the designer in Penpot.
- The renderer draws boards, groups, rectangles, circles and text. Paths, booleans, images and raw SVG become
  labelled placeholders with a warning; the Penpot file still holds them.
- Generated text boxes have estimated widths; Penpot measures the real text when the file opens.
- The format is pinned to Penpot `PENPOT_VERSION` in `scripts/penpot_scene.py` (2.18.0). When penpot.app upgrades
  and an import fails, update `FILE_VERSION`, `FEATURES` and `MIGRATIONS` from that release's source
  (`common/src/app/common/files/defaults.cljc`, `features.cljc`, `files/migrations.cljc`).

## Completion criteria

- `docs/mockups/<screen>.penpot` and `docs/mockups/<screen>.svg` exist, and nothing else was written elsewhere.
- `python scripts/validate_docs.py` passes, so the SVG is the current rendering.
- Every state of the screen has a board; every layer and group has a meaningful name; only real fields are shown.
- The owner imported the `.penpot` and confirmed it, and the story, task and pull request show the SVG.
