# Contributing

## Work items

Follow the mandatory [GitHub Project workflow](.agents/skills/raidmanager-github-project-workflow/SKILL.md).
Story development requires an accepted story and a child task with measurable acceptance criteria before branching
or implementation. Complete the task through its own issue-linked pull request.

An epic stays open and outside `Done` until it has at least one native child story, all child stories are closed
and `Done`, and its exit criteria are met. A story stays open and outside `Done` until it has at least one native
child work item, all child work items are closed and `Done`, and its acceptance criteria are evidenced. Tasks,
bugs, and spikes count as work items. The [hierarchy workflow](.github/workflows/project-hierarchy.yml) reopens
invalid closures and audits them every 15 minutes. Direct Project `Done` changes are also reconciled when the
`RAID_MANAGER_PROJECT_TOKEN` repository secret has Projects write and Issues write access.

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
- put `Closes #<work-item-number>` on its own line before the first heading; use `Refs` for parent stories and epics;
- be reviewed first by its operator, the author who ran the agent: a meaningful review comment on the latest commit,
  then **Ready for review** on the draft;
- then receive one approval of the latest commit, with a meaningful comment, from someone other than its author and
  last pusher, given after the operator's review;
- check every author self-review item except the CI and approval item;
- preserve inward-only project dependencies;
- include tests for business behavior and changed UI behavior;
- update documentation, API contracts, diagrams, and the glossary when affected;
- add a human-readable entry under `CHANGELOG.md` `[Unreleased]` for observable changes;
- contain no credentials or environment-specific secrets;
- pass the required `build-test`, `validate`, and `review-gate` checks, including format, analyzers, tests, Markdown
  lint, prose, internal links, a strict documentation build, and the review gate.

The checks catch mechanical defects; reviewers still verify technical accuracy, acceptance criteria, terminology,
and whether behavior changes have matching documentation. The published site is updated automatically after merge.

Agent pull requests open as drafts. The operator reviews first: submit a review with **Comment** that explains what
you checked and names a changed file or identifier, then click **Ready for review**. Without that comment the pull
request returns to draft with the reason; otherwise the other reviewers are requested. A new commit returns the pull
request to draft for another operator pass. The peer then approves with a comment of their own. Every review comment,
summary or inline, must be meaningful: at least 10 words for a summary or 5 for an inline comment, not only generic
praise such as LGTM, not random text, a summary must name a changed file or an identifier from the diff, and no
comment may copy another person's. A failing comment turns `review-gate` red and names the fix; editing the comment
re-checks it. [Review a pull request](docs/how-to/review-a-pull-request.md) walks through both reviews with examples.
See [ADR-0009](docs/adr/0009-prove-reviews-with-meaningful-comments.md) and
[ADR-0006](docs/adr/0006-review-by-operator-before-peer.md). The agent never writes the operator's review comments and
never marks a pull request ready.

GitHub permits squash merges only. The `review` workflow merges a pull request as soon as the operator's review, the
peer approval, the required `build-test`, `validate`, and `review-gate` checks, and resolution of review
conversations are all in place ([ADR-0008](docs/adr/0008-merge-with-the-workflow-token.md)). Branch protection still
decides, and nobody queues or clicks the merge. Do not use administrator bypass or merge your own pull request. GitHub
deletes the source branch after merge; confirm the linked task is closed and its project status is `Done`.

## Local setup

Install the .NET 10 SDK and give your machine access to the Pivot.Framework packages as
[`README.md`](README.md#give-your-machine-access-to-the-pivotframework-packages) describes.
From the solution root, run `dotnet restore RaidManager.sln`, `dotnet build RaidManager.sln --no-restore`, and
`dotnet test RaidManager.sln --no-build`.

## Releases

The `v1.0` milestone tracks the first usable release; it is not a release by itself. After its stories and checks
pass, the owner approves a release, assigns a Semantic Versioning number, dates the changelog section, and tags it.
Documentation publishes from `main` after every merge, independently of application releases.

## Security

Do not report security vulnerabilities in public issues.
Use [GitHub private vulnerability reporting](./SECURITY.md) to send a report to Gihed Annabi.
