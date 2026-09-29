# Contributing

## Work items

Follow the mandatory [GitHub Project workflow](.agents/skills/raidmanager-github-project-workflow/SKILL.md).
Story development requires an accepted story and a child task with measurable acceptance criteria before branching
or implementation. Complete the task through its own issue-linked pull request.

## Branching

Create short-lived `feature/<task-number>-<slug>` or `fix/<task-number>-<slug>` branches from `main`.
Keep each pull request focused on one vertical slice or one architectural change.

## Commits

Use imperative, intention-revealing commit messages.
Do not combine unrelated refactors and feature behavior in one commit.

## Pull requests

Every pull request must:

- preserve inward-only project dependencies;
- include tests for business behavior and changed UI behavior;
- update documentation and the glossary when new domain terminology is introduced;
- contain no credentials or environment-specific secrets;
- pass formatting, analyzers, tests and documentation validation.

## Local setup

Configure the Pivot.Framework GitHub Packages credentials described in `README.md`, then run restore, build and tests
from the solution root.

## Security

Do not report security vulnerabilities in public issues.
Contact the repository owner privately until a dedicated security reporting channel is configured.
