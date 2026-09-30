# Design a screen in Penpot

Every change to what users see starts from a mockup
([ADR-0017](../adr/0017-penpot-mockups-for-ui-work.md)). This covers website pages, companion windows, WoW addon
frames and Discord messages. The design is made in [Penpot](https://penpot.app). Its source, `<screen>.penpot`, and
the SVG rendered from it, `<screen>.svg`, are kept together in `docs/mockups/` and nowhere else
([ADR-0018](../adr/0018-generate-penpot-mockups-in-the-repository.md)).

## One-time setup

1. Create a free account at [penpot.app](https://penpot.app) (the Professional plan is free for up to eight members),
   or use a self-hosted Penpot.
2. Create a team and a project named **RaidManager**, and invite the people who design or review screens.
3. Keep one Penpot file per screen, named like its future mockup file, for example `character-review`.
4. Use the Radzen material look, which is the website's theme
   ([ADR-0012](../adr/0012-interactive-server-rendering-with-radzen.md)): its colors, spacing and components.

## Generate the first version

A screen's first version is generated in the repository, so it arrives in Penpot as a small design system:

- every text is an editable text layer;
- every layer and group has a function-based name;
- the website's colours and text styles are shared library assets that the layers use;
- the buttons are linked into a clickable prototype.

1. Write `scripts/mockups/<screen_name>.py` with the scene model in `scripts/penpot_scene.py`: one board per state
   (filled, empty, error, dialogs), made of named groups, rectangles, circles and text. Take colours from the house
   palette and link each control to the board it leads to.
2. Run `python scripts/mockups/<screen_name>.py`. It checks that every colour is a named palette colour and that text
   meets WCAG AA contrast, then writes `docs/mockups/<screen>.penpot` and renders `docs/mockups/<screen>.svg` from it.
   Use lowercase words with hyphens for `<screen>`.
3. Import the `.penpot` file in Penpot (**Import Penpot files** on the dashboard) and check it: texts are editable,
   the Assets panel lists the colours and typographies, and View mode plays each flow.

## Design in Penpot

1. Adjust the design in Penpot. Draw each state as a board, side by side in one file.
2. While the design is in progress, open **View mode**, choose **Share**, and copy the link into the issue's
   **Mockup** field. The issue's `needs-mockup` label goes away within 15 minutes, or at once when you edit the issue.
3. When the design is agreed, choose **Download Penpot file (.penpot)** from the main menu and save it over
   `docs/mockups/<screen>.penpot`. From then on, the downloaded file is the source; don't rerun the screen script.
4. Render its SVG: `python scripts/penpot_render.py docs/mockups/<screen>.penpot`. Never export or edit the SVG by
   hand; the docs check fails when the SVG isn't the current rendering of its `.penpot` file. The renderer draws
   boards, groups, rectangles, circles and text, and shows anything else as a labelled placeholder.
5. Commit both files together in the pull request of the design task or of the task that implements the screen.
   Never leave a `.penpot` or SVG in the repository root or any other folder; the docs check fails on it.
6. Show the committed image in the user story, its UI task and the pull request, for example
   `![Character review](https://github.com/AnnabiGihed/RaidManager/blob/main/docs/mockups/character-review.svg)`.

## Implement and review

- Show the mockup in the pull request that changes the screen, as an image or a link to `docs/mockups/<screen>.svg`.
  The docs `validate` check fails if a pull request changes user-interface files without one.
- A pull request that changes UI code without changing what users see, such as a refactor or a renamed field, states
  it on its own line: `No visual change: <reason>`.
- If the implementation must differ from the design, update the Penpot file and render its SVG again in the same
  pull request, so the mockup stays the truth.
- To change an existing screen, import `docs/mockups/<screen>.penpot` into Penpot (**Import Penpot files** on the
  dashboard), edit it, download it over the old file, and render its SVG again.

## Which items need a mockup

Any epic, feature, story, improvement, bug, task or spike that changes what users see. Answer **Yes** to the
**User interface** question in the issue form, or add the `ui` label. The item must then link or show its mockup,
and the [hierarchy workflow](../reference/project-automation.md) labels it `needs-mockup` until it does. An epic or
feature can show the mockups of its stories in a short list.
