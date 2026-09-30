# ADR-0020: Fail pull requests on SonarCloud findings

- Status: Proposed
- Date: 2026-09-30
- Deciders: Gihed Annabi

## Context

The owner connected the repository to SonarCloud. SonarCloud's automatic analysis runs on every pull request with no
scanner or token in the workflows, comments its findings on the changed lines, and publishes a
`SonarCloud Code Analysis` check.

Two pull requests merged with findings anyway: eight duplicated-literal code smells in #204, and two more that the
owner spotted on #205 before merging. The check passed on both, for two reasons:

- The check follows the project's quality gate. The default gate only fails on ratings, coverage, duplication and
  hotspot review. A code smell leaves the maintainability rating at A, so the gate passes, and so would a new
  vulnerability that doesn't lower the security rating.
- The check isn't required on `main`, so even a failing gate wouldn't stop a merge.

This is the same trap as the Vale step fixed in #174: findings shown as annotations while the check stays green.

## Decision

- **Any SonarCloud finding fails the pull request.** A `sonar` job in `ci.yml` runs
  `scripts/sonar_gate.py` on every pull request:
  - It waits for SonarCloud's analysis of the head commit, up to 15 minutes.
  - It then reads the pull request's open issues (bugs, vulnerabilities, code smells, any severity) and its
    security hotspots to review from SonarCloud's public web API.
  - It fails when there is at least one, with an error annotation on each file and line.
  - It also fails when SonarCloud hasn't analyzed the head commit in time, so a missing analysis never passes.
- **`sonar` is a required check on `main`,** next to `build-test`, `validate` and `review-gate`.
- **Fix every finding in the same pull request.** A finding that is genuinely wrong is marked as a false positive
  or accepted in SonarCloud, one at a time, with a reason that names the pull request. Findings are never
  bulk-resolved to turn the check green.
- **The rule lives in the repository.** The check reads SonarCloud's findings directly instead of relying on a
  quality gate configured in SonarCloud's settings, so the rule is reviewed and versioned like the code. The
  project is public, so the check needs no token.
- **Findings already on `main` are fixed separately** (#209). The check only sees the pull request's own findings,
  so older ones don't block unrelated work.

## Consequences

**Positive**

- A finding either gets fixed or gets a recorded reason before merge; nothing merges silently.
- No secret is added: the analysis is SonarCloud's automatic analysis, and the check reads a public API.

**Negative**

- Every pull request waits for SonarCloud, usually one to two minutes. If SonarCloud is down, pull requests can't
  merge until it recovers or the check is rerun.
- Minor findings also block. That is intended: fixing them is usually quicker than discussing them.

## Alternatives considered

- **A custom quality gate with "no new issues" in SonarCloud:** keeps the rule in SonarCloud's settings, outside
  review, and still needs the check to be required.
- **Make the existing `SonarCloud Code Analysis` check required as it is:** the default gate passed both pull
  requests, so requiring it changes nothing.
- **Run Sonar's scanner in CI with a token:** needed for coverage import later, but doesn't change what the gate
  checks, and adds a secret.
