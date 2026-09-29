# ADR-0003: Publish documentation through GitHub Pages

- Status: Accepted
- Date: 2026-09-29
- Deciders: Gihed Annabi

## Context

RaidManager has a MkDocs site and documentation rules, but its workflow only lints Markdown and checks some links.
The project needs a predictable public documentation URL and automatic publication after a successful merge.
The repository is public, and the owner approved public GitHub Pages publication.

## Decision

We validate the root documents, Markdown, prose, internal links, diagrams, and a strict MkDocs build on each pull
request. We publish the same built site to GitHub Pages after checks pass on `main`. We keep project-authored Vale
rules in this repository because the imported organizational style repository is unavailable to RaidManager.
OpenAPI validation runs when a committed specification exists. Reviewers retain responsibility for semantic accuracy
and matching documentation to behavior changes.

## Consequences

**Positive**

- Readers get an automatically updated documentation site at a stable project URL.
- Pull requests fail on mechanical documentation defects before merge.

**Negative**

- Published documentation is public; sensitive operational details cannot be placed in the site source.
- GitHub Pages, MkDocs, and documentation linter availability become release-pipeline dependencies.

## Alternatives considered

- **Manual publishing:** rejected because it can drift from `main` and cannot satisfy the automatic publication rule.
- **Separate hosted documentation service:** rejected because it adds credentials and infrastructure before the first
  product release.
