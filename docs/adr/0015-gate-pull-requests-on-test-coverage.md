# ADR-0015: Gate pull requests on test coverage

- Status: Proposed
- Date: 2026-09-30
- Deciders: Gihed Annabi

## Context

Every test project references `coverlet.collector`, but CI never collected coverage, so nobody could see which code
the tests exercise. Measured on `main`, product code has 81% line coverage once tests, migrations and generated code
are excluded. The domain project is the weakest, at 74%. A gate on the total alone would let a pull request add
untested code as long as the total stays above the bar, and a high total bar would block unrelated work on old gaps.

## Decision

CI measures line coverage on every run and enforces two rules on pull requests:

- **Changed lines:** at least 80% of the executable source lines a pull request adds or changes under `src/` are
  covered. Declarations, blank lines and comments don't count. A pull request that changes no executable line
  passes this rule.
- **Total:** line coverage of all product code stays at or above 60%.

`coverage.runsettings` excludes test projects, EF Core migrations, files under `obj/` and code marked
`[GeneratedCode]` or `[ExcludeFromCodeCoverage]`, and skips auto-implemented properties. `scripts/coverage_gate.py`
merges the reports, computes both numbers from the pull request's diff, and writes a summary. That summary lists the
uncovered changed lines and any project that no test loads. CI writes it to the run page, keeps it as one comment on
the pull request, and uploads the reports. `[ExcludeFromCodeCoverage]` needs a comment explaining why the code can't
be tested.

## Consequences

**Positive**

- New code arrives with tests, and reviewers see exactly which changed lines aren't covered.
- The gate needs no external service or token: the built-in `GITHUB_TOKEN` posts the comment.

**Negative**

- Line coverage says a line ran, not that a test checked its result. Reviewers still judge the assertions.
- A project that no test loads has no coverage data. The summary names it, but the total leaves it out.

## Alternatives considered

- **Total minimum only:** simpler, but a pull request can add untested code while the total stays above the bar.
- **Report only:** gives visibility, but nothing stops untested code from merging.
- **A hosted service (Codecov, SonarCloud):** adds an account, a token and a third party for what the build can
  compute itself.
