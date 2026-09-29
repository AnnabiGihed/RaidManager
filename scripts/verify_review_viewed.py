"""Enforce the two-step review: the operator reviews every file first, then a peer approves the head commit.

The operator is the pull-request author, the person who ran the agent. They review by marking every changed file as
viewed and then marking the draft pull request Ready for review. A peer (anyone but the author) must then approve
the head commit, after that Ready for review event, having also marked every changed file as viewed.

GitHub exposes a file's viewed state only to the user who viewed it, so each person's marks are read with that
person's own read-only token, from the environment variable REVIEW_TOKEN_<LOGIN>.

Usage: verify_review_viewed.py gate      full check for the required status check
       verify_review_viewed.py operator  operator step only, run when the pull request is marked ready
"""

from __future__ import annotations

import json
import os
import re
import sys
import urllib.request
from dataclasses import dataclass
from datetime import datetime


GRAPHQL_URL = "https://api.github.com/graphql"

PULL_REQUEST_QUERY = """
query($owner: String!, $name: String!, $number: Int!) {
  repository(owner: $owner, name: $name) {
    pullRequest(number: $number) {
      author { login }
      isDraft
      headRefOid
      timelineItems(last: 1, itemTypes: [READY_FOR_REVIEW_EVENT]) {
        nodes { ... on ReadyForReviewEvent { createdAt } }
      }
      latestOpinionatedReviews(first: 100) {
        nodes { author { login } state submittedAt commit { oid } }
      }
    }
  }
}
"""

VIEWED_FILES_QUERY = """
query($owner: String!, $name: String!, $number: Int!, $cursor: String) {
  viewer { login }
  repository(owner: $owner, name: $name) {
    pullRequest(number: $number) {
      files(first: 100, after: $cursor) {
        pageInfo { hasNextPage endCursor }
        nodes { path viewerViewedState }
      }
    }
  }
}
"""


@dataclass(frozen=True)
class Review:
    author: str
    state: str
    commit: str
    submitted_at: datetime


@dataclass(frozen=True)
class PullRequest:
    author: str
    is_draft: bool
    head_sha: str
    ready_at: datetime | None
    reviews: list[Review]


def token_variable(login: str) -> str:
    """Return the environment variable holding a person's review token."""
    return "REVIEW_TOKEN_" + re.sub(r"[^A-Z0-9]", "_", login.upper())


def viewed_errors(login: str, files: dict[str, str] | None) -> list[str]:
    """Return why a person has not viewed every changed file; None means their token is missing."""
    if files is None:
        return [f"no review token for @{login}: add the {token_variable(login)} repository secret"]
    errors: list[str] = []
    for path, state in sorted(files.items()):
        if state == "DISMISSED":
            errors.append(f"changed since @{login} viewed it: {path}")
        elif state != "VIEWED":
            errors.append(f"not marked as viewed by @{login}: {path}")
    return errors


def operator_errors(pull_request: PullRequest, operator_files: dict[str, str] | None) -> list[str]:
    """Return why the operator's review is not complete."""
    operator = pull_request.author
    if pull_request.is_draft or pull_request.ready_at is None:
        return [f"@{operator} has not finished their review: view every file, then mark the draft Ready for review"]
    return viewed_errors(operator, operator_files)


def peer_errors(pull_request: PullRequest, files_by_login: dict[str, dict[str, str] | None]) -> list[str]:
    """Return why no peer approval counts yet; an approval counts only after the operator's review."""
    peer_reviews = [review for review in pull_request.reviews if review.author.lower() != pull_request.author.lower()]
    blocking = [review for review in peer_reviews if review.state == "CHANGES_REQUESTED"]
    if blocking:
        return [f"@{review.author} requested changes" for review in blocking]

    approvals = sorted(
        (review for review in peer_reviews if review.state == "APPROVED"),
        key=lambda review: review.submitted_at,
        reverse=True,
    )
    if not approvals:
        return [f"waiting for a peer approval: someone other than @{pull_request.author} must approve"]

    latest_errors: list[str] = []
    for approval in approvals:
        errors: list[str] = []
        if approval.commit != pull_request.head_sha:
            errors.append(
                f"@{approval.author} approved {approval.commit[:7]}, not the current head {pull_request.head_sha[:7]}"
            )
        if pull_request.ready_at is None or approval.submitted_at <= pull_request.ready_at:
            errors.append(f"@{approval.author} approved before @{pull_request.author} marked the pull request ready")
        errors.extend(viewed_errors(approval.author, files_by_login.get(approval.author.lower())))
        if not errors:
            return []
        latest_errors = latest_errors or errors
    return latest_errors


def gate_errors(pull_request: PullRequest, files_by_login: dict[str, dict[str, str] | None]) -> list[str]:
    """Return every reason the pull request may not merge yet; an empty list means the gate passes."""
    errors = operator_errors(pull_request, files_by_login.get(pull_request.author.lower()))
    return errors or peer_errors(pull_request, files_by_login)


def graphql(token: str, query: str, variables: dict[str, object]) -> dict:
    request = urllib.request.Request(
        GRAPHQL_URL,
        data=json.dumps({"query": query, "variables": variables}).encode(),
        headers={"Authorization": f"bearer {token}", "Content-Type": "application/json"},
    )
    with urllib.request.urlopen(request, timeout=30) as response:
        body = json.load(response)
    if body.get("errors"):
        raise RuntimeError(f"GitHub GraphQL error: {body['errors']}")
    return body["data"]


def parse_time(value: str) -> datetime:
    return datetime.fromisoformat(value.replace("Z", "+00:00"))


def fetch_pull_request(token: str, owner: str, name: str, number: int) -> PullRequest:
    node = graphql(token, PULL_REQUEST_QUERY, {"owner": owner, "name": name, "number": number})["repository"]["pullRequest"]
    ready_events = [event for event in node["timelineItems"]["nodes"] if event]
    return PullRequest(
        author=node["author"]["login"],
        is_draft=node["isDraft"],
        head_sha=node["headRefOid"],
        ready_at=parse_time(ready_events[-1]["createdAt"]) if ready_events else None,
        reviews=[
            Review(review["author"]["login"], review["state"], review["commit"]["oid"], parse_time(review["submittedAt"]))
            for review in node["latestOpinionatedReviews"]["nodes"]
            if review["author"] and review["commit"] and review["submittedAt"]
        ],
    )


def fetch_viewed_files(login: str, owner: str, name: str, number: int) -> dict[str, str] | None:
    """Read one person's viewed marks with their own token, or None when their token is not configured."""
    token = os.environ.get(token_variable(login), "")
    if not token:
        return None
    files: dict[str, str] = {}
    cursor: str | None = None
    while True:
        data = graphql(token, VIEWED_FILES_QUERY, {"owner": owner, "name": name, "number": number, "cursor": cursor})
        if data["viewer"]["login"].lower() != login.lower():
            raise RuntimeError(f"{token_variable(login)} belongs to @{data['viewer']['login']}, not @{login}")
        page = data["repository"]["pullRequest"]["files"]
        files.update({file["path"]: file["viewerViewedState"] for file in page["nodes"]})
        if not page["pageInfo"]["hasNextPage"]:
            return files
        cursor = page["pageInfo"]["endCursor"]


def main(mode: str) -> int:
    owner, name = os.environ["GITHUB_REPOSITORY"].split("/")
    number = int(os.environ["PR_NUMBER"])
    pull_request = fetch_pull_request(os.environ["GH_TOKEN"], owner, name, number)
    logins = {pull_request.author} | {review.author for review in pull_request.reviews}
    files_by_login = {login.lower(): fetch_viewed_files(login, owner, name, number) for login in logins}

    if mode == "operator":
        errors = viewed_errors(pull_request.author, files_by_login[pull_request.author.lower()])
    else:
        errors = gate_errors(pull_request, files_by_login)
    for error in errors:
        print(f"ERROR: {error}")
    if errors:
        return 1
    if mode == "operator":
        print(f"@{pull_request.author} viewed every changed file; a peer review is now expected")
    else:
        print(f"@{pull_request.author} reviewed, then a peer approved {pull_request.head_sha[:7]}, both after viewing every file")
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1] if len(sys.argv) > 1 else "gate"))
