# ADR-0018: Generate Penpot mockups and render their SVGs in the repository

- Status: Proposed
- Date: 2026-09-30
- Deciders: Gihed Annabi

## Context

[ADR-0017](0017-penpot-mockups-for-ui-work.md) made a Penpot mockup mandatory for all user-interface work, with a
designer drawing each screen in Penpot and exporting its SVG by hand. The first mockup showed three problems:

- An SVG drafted outside Penpot loses its text on import: Penpot turns SVG text into vector outlines.
- Exporting the SVG by hand lets it drift from the `.penpot` file.
- Mockup files were saved in the repository root while they were being made.

Penpot's `.penpot` format (binfile v3, since Penpot 1.15) is a zip of JSON documents: a manifest, the file, its
pages and one document per shape. It is validated on import against Penpot's own schemas. A file generated from
Penpot 2.18.0's source imported cleanly, with every text as an editable text layer.

## Decision

- **Generate the first version in the repository.** `scripts/penpot_scene.py` turns a screen script,
  `scripts/mockups/<screen_name>.py`, into `docs/mockups/<screen>.penpot`: named boards, groups, rectangles, circles
  and editable Roboto text layers. The format is pinned to Penpot 2.18.0 (file version 67, its persisted features and
  its 82 data migrations) and must be updated when penpot.app upgrades.
- **Render the SVG from the `.penpot` file.** `scripts/penpot_render.py` draws boards, groups, rectangles, circles and
  text from the file's own data, and marks anything else as a labelled placeholder. The SVG is never drawn or
  exported by hand. The docs check fails when an SVG isn't the current rendering of its `.penpot` file.
- **The `.penpot` file stays the source of truth.** The owner imports it in Penpot and confirms it. After an edit in
  Penpot, the downloaded file replaces the generated one, and the SVG is rendered again.
- **Mockups live only in `docs/mockups/`.** The docs check fails on a `.penpot` file anywhere else, and on an SVG
  outside `docs/` or a web project's `wwwroot/`, for example in the repository root.
- **The SVG is used where the work is tracked:** in the user story, improvement or bug, its UI task and the pull
  request.
- The `penpot-mockups` skill holds the workflow and the house look; `raidmanager-conventions` §10 holds the rules.

## Consequences

**Positive**

- A screen is drafted in minutes as an editable Penpot design, and designers take over in their usual tool.
- Every committed SVG provably matches its Penpot file, and review stays in the pull request.

**Negative**

- The generated format follows Penpot's internal file layout. A Penpot upgrade can require updating the pinned
  version, features and migration list.
- The renderer draws a subset of Penpot: paths, booleans, images and raw SVG appear as placeholders, and text is
  laid out approximately. Penpot itself stays the reference for the final look.

## Alternatives considered

- **Import a drafted SVG into Penpot:** shapes become layers, but text becomes outlines that can't be edited.
- **A Penpot plugin that draws the design inside Penpot:** keeps text editable, but needs a plugin to be installed
  and run by hand for each screen, and leaves no generated file to review.
- **Export the SVG from Penpot by hand:** the most faithful rendering, but it can't be checked against the
  `.penpot` file and drifts silently.
- **Penpot's import and export API with an access token:** validates each file on the server and returns Penpot's
  normalised file. It stays an optional check in the skill, because it needs a personal token on the machine.
