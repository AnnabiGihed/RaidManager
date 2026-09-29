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
- be reviewed first by its operator, the author who ran the agent: the operator marks every changed file as
  **Viewed** and then marks the draft **Ready for review**;
- then receive one approval of the latest commit, from someone other than its author and last pusher, given after
  the operator's review and after the reviewer marked every changed file as **Viewed**;
- check every author self-review item except the CI and approval item;
- preserve inward-only project dependencies;
- include tests for business behavior and changed UI behavior;
- update documentation, API contracts, diagrams, and the glossary when affected;
- add a human-readable entry under `CHANGELOG.md` `[Unreleased]` for observable changes;
- contain no credentials or environment-specific secrets;
- pass the required `build-test`, `validate`, and `review-files-viewed` checks, including format, analyzers, tests,
  Markdown lint, prose, internal links, a strict documentation build, and the viewed-files review gate.

The checks catch mechanical defects; reviewers still verify technical accuracy, acceptance criteria, terminology,
and whether behavior changes have matching documentation. The published site is updated automatically after merge.

Agent pull requests open as drafts. The operator reviews first: mark each file as **Viewed** in the **Files
changed** tab, then click **Ready for review**. If a file is not viewed, the pull request returns to draft with a
comment listing it; otherwise the other reviewers are requested. A new commit returns the pull request to draft for
another operator pass. The peer then marks every file as **Viewed** and approves. Viewing a file does not re-run
`review-files-viewed`, so approve (or re-submit the approval) after the last file is viewed. Each reviewer needs a
read-only `REVIEW_TOKEN_<LOGIN>` secret. See [ADR-0005](docs/adr/0005-require-viewed-files-before-approval.md) and
[ADR-0006](docs/adr/0006-review-by-operator-before-peer.md). The agent never marks files as viewed or a pull request
as ready.

GitHub permits squash merges only. When the operator marks a pull request ready, the `review` workflow queues a
squash auto-merge ([ADR-0007](docs/adr/0007-queue-auto-merge-when-ready.md)). GitHub merges only after an independent
approval, the required `build-test`, `validate`, and `review-files-viewed` checks, and resolution of review
conversations. Do not use administrator bypass or merge your own pull request. GitHub deletes the source branch after
merge; confirm the linked task is closed and its project status is `Done`.

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
