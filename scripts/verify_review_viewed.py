"""Require the designated reviewer to approve the head commit after marking every changed file as viewed.

GitHub exposes a file's viewed state only to the user who viewed it, so this check authenticates with the
designated reviewer's own read-only token (REVIEW_GATE_TOKEN) and reads that reviewer's state.
"""

from __future__ import annotations

import json
import os
import urllib.request
from dataclasses import dataclass


GRAPHQL_URL = "https://api.github.com/graphql"

QUERY = """
query($owner: String!, $name: String!, $number: Int!, $cursor: String) {
  viewer { login }
  repository(owner: $owner, name: $name) {
    pullRequest(number: $number) {
      headRefOid
      files(first: 100, after: $cursor) {
        pageInfo { hasNextPage endCursor }
        nodes { path viewerViewedState }
      }
      latestOpinionatedReviews(first: 100) {
        nodes { author { login } state commit { oid } }
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


def evaluate(reviewer: str, head_sha: str, files: dict[str, str], reviews: list[Review]) -> list[str]:
    """Return every reason the reviewer's approval does not yet count; an empty list means the gate passes."""
    errors: list[str] = []
    review = next((candidate for candidate in reviews if candidate.author.lower() == reviewer.lower()), None)
    if review is None:
        errors.append(f"@{reviewer} has not reviewed this pull request")
    elif review.state != "APPROVED":
        errors.append(f"@{reviewer}'s latest review is {review.state}, not an approval")
    elif review.commit != head_sha:
        errors.append(f"@{reviewer} approved {review.commit[:7]}, not the current head {head_sha[:7]}")

    for path, state in sorted(files.items()):
        if state == "DISMISSED":
            errors.append(f"changed since @{reviewer} viewed it: {path}")
        elif state != "VIEWED":
            errors.append(f"not marked as viewed by @{reviewer}: {path}")
    return errors


def fetch(token: str, owner: str, name: str, number: int) -> tuple[str, str, dict[str, str], list[Review]]:
    """Read the reviewer's login, the head commit, per-file viewed state and latest reviews."""
    files: dict[str, str] = {}
    cursor: str | None = None
    while True:
        payload = json.dumps({"query": QUERY, "variables": {"owner": owner, "name": name, "number": number, "cursor": cursor}})
        request = urllib.request.Request(
            GRAPHQL_URL,
            data=payload.encode(),
            headers={"Authorization": f"bearer {token}", "Content-Type": "application/json"},
        )
        with urllib.request.urlopen(request, timeout=30) as response:
            body = json.load(response)
        if body.get("errors"):
            raise RuntimeError(f"GitHub GraphQL error: {body['errors']}")
        pull_request = body["data"]["repository"]["pullRequest"]
        files.update({node["path"]: node["viewerViewedState"] for node in pull_request["files"]["nodes"]})
        page = pull_request["files"]["pageInfo"]
        if not page["hasNextPage"]:
            break
        cursor = page["endCursor"]

    reviews = [
        Review(node["author"]["login"], node["state"], node["commit"]["oid"])
        for node in pull_request["latestOpinionatedReviews"]["nodes"]
        if node["author"] and node["commit"]
    ]
    return body["data"]["viewer"]["login"], pull_request["headRefOid"], files, reviews


def main() -> int:
    token = os.environ.get("REVIEW_GATE_TOKEN", "")
    if not token:
        print("ERROR: the REVIEW_GATE_TOKEN secret is not configured; the review gate fails closed")
        return 1

    owner, name = os.environ["GITHUB_REPOSITORY"].split("/")
    reviewer, head_sha, files, reviews = fetch(token, owner, name, int(os.environ["PR_NUMBER"]))
    errors = evaluate(reviewer, head_sha, files, reviews)
    for error in errors:
        print(f"ERROR: {error}")
    if errors:
        print("After viewing every file, submit (or re-submit) the approval to re-run this check.")
        return 1
    print(f"@{reviewer} approved {head_sha[:7]} after viewing all {len(files)} changed files")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
