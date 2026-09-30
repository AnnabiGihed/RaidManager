# ADR-0017: Penpot mockups for all user-interface work

- Status: Proposed
- Date: 2026-09-30
- Deciders: Gihed Annabi

## Context

RaidManager has several user interfaces:

- website pages;
- a desktop companion;
- WoW addon frames;
- Discord messages and interactions.

Screens were built straight from story text, so reviewers had nothing to compare them with, and designers had no
place to work. The owner asked for a design tool that professional UI designers know. Its files must be free,
versioned and viewable on GitHub.

Figma is the industry standard, but its free plan allows three design files and two editors per file and keeps 30
days of history. Its files live only in Figma's cloud. Penpot is open source, works like Figma and imports Figma
files. Its full-featured plan is free for up to eight members, and it can be hosted on your own server. Its designs
export as `.penpot` files and each board exports as SVG. Neither tool's source file renders on GitHub, so the
rendered image has to be an export.

## Decision

Penpot is RaidManager's UI design tool, and a mockup is required for every user-interface change:

- Each screen, or group of related states, is a Penpot board. Its `.penpot` source and its SVG export are committed
  together as `docs/mockups/<screen>.penpot` and `docs/mockups/<screen>.svg`. GitHub, the documentation site and the
  wiki show the SVG.
- Every epic, feature, story, improvement, bug, task or spike that changes what users see carries the `ui` label and
  links or shows its mockup: the committed SVG, or a Penpot share link while the design is in progress. The issue
  forms ask the question and the hierarchy workflow adds the label. It flags an item without a mockup with
  `needs-mockup`.
- A pull request that changes user-interface files shows the mockup it implements, or states
  `No visual change:` with a reason. The docs `validate` check enforces it.
- The docs check requires every mockup SVG to have its `.penpot` source, and the other way round.
- The mockup is the agreed design. The implementation follows it, and a deliberate difference updates the mockup in
  the same pull request.

## Consequences

**Positive**

- Designers work in a familiar, professional tool, and nothing costs money.
- Reviewers compare the change with an image in the pull request, and the board shows which UI work still lacks a
  design.

**Negative**

- `.penpot` files are binary, so pull requests can't show line-by-line differences; reviewers compare the SVG
  exports instead.
- Exporting and committing both files is a manual step for the designer.
- Existing UI stories start with the `needs-mockup` label until their screens are designed.

## Alternatives considered

- **Figma:** the most common tool, but its free plan's limits and cloud-only files don't meet the versioning and
  cost requirements.
- **draw.io or Excalidraw SVG files:** they render on GitHub and embed their source, but designers rarely use them
  for interface design.
- **HTML mockups with the Radzen theme:** closest to the final look, but GitHub doesn't render HTML, and a
  developer has to build them.
