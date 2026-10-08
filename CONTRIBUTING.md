# Contributing

## Work items

Work management follows the [Work Management and Delivery Specification](docs/reference/work-management-specification.md)
([ADR-0026](docs/adr/0026-adopt-the-work-management-specification.md)), applied by the eight `work-*` skills in
`.agents/skills/`:

- Every item sits in one chain of native sub-issues: Epic → Feature → User Story, Improvement, Bug or Spike → Task.
  Nothing is standalone. Classify by intent first, then scope (specification §2, §3), and create issues with the issue
  forms.
- No task, story, improvement, bug or spike is executed outside an active sprint (specification §8).
- Status, Delivery Stage, completion, cancellation and releases follow specification §6 to §13.

Deliver each task through the mandatory [GitHub Project workflow](.agents/skills/raidmanager-github-project-workflow/SKILL.md):
its own branch and issue-linked pull request. The [hierarchy workflow](.github/workflows/project-hierarchy.yml) and
the checks in [Project automation](docs/reference/project-automation.md) enforce the rules they can see without a
personal token or repository secret.

## Branching

Create short-lived `feature/<task-number>-<slug>` or `fix/<task-number>-<slug>` branches from `main`.
Dependabot's `dependabot/...` branches are the one exception; each of its pull requests still closes its own task
([dependency updates](docs/reference/project-automation.md#dependency-updates)).
Keep each pull request focused on one task. Do not push directly to `main`.

## Commit messages

Use [Conventional Commits](https://www.conventionalcommits.org/) with the task number in each title, for example
`docs(ci): enforce documentation checks (#44)`.
Do not combine unrelated refactors and feature behavior in one commit.

## Pull requests

Every pull request must:

- target `main`, link its task, and include the required five-section description and author checklist;
- put `Closes #<task-number>` on its own line before the first heading; it closes only a task whose chain reaches
  an epic through a story, improvement, bug or spike and a feature. Use `Refs` for those parents;
- show the Penpot mockup it implements when it changes what users see, or state `No visual change: <reason>`
  ([ADR-0017](docs/adr/0017-penpot-mockups-for-ui-work.md), [Design a screen](docs/how-to/design-a-screen.md));
- pass the coverage gate: tests cover at least 80% of the changed source lines, and total line coverage stays at or
  above 60% ([ADR-0015](docs/adr/0015-gate-pull-requests-on-test-coverage.md)). The coverage comment on the pull
  request lists any uncovered changed line;
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
- pass the required `description`, `docs`, `build-test`, `sonar`, and `review-gate` checks, including the
  description, format, analyzers, tests, Markdown lint, prose, internal links, a strict documentation build, and the
  review gate; a check whose files the pull request doesn't change is skipped, which counts as passed.

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
peer approval, the required `description`, `docs`, `build-test`, `sonar`, and `review-gate` checks, and resolution of review
conversations are all in place ([ADR-0008](docs/adr/0008-merge-with-the-workflow-token.md)). Branch protection still
decides, and nobody queues or clicks the merge. Do not use administrator bypass or merge your own pull request. GitHub
deletes the source branch after merge; confirm the linked task is closed and its project status is `Done`.

Every workflow job runs on the owner's self-hosted runner, labeled `self-hosted`, `linux` and `pc-personal`
([ADR-0034](docs/adr/0034-run-the-workflows-on-a-self-hosted-runner.md)): checks wait while it is offline. A job that a
`pull_request` event starts skips pull requests from forks, so a fork's code never runs on that machine; such a pull
request gets no checks and isn't merged as it is. The docs check fails on a job without these labels or without that
guard.

## Local setup

Install the .NET 10 SDK and give your machine access to the Pivot.Framework packages as
[`README.md`](README.md#give-your-machine-access-to-the-pivotframework-packages) describes.
From the solution root, run `dotnet restore RaidManager.sln`, `dotnet build RaidManager.sln --no-restore`, and
`dotnet test RaidManager.sln --no-build`. For the repository scripts, run
`python -m unittest discover -s scripts/tests` and `python -m mypy`. For documentation, run
`python scripts/spell_check.py` after `pip install pyspellchecker`: it accepts US English and the project word list
in `.vale/styles/config/vocabularies/RaidManager/accept.txt`, which Visual Studio also uses. CI runs all three.

## Releases

The `v1.0` milestone tracks the first usable release; it is not a release by itself. After its stories and checks
pass, the owner approves a release, assigns a Semantic Versioning number, dates the changelog section, and tags it.
Documentation publishes from `main` after every merge, independently of application releases: to the
[GitHub Pages site](https://annabigihed.github.io/RaidManager/) and to the
[GitHub Wiki](https://github.com/AnnabiGihed/RaidManager/wiki). Both are generated from `docs/`. Never edit the wiki
directly; the next merge overwrites it ([ADR-0014](docs/adr/0014-mirror-documentation-to-the-github-wiki.md)). Every
document starts with its `#` title and appears in the `mkdocs.yml` navigation, which also builds the wiki sidebar.

## Security

Do not report security vulnerabilities in public issues.
Use [GitHub private vulnerability reporting](./SECURITY.md) to send a report to Gihed Annabi.
