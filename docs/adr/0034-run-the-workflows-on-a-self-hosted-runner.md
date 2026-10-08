# ADR-0034: Run the workflows on the owner's self-hosted runner

- Status: Accepted
- Date: 2026-10-08
- Deciders: Gihed Annabi

## Context

Every GitHub Actions job of the repository ran on `ubuntu-latest`, a runner GitHub hosts and wipes after each job. The
owner decided to switch every workflow to a self-hosted runner on their own Linux computer, labeled `self-hosted`,
`linux` and `pc-personal` (owner request on #561).

The repository is public. GitHub advises using self-hosted runners only with private repositories, because a pull
request from a fork can run its own code on the runner: here, the owner's computer. On 2026-10-08 the repository asked
for approval only before a first-time contributor's workflows ran, so anyone with one merged pull request could start a
job on that machine without approval.

The workflows rely on tools GitHub's images come with: Git, the GitHub CLI, `jq`, curl, SSH, `gzip` and Docker. The
PostgreSQL tests start containers through `Testcontainers`, the dev deployment builds images and sends them with
`docker save`, and the Lua job compiles Lua and `luarocks` from source.

## Decision

- **Every job** of every workflow in `.github/workflows/` uses `runs-on: [self-hosted, linux, pc-personal]`.
- **No fork pull request reaches the runner** (owner decision on #561, the recommended option). Each job a
  `pull_request` event can start carries
  `if: github.event_name != 'pull_request' || github.event.pull_request.head.repo.full_name == github.repository`
  (the `sonar` job adds the same test to its own condition). The `pull_request_target`, `workflow_run`, `issues`,
  `schedule`, push and manual runs execute `main`'s own workflow code, never a pull request's, and the `review` and
  `dependency-task` jobs already skip forks.
- **The owner sets** "Require approval for all external contributors" under the repository's Settings, Actions,
  General, so a fork's workflows wait for approval even if a guard is missed.
- **The docs check enforces these rules.** `scripts/workflow_runners.py`, run by `validate_docs.py`, fails on a job
  without these labels, on a job reachable from a fork pull request without the guard, and on a job that calls
  `python` or `python3` without `actions/setup-python` (#564).
- **Jobs install what they can themselves** (owner decision on #561): the local action `.github/actions/setup-tools`
  puts the GitHub CLI and `jq`, pinned and checked against their SHA-256, in the job's temporary folder when the image
  lacks them, and every job that calls them runs it after checkout. Python, .NET and Node come from the `setup-*`
  actions: the image has no `python` command, and the dev deployment's first run on the runners failed on it (#564).
- **The runner image provides** what can't be installed without `sudo`: Git, curl, `gzip` and `unzip`; a C compiler
  with `make` for the `lua` job; Docker usable by the job for the PostgreSQL tests and the deployment; and an SSH
  client for the deployment.

- **No job uses `sudo`:** the runners refuse it ("no new privileges"). The `lua` job therefore builds Lua 5.1.5
  itself, checked against its SHA-256, instead of with `leafo/gh-actions-lua`, which installs the `readline` headers
  with `sudo apt-get`.

## Consequences

- Jobs run only while the owner's computer is on and can start runners. A pull request's checks, the review merge,
  the dev deployment and the docs publication wait for it.
- A pull request from a fork gets no checks: its guarded jobs are skipped, and GitHub counts a skipped required check
  as passed. Such a pull request is never merged as it is. The owner pushes the change to a branch of this repository,
  whose pull request runs every check.
- The runners are ephemeral (the owner, 2026-10-08): each job gets a new runner that registers itself and is removed
  after the job, so no workspace, tool cache or package cache carries over. The `setup-*` actions and the restores
  download again in every job, and the image must provide the tools listed above.
- The deploy SSH keys and the environment secrets ([ADR-0028](0028-keep-the-test-secrets-in-a-github-environment.md))
  are now handled on the owner's computer during the deploy job instead of on a disposable GitHub machine.
- The Windows packaging job [ADR-0033](0033-publish-the-companion-through-the-microsoft-store.md) proposes needs a
  Windows runner, which this runner isn't; that ADR's implementation decides how it runs.
- GitHub-hosted runner minutes are no longer used.

## Alternatives considered

- **Keep GitHub-hosted runners:** no change on the owner's machine and no exposure, but not what the owner chose.
- **Self-hosted runner with the approval setting only:** fewer workflow changes, but a fork's code runs on the computer
  as soon as a run is approved, and one wrong approval is enough.
- **Make the repository private:** removes the fork risk, but GitHub Pages and SonarCloud's free plan need a public
  repository; the documentation site and the `sonar` gate would need replacing.
