"""Decide whether a merge needs a dev deployment, from the files it changed (#491).

The review workflow starts `deploy-dev` after every merge. A merge that changes only documentation, skills, tests,
scripts, mockups or other workflows changes nothing dev runs, so rebuilding the images and restarting dev would only
cost time and a restart. This script compares `main` with the commit of the last successful dev deployment, read
from the `dev` environment's deployments, and lists the changed files that can affect a deployment (owner decisions
on #491):

- anything under `src/` and `deploy/`, except the WoW addon in `src/Addon/`, which players install in the game, and
  the desktop companion's two projects, published as a CI artifact for players' computers (#600): neither runs on the
  server (owner decisions on #491 and #600, ADR-0031, ADR-0032);
- the build files: `Directory.Build.props`, `Directory.Packages.props`, `nuget.config`, `global.json`,
  `dotnet-tools.json` and `RaidManager.sln`;
- the deployment workflow itself, `.github/workflows/deploy-dev.yml`.

Comparing with the last successful deployment, not the previous commit, means a change from a failed or skipped run
is deployed by the next one. A run started by hand (`TRIGGER` other than `merge`) always deploys, and so does a run
that finds no successful deployment in the current history.

It prints `deploy=true` or `deploy=false` for `$GITHUB_OUTPUT` on standard output, and its reasons on standard
error. Everything goes through `gh` and `git` with the workflow's built-in token; nothing from the command line or
the environment reaches them except the trigger name.

Usage, from a full checkout of the commit to deploy, with GH_TOKEN and TRIGGER set by the workflow:
    deploy_changes.py >> "$GITHUB_OUTPUT"
"""

from __future__ import annotations

import json
import os
import subprocess
import sys
from collections.abc import Callable, Iterable

REPOSITORY = "AnnabiGihed/RaidManager"
ENVIRONMENT = "dev"
MERGE_TRIGGER = "merge"
DEPLOYED_FOLDERS = ("src/", "deploy/")
NOT_DEPLOYED_FOLDERS = ("src/Addon/", "src/Containers/UI/Core/RaidManager.Companion.Client/",
                        "src/Containers/UI/Hosting/RaidManager.Companion/")
DEPLOYED_FILES = frozenset({
    "Directory.Build.props", "Directory.Packages.props", "nuget.config", "global.json", "dotnet-tools.json",
    "RaidManager.sln", ".github/workflows/deploy-dev.yml",
})
# Deployments are read newest first; a successful one is normally among the latest few.
DEPLOYMENTS_READ = 30


def affects_deployment(path: str) -> bool:
    """Tells whether a changed file can change what the dev environment runs."""
    if path.startswith(NOT_DEPLOYED_FOLDERS):
        return False
    return path.startswith(DEPLOYED_FOLDERS) or path in DEPLOYED_FILES


def deployed_changes(changed: Iterable[str]) -> list[str]:
    """The changed files that need a deployment, in the order given."""
    return [path for path in changed if affects_deployment(path)]


def last_deployed_commit(deployments: list[dict], latest_state: Callable[[int], str | None],
                         history: list[str]) -> str | None:
    """The newest commit in the current history whose dev deployment succeeded, as git itself names it."""
    known = set(history)
    for deployment in deployments:
        sha = deployment.get("sha")
        if sha in known and latest_state(int(deployment["id"])) == "success":
            return next(commit for commit in history if commit == sha)
    return None


def gh_json(path: str) -> list | dict:
    output = subprocess.run(["gh", "api", f"repos/{REPOSITORY}/{path}"], check=True, capture_output=True, text=True,
                            encoding="utf-8").stdout
    return json.loads(output)


def git(*arguments: str) -> str:
    return subprocess.run(["git", *arguments], check=True, capture_output=True, text=True, encoding="utf-8").stdout


def latest_state(deployment_id: int) -> str | None:
    statuses = gh_json(f"deployments/{deployment_id}/statuses?per_page=1")
    return statuses[0].get("state") if isinstance(statuses, list) and statuses else None


def decide() -> tuple[bool, list[str]]:
    """Returns whether to deploy and the reasons, read from the repository and its deployments."""
    if os.environ.get("TRIGGER") != MERGE_TRIGGER:
        return True, ["Started by hand: a manual run always deploys."]
    history = git("rev-list", "HEAD").split()
    deployments = gh_json(f"deployments?environment={ENVIRONMENT}&per_page={DEPLOYMENTS_READ}")
    base = last_deployed_commit(deployments if isinstance(deployments, list) else [], latest_state, history)
    if base is None:
        return True, ["No successful dev deployment is in the current history: deploying."]
    changed = deployed_changes(git("diff", "--name-only", base, history[0]).split("\n"))
    if changed:
        return True, [f"Changed since the last dev deployment ({base[:7]}), so dev is deployed:",
                      *(f"  {path}" for path in changed)]
    return False, [f"Nothing dev runs changed since the last dev deployment ({base[:7]}): the deployment is skipped "
                   "and the delivered items are recorded."]


def main() -> None:
    deploy, reasons = decide()
    for reason in reasons:
        print(reason, file=sys.stderr)
    print(f"deploy={'true' if deploy else 'false'}")


if __name__ == "__main__":
    main()
