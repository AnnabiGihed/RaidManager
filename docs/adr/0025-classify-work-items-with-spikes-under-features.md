# ADR-0025: Classify work items by intent, with spikes under features

- Status: Proposed
- Date: 2026-10-02
- Deciders: Gihed Annabi
- Supersedes: [ADR-0016](0016-epic-feature-story-task-hierarchy.md)

## Context

[ADR-0016](0016-epic-feature-story-task-hierarchy.md) put every work item in one chain, but it placed a spike beside
tasks under a story, improvement or bug. Research then had to borrow a parent whose outcome it hadn't chosen yet, and
it couldn't be split into tasks. The guard also skipped open issues without a type label, so an item could stay
standalone without being flagged. The owner restated the rule on 2026-10-02: no item is ever standalone, and work is
classified by intent first, then scope. Not following it is a major failure.

## Decision

### Hierarchy

```text
Epic
└── Feature
    ├── User Story ── Task
    ├── Improvement ── Task
    ├── Bug ── Task
    └── Spike ── Task
```

| Type | Label | Parent | Allowed children |
| --- | --- | --- | --- |
| Epic | `type:epic` | none | features |
| Feature | `type:feature` | an epic | user stories, improvements, bugs, spikes |
| User Story | `type:story` | a feature | tasks |
| Improvement | `type:improvement` | a feature | tasks |
| Bug | `type:bug` | a feature | tasks |
| Spike | `type:spike` | a feature | tasks |
| Task | `type:task` | a user story, improvement, bug or spike | none |

Every issue carries exactly one type label and, except an epic, a parent of the allowed type on the same
milestone, so a milestone view never shows a child without its parent. Nothing is standalone.
A task describes how bounded work is done and verified; its parent says why the work exists: deliver a capability,
enhance something, correct a defect, or answer a research question.

### Classification

Classify by intent first, then scope.

| Type | Identifying question | Expected result |
| --- | --- | --- |
| User Story | Does a user need a capability they can't perform today? | A new, independently verifiable user capability |
| Improvement | Does existing behavior, tooling, documentation or process need to become better? | A verifiable enhancement |
| Bug | Does actual behavior contradict agreed requirements or intended behavior? | Correct behavior restored |
| Spike | Must we resolve an unknown before choosing a direction or implementing? | Evidence, findings or a recommendation |
| Feature | Are several related outcomes needed to deliver one coherent capability? | A capability delivered through its child items |
| Epic | Are several features needed to achieve a broader user or business objective? | An objective achieved across features |

**Choose a user story** when you can name an actor, a capability they lack and its benefit; acceptance criteria
describe observable user behavior. Not an improvement (the capability is missing), not a bug (no agreed requirement is
violated), not a spike (it delivers usable behavior), not a feature (it is one verifiable outcome), not an epic. For
example: an officer can create a raid event from Discord.

**Choose an improvement** when existing behavior, tooling, documentation or process works as specified but needs a
verifiable enhancement. Not a bug (it meets its requirements), not a spike (the enhancement and its success criteria
are understood), not a user story (it improves something that exists), not a feature (it is one bounded
enhancement), not an epic. For example: fewer steps to sign up for an existing raid event.

**Choose a bug** when actual behavior differs from agreed or intended behavior; describe expected and actual
behavior, the steps to reproduce, and how the fix is verified. Not an improvement (the behavior is wrong, not merely
inconvenient), not a spike (the defect is established, even if its cause isn't), not a user story, not a feature,
not an epic. If the expected behavior was never agreed, clarify it before calling it a bug. For example: a player
signs up once but appears twice in the roster.

**Choose a spike** when the main deliverable is knowledge needed to make a decision; state a specific question, a
time box and exit criteria. Not an improvement (it decides what should change), not a bug (it investigates rather
than promises a fix), not a user story (it produces evidence, not a capability), not a feature, not an epic. A spike
belongs directly to a feature. Its tasks own the bounded activities that produce evidence, such as comparing
candidates, a disposable proof of concept, tests, and a written recommendation; the spike owns the question and the
conclusion. It can inform sibling stories, improvements or bugs through dependency links, and its conclusion may
recommend implementation, more research, or abandoning an approach. Research succeeds without finding a viable
solution.

**Choose a feature** when one coherent capability needs several related stories, improvements, bugs or spikes. Not
an improvement or a bug (it groups delivery rather than one change or failure), not a spike (its exit criteria need a
delivered capability), not a user story (several verifiable outcomes sit under it), not an epic (it is one
capability). For example: Discord raid planning, covering event creation, signup, withdrawal and roster viewing.

**Choose an epic** when a broad user or business objective needs several features; define its boundaries and
observable exit criteria. Not an improvement, bug, spike, user story or feature: one enhancement, defect, finding,
outcome or capability can't satisfy it. For example: communities organize raids through RaidManager.

### Completion and pull requests

- An epic, feature, user story, improvement, bug or spike closes as completed only when at least one child of the
  level below is completed and every child is closed. A spike therefore closes with at least one completed task, such
  as the one recording its findings.
- A pull request closes tasks only (`Closes #N`), and each task must reach an epic through its parent and a feature.
  Branches and commits carry the task's number. Other items are referenced with `Refs #N`.
- Items closed as *not planned* or *duplicate* are abandoned and exempt.

### Enforcement

- The issue forms state the chain and each type's identifying question, and ask for the right parent; a spike's form
  also asks for its time box. Blank issues stay disabled.
- The `project-hierarchy` workflow labels an issue `needs-parent` with one comment when it has no type label, more
  than one, a missing parent or a parent of the wrong type, and reopens an early completion. It checks every changed
  issue and audits the board every 15 minutes.
- The docs `validate` check and the `review` workflow reject a pull request that closes anything but a task reaching
  an epic.
- The `raidmanager-github-project-workflow` and `raidmanager-conventions` skills, and `AGENTS.md`, make the
  hierarchy and the classification mandatory for every agent session.

### Migration

- Spikes #37, #40 and #103 move from their stories and improvement to those items' features.
- Closed spike #40 gets a completed task recording its research.
- Test issue #118, which has no type label, leaves the Project.

## Consequences

**Positive**

- Research has its own place under the capability it serves, and its work is tracked as tasks like any other.
- An issue without a type label, or a standalone one, is flagged within 15 minutes.
- One classification guide decides the type, so the same work isn't filed as a story by one person and an
  improvement by another.

**Negative**

- A spike needs at least one task before it can close, even for short research.
- Choosing between an improvement and a bug can need the owner's answer on what was agreed.

## Alternatives considered

- **Keep spikes as tasks under a story (ADR-0016):** fewer items, but research had to pick an outcome before it knew
  one, and it couldn't be split.
- **Let a spike close without tasks:** simpler for short research, but the owner chose one rule for every type.
- **Exempt open issues without a type label:** less noise, but it leaves a way to create standalone work.
