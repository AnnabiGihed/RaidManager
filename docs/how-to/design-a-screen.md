# Design a screen in Penpot

Every change to what users see starts from a mockup
([ADR-0017](../adr/0017-penpot-mockups-for-ui-work.md)). This covers website pages, companion windows, WoW addon
frames and Discord messages. The design is made in [Penpot](https://penpot.app), and its source and image are kept in
`docs/mockups/`.

## One-time setup

1. Create a free account at [penpot.app](https://penpot.app) (the Professional plan is free for up to eight members),
   or use a self-hosted Penpot.
2. Create a team and a project named **RaidManager**, and invite the people who design or review screens.
3. Keep one Penpot file per screen, named like its future mockup file, for example `character-review`.
4. Use the Radzen material look, which is the website's theme
   ([ADR-0012](../adr/0012-interactive-server-rendering-with-radzen.md)): its colors, spacing and components.

## Design and share

1. Draw each state of the screen as a board, such as empty, filled, error and loading, side by side in one file.
2. While the design is in progress, open **View mode**, choose **Share**, and copy the link into the issue's
   **Mockup** field. The issue's `needs-mockup` label goes away within 15 minutes, or at once when you edit the issue.
3. When the design is agreed, export it:
   - Select every board of the screen. In **Export** on the right, choose **SVG**, and save the export as
     `docs/mockups/<screen>.svg`. Use lowercase words with hyphens for `<screen>`.
   - From the main menu, choose **Download Penpot file (.penpot)** and save it as `docs/mockups/<screen>.penpot`.
4. Commit both files together in the pull request of the task that implements the screen, or in a design task of
   its own. The docs check fails when one of the pair is missing.
5. Replace the Penpot link in the issue with the committed image, for example
   `![Character review](https://github.com/AnnabiGihed/RaidManager/blob/main/docs/mockups/character-review.svg)`.

## Implement and review

- Show the mockup in the pull request that changes the screen, as an image or a link to `docs/mockups/<screen>.svg`.
  The docs `validate` check fails if a pull request changes user-interface files without one.
- A pull request that changes UI code without changing what users see, such as a refactor or a renamed field, states
  it on its own line: `No visual change: <reason>`.
- If the implementation must differ from the design, update the Penpot file and both exports in the same pull
  request, so the mockup stays the truth.
- To change an existing screen, import `docs/mockups/<screen>.penpot` into Penpot (**Import Penpot files** on the
  dashboard), edit it, and export both files again.

## Which items need a mockup

Any epic, feature, story, improvement, bug, task or spike that changes what users see. Answer **Yes** to the
**User interface** question in the issue form, or add the `ui` label. The item must then link or show its mockup,
and the [hierarchy workflow](../reference/project-automation.md) labels it `needs-mockup` until it does. An epic or
feature can show the mockups of its stories in a short list.
