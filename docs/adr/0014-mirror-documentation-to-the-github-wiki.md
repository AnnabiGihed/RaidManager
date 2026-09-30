# ADR-0014: Mirror the documentation to the GitHub Wiki

- Status: Proposed
- Date: 2026-09-30
- Deciders: Gihed Annabi

## Context

[ADR-0003](0003-publish-documentation-through-github-pages.md) publishes `docs/` as a GitHub Pages site. The
repository's Wiki tab is enabled but empty, so readers who open it find no documentation. A hand-written wiki would
drift from the code, and the documentation rules forbid a second source.

## Decision

The GitHub Wiki is a generated mirror of `docs/`, next to the GitHub Pages site.

- `docs/` stays the only source. `scripts/build_wiki.py` turns each document into a wiki page named after its title:
  - it rewrites links between documents to wiki pages, and links outside `docs/` to the repository on GitHub;
  - it copies the diagrams the pages use;
  - it builds the sidebar from the `mkdocs.yml` navigation, which must list every document;
  - it notes on each page that the page is generated, and which file to edit.
- Every pull request builds the wiki in the docs check. A link to a missing file, two documents with the same wiki
  page name, or a document missing from the navigation fails the check.
- After each merge to `main`, `docs-publish` pushes the generated pages to the wiki repository with the built-in
  `GITHUB_TOKEN`. No personal token or secret is involved, and the job pushes nothing when the pages didn't change.
- Edits made in the wiki are overwritten by the next publication. Wiki editing is restricted to collaborators.

## Consequences

**Positive**

- The Wiki tab and the Pages site always show the same documentation as `main`.
- Broken links and navigation gaps fail on the pull request that causes them, for both targets.

**Negative**

- GitHub creates the wiki repository only when its first page is saved in the browser, so the owner does that once.
- The wiki renders plain GitHub Markdown, so documents keep to features both targets support.

## Alternatives considered

- **Keep the Wiki tab disabled:** no drift, but readers who look for a wiki find nothing.
- **Write the wiki by hand:** it would drift from `docs/` and break the single-source rule.
- **A wiki-sync action from the marketplace:** it does the same copy but can't rewrite links or check them on pull
  requests, and adds a third-party dependency.
