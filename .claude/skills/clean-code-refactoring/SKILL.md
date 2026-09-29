---
name: clean-code-refactoring
description: FAVV-AFSCA Clean Code rules for code smells and refactoring. Use when reviewing code (to name smells precisely), when asked to refactor, clean up, restructure or remove duplication, or when you notice a smell in legacy code while making another change. Covers the smell catalogue (bloaters, OO abusers, change preventers, dispensables, couplers), the rule of three, behavior-preserving refactoring in separate commits under test coverage, characterization tests, refactoring PR descriptions, and logging technical debt instead of fixing it opportunistically.
---

# Code Smells and Refactoring

Source: Development Standards Wiki → *Clean Code* → *Code Smells and Refactoring*. Cite rules as `Code Smells and Refactoring / R<n>`. Severity and *Analyzer*/*Review* markers work as in `clean-code-naming`.

## 1. Naming smells in review

- 🔴 **R1.** Name the smell and its evidence; never a preference. "Feature envy: `SlaCalculator` reads six members of `Dossier` and none of its own." not "this feels off". *Review.*
- 🟡 **R2.** Use the catalogue names from refactoring.guru/refactoring/smells so the remedy is searchable. *Review.*
- 🟡 **R3.** Prefix review comments: `blocking:` when the smell will cause a defect or is in new code; `suggestion:` or `nit:` when pre-existing (worth a ticket). *Review.*

## 2. Smell catalogue

| Rule | Smell | Signal | Remedy | Enforced by |
| --- | --- | --- | --- | --- |
| 🔴 R4 | Long method | Complexity/length thresholds exceeded | `clean-code-functions` R1-R6 | S138, S3776, S1541 |
| 🔴 R5 | Large / God class | Many members, many dependencies | Split by responsibility (`clean-code-types` R1-R3) | S1448, S1200 |
| 🔴 R6 | Long parameter list | Parameter threshold exceeded | Parameter object / value object | S107 |
| 🔴 R7 | Primitive obsession | Raw `string`/`int`/`Guid` for domain values | Value objects (`clean-code-types` R7-R8) | Review |
| 🟡 R8 | Data clump | Same values passed together in 3+ places | Introduce a type | Review |
| 🔴 R9 | Switch on type | Same type switch in more than one place | Polymorphism or one mapping table | Review, S1479 |
| 🟡 R10 | Temporary field | Field meaningful only during one operation | Parameter or local | Review |
| 🟡 R11 | Refused bequest | Subclass overrides inherited members to throw | Composition | S3877 |
| 🟡 R12 | Alternative classes, different interfaces | Two types, same job, different names | One abstraction | Review |
| 🔴 R13 | Shotgun surgery | One business change edits many unrelated files | Consolidate the knowledge in one place | Review |
| 🟡 R14 | Divergent change | One class edited for unrelated reasons | Split | Review |
| 🟡 R15 | Parallel inheritance | New subclass forces another subclass | Collapse hierarchies | Review |
| 🔴 R16 | Duplicated code | Duplication on new code; third occurrence | Extract (rule of three) | SonarCloud, S4144, S1871 |
| 🔴 R18 | Dead code | Unreachable code, unused private/internal members and parameters | Delete | S1144, S1172, S1481, S3626, CA1508 |
| 🔴 R19 | Commented-out code | Code in comments | Delete | S125 |
| 🟡 R20 | Speculative generality | Interface with one implementation, unused flag, factory for one type | Remove until the second case exists | Review |
| 🟡 R21 | Lazy class | Class no longer earning its existence | Inline | Review |
| 🔴 R22 | Feature envy | Method uses another type's data more than its own | Move behavior to the data | Review |
| 🔴 R23 | Message chains | `a.B.C.D.Do()` | Tell, don't ask | Review |
| 🟡 R24 | Middle man | Class only delegates | Remove, unless it inverts a dependency or translates layers (then document why) | Review |
| 🟡 R25 | Inappropriate intimacy | Two classes reach into each other's internals | Merge or extract shared knowledge | Review |

- 🔴 **R17.** Never remove duplication by extracting a shared abstraction over things that merely look alike, especially across bounded contexts. Extract only when they are shown to change together. *Review.*

## 3. Refactoring discipline

- 🔴 **R26.** A refactoring preserves behavior. A change to observable behavior is never labelled `refactor`. *Review,* `refactor:` commit type.
- 🔴 **R27.** Never mix refactoring and behavior change in one commit. When asked to do both, produce separate commits: first the structural move, then the behavior change with its test. *Review.*
- 🔴 **R28.** Never refactor without tests covering the preserved behavior. If coverage is missing, write characterization tests that pin the **current** behavior first, in their own commit, even where that behavior looks wrong; log suspected defects as separate tickets. *Review.*
- 🔴 **R29.** A refactoring PR description states what was preserved and the evidence. *Review.*
- 🟡 **R30.** Boy Scout rule within scope: renaming a misleading local in the method you edit is welcome; reformatting or restructuring the rest of the file is not. *Review.*
- 🟡 **R31.** Large restructurings are a sequence of small, mergeable, revertible steps; replace subsystems strangler-fig style. *Review.*
- 🟡 **R32.** Commit automated refactorings (rename, extract, move) separately from hand-written changes. *Review.*

Refactoring PR description template:

```text
refactor(<scope>): <what moved> (<work item>)

## What changed
<Structural change. State explicitly: no rule/behavior was modified.>

## Why it changed
<Reason, with work item.>

## How it was tested
<Which existing tests pass unmodified; which tests moved without changing arrange/assert.>

## What to review carefully
<Any place where copies disagreed and which behavior was kept; follow-up tickets for behavior changes.>
```

## 4. Debt you do not fix now

- 🔴 **R33.** Never fix a smell in legacy code opportunistically in an unrelated change. Tell the user and propose a technical-debt work item referencing the file and rule ID. *Review.*
- 🟡 **R34.** A debt item states the cost of leaving it: which future change it slows down. *Review.*
- 🟡 **R35.** Prioritize hotspots (high complexity and high change frequency, from SonarCloud) over merely ugly files. *SonarCloud hotspot report.*

Debt item template:

```text
Title: <Smell> in <File/Type> (<Page> / R<n>)
Observation: <evidence>
Cost of leaving it: <which change it slows or which defect it risks>
Suggested remedy: <catalogue refactoring>
```
