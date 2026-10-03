"""Enforce the two-step review, proven by meaningful review comments.

The operator is the pull-request author, the person who ran the agent; for a dependency update opened by
Dependabot, it is the repository owner (owner decision on #460). They review first: a review comment on the
head commit that explains what they checked, then Ready for review on the draft. A peer (anyone but the author) then
approves the head commit with a comment of their own. Every human review comment, whether a review summary or an
inline comment, must be meaningful: long enough, not only generic praise, not random text, and, for a summary,
about this change. Everything is read with the workflow token; no personal token is needed.

Usage: verify_review.py gate      full check; exits 0 when it passes, 3 while a review is outstanding, 1 on a problem
       verify_review.py operator  operator step only, run when the pull request is marked ready
"""

from __future__ import annotations

import json
import os
import re
import sys
import urllib.request
from dataclasses import dataclass
from difflib import SequenceMatcher
from datetime import datetime


SUCCESS, PENDING, FAILURE = "success", "pending", "failure"
EXIT_CODES = {SUCCESS: 0, FAILURE: 1, PENDING: 3}

SUMMARY_MINIMUM_WORDS = 10
# A comment sharing this share of its word sequence with another person's comment is a copy, even lightly edited.
DUPLICATE_SIMILARITY = 0.8
INLINE_MINIMUM_WORDS = 5

# Praise and filler that say nothing about what was reviewed. They are removed before counting words.
GENERIC_PHRASES = (
    "looks good to me", "looks good", "look good", "lgtm", "ship it", "sounds good", "all good", "good job",
    "nice work", "great work", "well done", "thank you", "thanks", "approved", "approve", "okay", "ok", "fine",
    "nice", "great", "good", "done", "perfect", "awesome", "cool", "yes", "+1", "nit",
)
KEYBOARD_ROWS = ("qwertyuiop", "asdfghjkl", "zxcvbnm", "1234567890")
TECHNICAL_ABBREVIATIONS = {"html", "http", "https", "xml", "css", "sql", "yml", "npm", "pnpm", "cdn", "ssh", "svg",
                           "pdf", "dns", "tcp", "crlf", "lf", "ts", "js", "md", "gh", "pr", "prs", "cs"}
BOT_SUFFIX = "[bot]"
# Bots whose pull requests the repository owner reviews as the operator (owner decision on #460).
DEPENDENCY_BOTS = frozenset({"dependabot[bot]"})
OWNER = "AnnabiGihed"


@dataclass(frozen=True)
class Review:
    author: str
    state: str
    commit: str
    submitted_at: datetime
    body: str


@dataclass(frozen=True)
class InlineComment:
    author: str
    path: str
    body: str
    created_at: datetime


@dataclass(frozen=True)
class Change:
    """What the pull request changes, used to tell whether a comment is about it."""

    paths: tuple[str, ...]
    diff: str


@dataclass(frozen=True)
class PullRequest:
    author: str
    is_draft: bool
    head_sha: str
    ready_at: datetime | None
    reviews: list[Review]
    comments: list[InlineComment]
    change: Change


def words_of(text: str) -> list[str]:
    return re.findall(r"[A-Za-z0-9][\w'./()-]*", text)


def without_quotes_and_links(text: str) -> str:
    """Drop quoted replies and links, which are not the commenter's own words."""
    lines = [line for line in text.splitlines() if not line.lstrip().startswith(">")]
    return re.sub(r"https?://\S+", " ", "\n".join(lines))


def without_generic_phrases(text: str) -> str:
    stripped = text.lower()
    for phrase in GENERIC_PHRASES:
        stripped = re.sub(rf"(?<![\w+]){re.escape(phrase)}(?!\w)", " ", stripped)
    return re.sub(r"[^\w\s'./()`-]", " ", stripped)


def is_code_like(word: str) -> bool:
    return bool(re.search(r"[_./()]|\d|[a-z][A-Z]", word))


def is_keyboard_mash(word: str) -> bool:
    """Return whether a word is a run of neighbouring keys, such as asdf or qwerty."""
    lowered = word.lower()
    rows = KEYBOARD_ROWS + tuple(row[::-1] for row in KEYBOARD_ROWS)
    if len(lowered) == 4 and any(lowered in row for row in rows):
        return True
    return any(lowered[i:i + 5] in row for row in rows for i in range(len(lowered) - 4))


def is_random_word(word: str, change: Change) -> bool:
    """Return whether a word looks like a keyboard mash or has no pronounceable shape."""
    lowered = word.lower().strip("'.-")
    if len(lowered) < 4 or is_code_like(word) or word.isupper() or lowered in TECHNICAL_ABBREVIATIONS:
        return False
    if lowered in change.diff.lower():
        return False
    if is_keyboard_mash(lowered):
        return True
    return not re.search(r"[aeiouy]", lowered) or bool(re.search(r"[^aeiouy\W\d]{6,}", lowered))


def mentions_the_change(text: str, change: Change) -> bool:
    """Return whether a comment names a changed file or an identifier that appears in the diff."""
    lowered = text.lower()
    for path in change.paths:
        name = path.rsplit("/", 1)[-1]
        stem = name.rsplit(".", 1)[0]
        if path.lower() in lowered or name.lower() in lowered or (len(stem) >= 3 and re.search(rf"\b{re.escape(stem.lower())}\b", lowered)):
            return True
    quoted = re.findall(r"`([^`]{3,})`", text)
    identifiers = [word.strip(".,:;()") for word in words_of(text) if is_code_like(word) and len(word) >= 3]
    return any(term.strip() and term.strip() in change.diff for term in quoted + identifiers)


def comment_problems(text: str, change: Change, *, summary: bool) -> list[str]:
    """Return why a review comment is not meaningful; an empty list means it is."""
    own_words = without_quotes_and_links(text)
    minimum = SUMMARY_MINIMUM_WORDS if summary else INLINE_MINIMUM_WORDS
    remaining = words_of(without_generic_phrases(own_words))
    if not words_of(own_words):
        return ["it is empty"]
    if not remaining:
        return ["it only contains generic praise such as LGTM or looks good"]
    problems: list[str] = []
    if len(remaining) < minimum:
        problems.append(f"it has {len(remaining)} meaningful words; at least {minimum} are needed")
    random_words = [word for word in words_of(own_words) if is_random_word(word, change)]
    if random_words and (any(is_keyboard_mash(word) for word in random_words) or len(random_words) * 4 > len(words_of(own_words))):
        problems.append(f"it contains random text: {', '.join(random_words[:3])}")
    if summary and not mentions_the_change(own_words, change):
        problems.append("it does not name a changed file or an identifier from the diff")
    return problems


def operator_of(author: str) -> str:
    """The person who reviews first: the author, or the owner for a dependency bot's pull request."""
    return OWNER if author.lower() in DEPENDENCY_BOTS else author


def is_person(login: str) -> bool:
    return not login.endswith(BOT_SUFFIX)


@dataclass(frozen=True)
class WrittenComment:
    """A human review comment in the form the rules check: who wrote it, where, and when."""

    author: str
    label: str
    body: str
    written_at: datetime
    summary: bool


def written_comments(pull_request: PullRequest) -> list[WrittenComment]:
    """Return every human review comment that must be meaningful, oldest first."""
    comments: list[WrittenComment] = []
    for review in pull_request.reviews:
        required = review.state in ("APPROVED", "CHANGES_REQUESTED") or review.author.lower() == pull_request.author.lower()
        if not is_person(review.author) or review.state in ("PENDING", "DISMISSED") or (not review.body.strip() and not required):
            continue
        kind = "approval" if review.state == "APPROVED" else "review"
        comments.append(WrittenComment(review.author, f"{kind} comment", review.body, review.submitted_at, True))
    for comment in pull_request.comments:
        if is_person(comment.author):
            comments.append(WrittenComment(comment.author, f"comment on {comment.path}", comment.body, comment.created_at, False))
    return sorted(comments, key=lambda comment: comment.written_at)


def is_copy(text: str, original: str) -> bool:
    """Return whether a comment repeats another one word for word, allowing small edits."""
    words = [word.lower() for word in words_of(without_quotes_and_links(text))]
    original_words = [word.lower() for word in words_of(without_quotes_and_links(original))]
    return bool(words) and SequenceMatcher(None, words, original_words).ratio() >= DUPLICATE_SIMILARITY


def invalid_comment_messages(pull_request: PullRequest) -> list[str]:
    """Return a fix request for every human review comment that is not meaningful or copies someone else's."""
    messages: list[str] = []
    comments = written_comments(pull_request)
    for index, comment in enumerate(comments):
        problems = comment_problems(comment.body, pull_request.change, summary=comment.summary)
        original = next(
            (earlier for earlier in comments[:index]
             if earlier.author.lower() != comment.author.lower() and is_copy(comment.body, earlier.body)),
            None,
        )
        if original:
            problems.append(f"it repeats @{original.author}'s {original.label}")
        if problems:
            messages.append(f"edit @{comment.author}'s {comment.label}: {'; '.join(problems)}")
    return messages


def operator_review(pull_request: PullRequest) -> Review | None:
    """Return the operator's latest meaningful review comment on the head commit."""
    candidates = [
        review for review in pull_request.reviews
        if review.author.lower() == pull_request.author.lower()
        and review.commit == pull_request.head_sha
        and review.body.strip()
        and not comment_problems(review.body, pull_request.change, summary=True)
    ]
    if not candidates:
        return None
    return max(candidates, key=lambda review: review.submitted_at)


def operator_errors(pull_request: PullRequest) -> list[str]:
    """Return why the operator's review is not complete."""
    if operator_review(pull_request) is None:
        return [
            f"waiting for @{pull_request.author}'s review: post a review comment on the latest commit that explains "
            "what you checked and names a changed file, then mark the draft Ready for review"
        ]
    if pull_request.is_draft or pull_request.ready_at is None:
        return [f"waiting for @{pull_request.author} to mark the draft Ready for review"]
    return []


def peer_result(pull_request: PullRequest) -> tuple[str, list[str]]:
    """Classify the peer approval, which counts only after the operator's review and sign-off."""
    latest_by_peer: dict[str, Review] = {}
    for review in sorted(pull_request.reviews, key=lambda review: review.submitted_at):
        if review.author.lower() != pull_request.author.lower() and review.state in ("APPROVED", "CHANGES_REQUESTED"):
            latest_by_peer[review.author.lower()] = review
    blocking = [review for review in latest_by_peer.values() if review.state == "CHANGES_REQUESTED"]
    if blocking:
        return FAILURE, [f"@{review.author} requested changes" for review in blocking]

    approvals = sorted(latest_by_peer.values(), key=lambda review: review.submitted_at, reverse=True)
    if not approvals:
        return PENDING, [f"waiting for a peer approval: someone other than @{pull_request.author} must approve"]

    operator = operator_review(pull_request)
    signed_off_at = max(time for time in (pull_request.ready_at, operator.submitted_at if operator else None) if time)
    latest_errors: list[str] = []
    for approval in approvals:
        errors: list[str] = []
        if approval.commit != pull_request.head_sha:
            errors.append(
                f"@{approval.author} approved {approval.commit[:7]}, not the current head {pull_request.head_sha[:7]}"
            )
        if approval.submitted_at <= signed_off_at:
            errors.append(f"@{approval.author} approved before @{pull_request.author}'s review and sign-off")
        if not errors:
            return SUCCESS, []
        latest_errors = latest_errors or errors
    return FAILURE, latest_errors


def gate_result(pull_request: PullRequest) -> tuple[str, list[str]]:
    """Classify the gate: pending while a review is outstanding, failure only for a problem someone must fix."""
    invalid = invalid_comment_messages(pull_request)
    if invalid:
        return FAILURE, invalid
    waiting = operator_errors(pull_request)
    if waiting:
        return PENDING, waiting
    return peer_result(pull_request)


def api(path: str) -> list | dict:
    """Read every page of a GitHub REST resource with the workflow token."""
    results: list = []
    url: str | None = f"https://api.github.com/{path}{'&' if '?' in path else '?'}per_page=100"
    while url:
        request = urllib.request.Request(url, headers={
            "Authorization": f"bearer {os.environ['GH_TOKEN']}",
            "Accept": "application/vnd.github+json",
        })
        with urllib.request.urlopen(request, timeout=30) as response:
            page = json.load(response)
            links = response.headers.get("Link", "")
        if isinstance(page, dict):
            return page
        results.extend(page)
        match = re.search(r'<([^>]+)>;\s*rel="next"', links)
        url = match.group(1) if match else None
    return results


def api_object(path: str) -> dict:
    """Read a GitHub REST resource that is a single object."""
    value = api(path)
    if not isinstance(value, dict):
        raise TypeError(f"GitHub returned a list for {path}")
    return value


def api_list(path: str) -> list:
    """Read every page of a GitHub REST resource that is a list."""
    value = api(path)
    if not isinstance(value, list):
        raise TypeError(f"GitHub returned an object for {path}")
    return value


def parse_time(value: str) -> datetime:
    return datetime.fromisoformat(value.replace("Z", "+00:00"))


def fetch_pull_request(repository: str, number: int) -> PullRequest:
    base = f"repos/{repository}"
    pull = api_object(f"{base}/pulls/{number}")
    files = api_list(f"{base}/pulls/{number}/files")
    ready_events = [event for event in api_list(f"{base}/issues/{number}/timeline") if event.get("event") == "ready_for_review"]
    return PullRequest(
        author=operator_of(pull["user"]["login"]),
        is_draft=pull["draft"],
        head_sha=pull["head"]["sha"],
        ready_at=max((parse_time(event["created_at"]) for event in ready_events), default=None),
        reviews=[
            Review(review["user"]["login"], review["state"], review["commit_id"], parse_time(review["submitted_at"]),
                   review.get("body") or "")
            for review in api(f"{base}/pulls/{number}/reviews")
            if review.get("user") and review.get("submitted_at")
        ],
        comments=[
            InlineComment(comment["user"]["login"], comment["path"], comment.get("body") or "",
                          parse_time(comment["created_at"]))
            for comment in api(f"{base}/pulls/{number}/comments")
            if comment.get("user")
        ],
        change=Change(tuple(file["filename"] for file in files), "\n".join(file.get("patch", "") for file in files)),
    )


def main(mode: str) -> int:
    pull_request = fetch_pull_request(os.environ["GITHUB_REPOSITORY"], int(os.environ["PR_NUMBER"]))
    if mode == "operator":
        errors = operator_errors(pull_request) if operator_review(pull_request) is None else []
        for error in errors:
            print(f"ERROR: {error[:1].upper()}{error[1:]}")
        if errors:
            return 1
        print(f"@{pull_request.author} reviewed the change; a peer review is now expected")
        return 0

    state, messages = gate_result(pull_request)
    if state == SUCCESS:
        messages = [f"@{pull_request.author} reviewed, then a peer approved {pull_request.head_sha[:7]} with meaningful comments"]
    for message in messages:
        print(f"{state.upper()}: {message[:1].upper()}{message[1:]}")
    return EXIT_CODES[state]


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1] if len(sys.argv) > 1 else "gate"))
