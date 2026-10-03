"""Record a deployment on the items and releases it delivered (#392).

Each deployment marks what it delivered with a label per environment, so the Project shows what runs where:
`deployed:dev`, `deployed:test` or `deployed:production` on success, and `deploy-failed:<environment>` on the items a
failed deployment would have delivered. The state is recomputed from the deployed commit's history on every run, so
the first run also labels everything delivered before it, and a rerun changes nothing.

- A completed item without sub-issues is delivered when every merged pull request that closed it is in the deployed
  commit's history; one closed without a merged pull request is delivered once it is closed.
- A completed parent is delivered when it has completed children and all of them are delivered; canceled children
  don't count.
- A release milestone is deployed to an environment when all its issues are closed and its completed ones are
  delivered; its description then says so, between markers, and stops saying so if that changes.

Everything goes through `gh`: the workflow's built-in token in Actions (ADR-0026, specification §22), the operator's
login locally. Comments are added only for changes after the first labeled run, never for the backfill.

Usage: record_deployment.py --environment dev --outcome success --commit <sha> --run-url <url> [--dry-run]
"""

from __future__ import annotations

import argparse
import json
import os
import re
import subprocess
import sys
from dataclasses import dataclass, field

ENVIRONMENTS = ("dev", "test", "production")
LABEL_COLORS = {"dev": "c2e0c6", "test": "bfdadc", "production": "0e8a16"}
FAILED_COLOR = "d73a4a"
MARKER_START, MARKER_END = "<!-- deployments:start -->", "<!-- deployments:end -->"

ISSUES_QUERY = """
query($owner: String!, $name: String!, $after: String) {
  repository(owner: $owner, name: $name) {
    issues(first: 100, after: $after) {
      pageInfo { hasNextPage endCursor }
      nodes {
        number state stateReason
        labels(first: 50) { nodes { name } }
        milestone { number }
        subIssues(first: 100) { nodes { number repository { nameWithOwner } } }
        closedByPullRequestsReferences(first: 20, includeClosedPrs: true) { nodes { merged mergeCommit { oid } } }
      }
    }
  }
}
"""


def deployed_label(environment: str) -> str:
    return f"deployed:{environment}"


def failed_label(environment: str) -> str:
    return f"deploy-failed:{environment}"


@dataclass(frozen=True)
class Issue:
    number: int
    completed: bool
    closed: bool
    labels: frozenset[str] = frozenset()
    merge_commits: tuple[str, ...] = ()
    children: tuple[int | None, ...] = ()
    milestone: int | None = None


@dataclass(frozen=True)
class Milestone:
    number: int
    title: str
    description: str


@dataclass
class Plan:
    add: dict[int, list[str]] = field(default_factory=dict)
    remove: dict[int, list[str]] = field(default_factory=dict)
    comments: dict[int, str] = field(default_factory=dict)
    milestones: dict[int, str] = field(default_factory=dict)


def parse_issue(node: dict, repository: str) -> Issue:
    children = tuple(child["number"] if child["repository"]["nameWithOwner"] == repository else None
                     for child in node["subIssues"]["nodes"])
    merges = tuple(pr["mergeCommit"]["oid"] for pr in node["closedByPullRequestsReferences"]["nodes"]
                   if pr["merged"] and pr["mergeCommit"])
    return Issue(
        number=node["number"],
        completed=node["state"] == "CLOSED" and node["stateReason"] == "COMPLETED",
        closed=node["state"] == "CLOSED",
        labels=frozenset(label["name"] for label in node["labels"]["nodes"]),
        merge_commits=merges,
        children=children,
        milestone=(node["milestone"] or {}).get("number"),
    )


def delivered_items(issues: dict[int, Issue], history: set[str]) -> set[int]:
    """Lists the completed items the deployed commit delivered, parents included."""
    memo: dict[int, bool] = {}

    def delivered(number: int | None, path: frozenset[int]) -> bool:
        issue = issues.get(number) if number is not None else None
        if issue is None or not issue.completed or number in path:
            return False
        if number not in memo:
            if issue.children:
                done = [child for child in issue.children
                        if child is None or child not in issues or not issues[child].closed or issues[child].completed]
                memo[number] = bool(done) and all(delivered(child, path | {number}) for child in done)
            else:
                memo[number] = all(commit in history for commit in issue.merge_commits)
        return memo[number]

    return {number for number in issues if delivered(number, frozenset())}


def milestone_deployed(number: int, issues: dict[int, Issue], delivered: set[int]) -> bool:
    members = [issue for issue in issues.values() if issue.milestone == number]
    completed = [issue for issue in members if issue.completed]
    return bool(completed) and all(issue.closed for issue in members) and all(
        issue.number in delivered for issue in completed)


def milestone_description(description: str, environment: str, deployed: bool, line: str) -> str:
    """Adds or removes the environment's line in the description's deployment block."""
    pattern = re.compile(re.escape(MARKER_START) + r"\n(.*?)" + re.escape(MARKER_END), re.DOTALL)
    match = pattern.search(description)
    lines = [entry for entry in (match.group(1).splitlines() if match else []) if entry.strip()]
    prefix = f"- Deployed to {environment}:"
    current = [entry for entry in lines if entry.startswith(prefix)]
    others = [entry for entry in lines if not entry.startswith(prefix)]
    entries = others + ((current or [line]) if deployed else [])
    entries.sort(key=lambda entry: next((i for i, env in enumerate(ENVIRONMENTS)
                                         if entry.startswith(f"- Deployed to {env}:")), len(ENVIRONMENTS)))
    base = pattern.sub("", description).rstrip() if match else description.rstrip()
    if not entries:
        return base
    block = MARKER_START + "\n" + "\n".join(entries) + "\n" + MARKER_END
    return f"{base}\n\n{block}" if base else block


def plan_success(plan: Plan, issues: dict[int, Issue], delivered: set[int], environment: str, note: str) -> None:
    done, failed = deployed_label(environment), failed_label(environment)
    backfill = not any(done in issue.labels for issue in issues.values())
    for number, issue in sorted(issues.items()):
        if number in delivered and done not in issue.labels:
            plan.add[number] = [done]
            if not backfill:
                plan.comments[number] = f"Deployed to **{environment}** by {note}."
        elif done in issue.labels and not issue.completed:
            plan.remove.setdefault(number, []).append(done)
        if failed in issue.labels and (number in delivered or not issue.completed):
            plan.remove.setdefault(number, []).append(failed)


def plan_failure(plan: Plan, issues: dict[int, Issue], delivered: set[int], environment: str, note: str) -> None:
    done, failed = deployed_label(environment), failed_label(environment)
    for number, issue in sorted(issues.items()):
        if number in delivered and not issue.children and not issue.labels & {done, failed}:
            plan.add[number] = [failed]
            plan.comments[number] = (f"The **{environment}** deployment failed ({note}), so this item isn't "
                                     f"deployed there yet. The next successful deployment to {environment} "
                                     f"removes `{failed}`.")


def plan_deployment(issues: dict[int, Issue], milestones: list[Milestone], history: set[str], environment: str,
                    succeeded: bool, run: str, commit: str, date: str) -> Plan:
    plan = Plan()
    delivered = delivered_items(issues, history)
    note = f"[this run]({run}), commit `{commit[:7]}`"
    if not succeeded:
        plan_failure(plan, issues, delivered, environment, note)
        return plan
    plan_success(plan, issues, delivered, environment, note)
    line = f"- Deployed to {environment}: {date}, {note}"
    for milestone in milestones:
        updated = milestone_description(milestone.description, environment,
                                        milestone_deployed(milestone.number, issues, delivered), line)
        if updated != milestone.description:
            plan.milestones[milestone.number] = updated
    return plan


def gh(*args: str, stdin: str | None = None) -> str:
    return subprocess.run(["gh", *args], input=stdin, capture_output=True, text=True, check=True,
                          encoding="utf-8").stdout


def fetch_issues(repository: str) -> dict[int, Issue]:
    owner, name = repository.split("/")
    issues: dict[int, Issue] = {}
    after = None
    while True:
        args = ["api", "graphql", "-f", f"query={ISSUES_QUERY}", "-f", f"owner={owner}", "-f", f"name={name}"]
        if after:
            args += ["-f", f"after={after}"]
        page = json.loads(gh(*args))["data"]["repository"]["issues"]
        for node in page["nodes"]:
            issue = parse_issue(node, repository)
            issues[issue.number] = issue
        if not page["pageInfo"]["hasNextPage"]:
            return issues
        after = page["pageInfo"]["endCursor"]


def fetch_milestones(repository: str) -> list[Milestone]:
    pages = json.loads(gh("api", "--paginate", "--slurp", f"repos/{repository}/milestones?state=all&per_page=100"))
    return [Milestone(m["number"], m["title"], m["description"] or "") for page in pages for m in page]


def ensure_labels(repository: str, environment: str) -> None:
    pages = json.loads(gh("api", "--paginate", "--slurp", f"repos/{repository}/labels?per_page=100"))
    existing = {label["name"] for page in pages for label in page}
    wanted = {
        deployed_label(environment): (LABEL_COLORS[environment], f"Deployed to the {environment} environment"),
        failed_label(environment): (FAILED_COLOR, f"The last {environment} deployment of this item failed"),
    }
    for name, (color, description) in wanted.items():
        if name not in existing:
            gh("api", f"repos/{repository}/labels", "-f", f"name={name}", "-f", f"color={color}",
               "-f", f"description={description}")


def apply(plan: Plan, repository: str) -> None:
    for number, labels in plan.add.items():
        gh("api", f"repos/{repository}/issues/{number}/labels", *[arg for label in labels
                                                                  for arg in ("-f", f"labels[]={label}")])
    for number, labels in plan.remove.items():
        for label in labels:
            gh("api", "-X", "DELETE", f"repos/{repository}/issues/{number}/labels/{label}")
    for number, body in plan.comments.items():
        gh("api", f"repos/{repository}/issues/{number}/comments", "-F", "body=@-", stdin=body)
    for number, description in plan.milestones.items():
        gh("api", "-X", "PATCH", f"repos/{repository}/milestones/{number}", "-F", "description=@-",
           stdin=description)


def matching(pattern: str, what: str):
    """Builds an argument type that accepts only values matching the pattern, so no option reaches git or gh."""
    def check(value: str) -> str:
        if not re.fullmatch(pattern, value):
            raise argparse.ArgumentTypeError(f"not a valid {what}: {value!r}")
        return value
    return check


def main(argv: list[str]) -> int:
    parser = argparse.ArgumentParser(description=__doc__.split("\n\n", 1)[0])
    parser.add_argument("--environment", choices=ENVIRONMENTS, required=True)
    parser.add_argument("--outcome", choices=("success", "failure"), required=True)
    parser.add_argument("--commit", required=True, type=matching(r"[0-9a-f]{7,40}", "commit"))
    parser.add_argument("--run-url", required=True, type=matching(r"https://[\w./-]+", "run address"))
    parser.add_argument("--date", required=True, type=matching(r"\d{4}-\d{2}-\d{2}", "date"),
                        help="the deployment date, YYYY-MM-DD")
    parser.add_argument("--repository", type=matching(r"[\w.-]+/[\w.-]+", "repository"),
                        default=os.environ.get("GITHUB_REPOSITORY", "AnnabiGihed/RaidManager"))
    parser.add_argument("--dry-run", action="store_true", help="print the changes without making them")
    args = parser.parse_args(argv)

    history = set(subprocess.run(["git", "rev-list", "--end-of-options", args.commit], capture_output=True, text=True, check=True)
                  .stdout.split())
    issues = fetch_issues(args.repository)
    plan = plan_deployment(issues, fetch_milestones(args.repository), history, args.environment,
                           args.outcome == "success", args.run_url, args.commit, args.date)
    print(f"{args.environment} {args.outcome} at {args.commit[:7]}: label {len(plan.add)} items, unlabel "
          f"{len(plan.remove)}, comment on {len(plan.comments)}, update {len(plan.milestones)} milestones")
    for number, labels in sorted(plan.add.items()):
        print(f"  #{number} + {', '.join(labels)}")
    for number, labels in sorted(plan.remove.items()):
        print(f"  #{number} - {', '.join(labels)}")
    for number in sorted(plan.milestones):
        print(f"  milestone {number} description updated")
    if not args.dry_run:
        ensure_labels(args.repository, args.environment)
        apply(plan, args.repository)
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
