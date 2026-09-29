# Contributing

## Work items

Follow the mandatory [GitHub Project workflow](.agents/skills/raidmanager-github-project-workflow/SKILL.md).
Story development requires an accepted story and a child task with measurable acceptance criteria before branching
or implementation. Complete the task through its own issue-linked pull request.

## Branching

Create short-lived `feature/<task-number>-<slug>` or `fix/<task-number>-<slug>` branches from `main`.
Keep each pull request focused on one task. Do not push directly to `main`.

## Commit messages

Use [Conventional Commits](https://www.conventionalcommits.org/) with the task number in each title, for example
`docs(ci): enforce documentation checks (#44)`.
Do not combine unrelated refactors and feature behavior in one commit.

## Pull requests

Every pull request must:

- target `main`, link its task, and include the required five-section description and author checklist;
- receive one approval from someone other than the author before a squash merge;
- preserve inward-only project dependencies;
- include tests for business behavior and changed UI behavior;
- update documentation, API contracts, diagrams, and the glossary when affected;
- add a human-readable entry under `CHANGELOG.md` `[Unreleased]` for observable changes;
- contain no credentials or environment-specific secrets;
- pass the required `build-test` and `validate` checks, including format, analyzers, tests, Markdown lint, prose,
  internal links, and a strict documentation build.

The checks catch mechanical defects; reviewers still verify technical accuracy, acceptance criteria, terminology,
and whether behavior changes have matching documentation. The published site is updated automatically after merge.

GitHub permits squash merges only. Once a pull request is ready and its latest checks pass, the task owner queues
auto-merge with `gh pr merge --auto --squash`. GitHub merges only after an independent approval, the required
`build-test` and `validate` checks, and resolution of review conversations. Do not use administrator bypass or merge
your own pull request. GitHub deletes the source branch after merge; confirm the linked task is closed and its
project status is `Done`.

## Local setup

Install the .NET 10 SDK and configure the Pivot.Framework GitHub Packages credentials described in `README.md`.
From the solution root, run `dotnet restore RaidManager.sln`, `dotnet build RaidManager.sln --no-restore`, and
`dotnet test RaidManager.sln --no-build`.

## Releases

The `v1.0` milestone tracks the first usable release; it is not a release by itself. After its stories and checks
pass, the owner approves a release, assigns a Semantic Versioning number, dates the changelog section, and tags it.
Documentation publishes from `main` after every merge, independently of application releases.

## Security

Do not report security vulnerabilities in public issues.
Use [GitHub private vulnerability reporting](./SECURITY.md) to send a report to Gihed Annabi.
