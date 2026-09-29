---
name: gherkin-scenarios
description: Strict, non-negotiable rules for writing and reviewing Gherkin .feature files and their Reqnroll step definitions. Use whenever you create, edit or review any .feature file, any scenario, or any [Given]/[When]/[Then] binding. Fixes file layout, keywords, step grammar, parameters, tables, tags, formatting and binding style so no choice is left to the author.
---

# Gherkin Scenarios

**Scope.** These rules govern business-behaviour tests of services built on Pivot.Framework (see `dotnet-unit-tests` for when Gherkin applies). The Pivot.Framework repository itself has no Reqnroll tests — its technical tests follow `pivot-testing`.

These rules are mandatory and exhaustive. Every rule uses MUST or MUST NOT. There are no optional rules, no "prefer", and no "where appropriate". If a situation is not covered here, or following a rule would contradict the user's request, **stop and ask the user**. Never decide on your own.

## 1. Files

1. One `Feature` per file.
2. The file name MUST be the Feature title in PascalCase with non-alphanumeric characters removed, plus `.feature`. `Feature: Attribute import job tracking` → `AttributeImportJobTracking.feature`.
3. The file MUST sit in the test project under `Features/`, mirroring the namespace path of the production code under test. Code in `SAM.Application.Features.AttributeTemplates` → `SAM.Application.UnitTests/Features/AttributeTemplates/AttributeImportJobTracking.feature`.
4. Encoding MUST be UTF-8 without BOM. Line endings MUST match the repository's `.gitattributes`; if none is set, use CRLF.
5. The file MUST end with exactly one newline. No trailing whitespace on any line.
6. The `# language:` header MUST NOT be used. All text MUST be in English.
7. You MUST NOT edit the generated `*.feature.cs` file.

## 2. File skeleton

Every feature file MUST have exactly this shape, in this order:

```gherkin
@workitem:<id>
Feature: <Feature title>
	As a <role>
	I want <capability>
	So that <benefit>

	Background:
		Given <shared precondition>

	Rule: <Rule statement>

		Scenario: <Scenario title>
			Given <precondition>
			When <action>
			Then <outcome>
```

- `Background` appears only when rule 5.1 requires it.
- Every `Scenario` and `Scenario Outline` MUST be inside a `Rule`. No scenario may sit directly under `Feature`.
- A Feature MUST contain 1 to 5 Rules. A Rule MUST contain 1 to 6 scenarios. If either limit would be exceeded, split the feature into a second file whose title names the narrower capability.

## 3. Allowed keywords

| Keyword | Status |
| --- | --- |
| `Feature`, `Rule`, `Background`, `Scenario`, `Scenario Outline`, `Examples`, `Given`, `When`, `Then`, `And` | Allowed |
| `But`, `*`, `Scenario Template`, `Example`, `Scenarios` | Forbidden |
| `Background` inside a `Rule` | Forbidden |

## 4. Titles and narrative

1. **Feature title**: sentence case (only the first word and proper nouns capitalized), a noun phrase naming the capability, no trailing period, at most 60 characters. Example: `Attribute import job tracking`.
2. **Narrative**: exactly three lines, `As a`, `I want`, `So that`, in that order. The role MUST be a real user role from the domain (`data steward`, `inspector`). If the feature is purely technical, the role MUST be `As a consumer of <component name in lowercase words>`. First person (`I`) is allowed only in the `I want` line.
3. **Rule statement**: a declarative sentence in present simple stating the business rule, sentence case, no trailing period, at most 80 characters. Example: `A completed import job keeps every rejected row`.
4. **Scenario title**: sentence case, no trailing period, at most 80 characters, starts with the subject, states the outcome in present simple. It MUST be unique within the file. Example: `Completed job lists its rejected rows`.
5. Titles MUST NOT contain the words `test`, `verify`, `check`, `should`, `scenario`, `case`, `correctly`, `properly`, `successfully`.

## 5. Scenario structure

1. **Background**: required when, and only when, every scenario in the file starts with the same one or more `Given` steps with identical text and values. Those steps MUST then move into a Feature-level `Background` and be removed from every scenario. A Background MUST contain only `Given` and `And`.
2. Every scenario MUST follow this exact sequence: one `Given`, zero to three `And` continuing the Given, exactly one `When`, one `Then`, zero to three `And` continuing the Then.
3. A scenario MUST have a `Given` even if a `Background` exists. The only exception is a scenario whose preconditions are entirely in the Background.
4. A scenario whose subject does not exist MUST still have a `Given` stating that: `Given no import job exists`.
5. The sequence MUST NOT go backwards. No `Given` after a `When`, and no `When` after a `Then`.
6. A scenario MUST contain at most 9 steps including `And` steps. If more are needed, split the scenario.
7. `Scenario Outline` MUST be used when, and only when, two or more scenarios in the same Rule have identical step text differing only in values. Such scenarios MUST be merged into one outline.
8. A `Scenario Outline` MUST have exactly one `Examples:` block, unnamed, with at least two data rows.

## 6. Step grammar

Every step describes one fact or one action. Apply these rules to the step text after the keyword.

1. **Given** states a precondition as a fact in present simple or present perfect: `an import job exists for a file of 1000 bytes`, `the import job has completed with the rejected row "Invalid row"`.
2. **When** states exactly one action in present simple, active voice, with the actor as subject: `the data steward opens the import job`. For technical features with no human actor, use passive voice with the system object as subject: `the import job is mapped`.
3. **Then** states exactly one observable outcome in present simple: `the import job status is "Completed"`. It MUST NOT use `should`, `must`, `will`, or `can`.
4. A step MUST NOT contain the word `and` or a comma joining two facts. Use a separate `And` step.
5. A step MUST be at most 120 characters.
6. A step MUST start with a lowercase letter unless it starts with a proper noun.
7. A step MUST NOT end with punctuation.
8. A step MUST NOT use first or second person: `I`, `me`, `my`, `we`, `our`, `you`, `your`.
9. A step MUST NOT contain implementation details: type or method names, PascalCase identifiers, namespaces, HTTP verbs or status codes, URLs, SQL, JSON field names, CSS selectors, file paths, or the words `click`, `button`, `page`, `field`, `endpoint`, `API`, `database`, `repository`, `mock`, `null`, `true`, `false`.
10. A step MUST use domain vocabulary in lowercase words: `import job`, `attribute template`, `data steward`.
11. Articles: use `a`/`an` when introducing a thing for the first time in the scenario, `the` for every later reference to it.

## 7. Values and parameters

1. Every value that could differ between scenarios MUST be a parameter. No value may be hard-coded into step definition code if it appears in the scenario's meaning.
2. **Whole numbers from −2,147,483,648 to 2,147,483,647**: written unquoted, bound with `{int}`.
3. **Every other value** (text, enum values, decimals, dates, identifiers, numbers outside that range): written in double quotes, bound with `{string}`.
4. Dates MUST use `yyyy-MM-dd`. Date-times MUST use `yyyy-MM-ddTHH:mm:ssZ` in UTC. Decimals MUST use `.` as separator.
5. Enum values MUST be written exactly as the enum member name: `"Completed"`.
6. A boolean state MUST be expressed as two distinct words in quotes, never `true`/`false`: `"active"` / `"inactive"`.
7. **Outline placeholders** MUST be camelCase, `<totalBytes>`, and MUST be quoted in the step exactly when rule 7.3 applies to their values.
8. **Examples tables**: header names MUST be the placeholder names in the order they first appear in the steps. Every column MUST be used by at least one step.

## 8. Data tables and doc strings

1. A step that needs to describe **three or more attributes of one thing** MUST use a vertical data table with header `| field | value |`, one attribute per row, field names in lowercase words.
2. A step that describes **two or more things of the same kind** MUST use a horizontal data table with a header row of lowercase field names and one row per thing.
3. A step with fewer attributes than rule 8.1 requires MUST NOT use a table.
4. Doc strings (`"""`) MUST be used only for multi-line text or payloads, and MUST declare the content type: `"""json`, `"""text`.
5. Table cells MUST follow the quoting-free form of rule 7: no quotes inside cells.

## 9. Tags and comments

1. The Feature MUST carry exactly one tag, `@workitem:<id>`, with the work item number from the tracker (Azure DevOps, GitHub or Jira). If the id is not given in the task or linked issue, ask the user. Never invent one.
2. The only tag allowed on a Scenario or Scenario Outline is `@ignore`. It MUST be preceded on the line above by the comment `# Ignored: <reason> (workitem <id>)`.
3. No other tags are allowed on any element. Rules and Examples MUST NOT be tagged.
4. No other comments are allowed anywhere in the file.

## 10. Formatting

1. Indentation MUST use tabs, one tab per level:

| Element | Tabs |
| --- | --- |
| Feature tag, `Feature:` | 0 |
| Narrative lines, `Background:`, `Rule:` | 1 |
| Background steps, Scenario tag, `@ignore` comment, `Scenario:`, `Scenario Outline:` | 2 |
| Scenario steps, `Examples:` | 3 |
| Data table rows under a step, doc strings, Examples table rows | 4 |

2. Exactly one blank line MUST separate: the narrative from `Background`/the first `Rule`; `Background` from the first `Rule`; each `Rule` line from its first scenario; consecutive scenarios; consecutive Rules; the last step of an outline from `Examples:`.
3. There MUST be no blank lines between steps, between `Examples:` and its table, or between table rows.
4. Table pipes MUST be column-aligned: one space after each `|`, each cell padded with spaces to the width of the longest cell in its column. Numbers are left-aligned like text.
5. Keywords MUST be followed by exactly one space (`Given a`, `Rule: A`, `Scenario: Completed`).

## 11. Step definitions (Reqnroll)

1. Before writing a new step, search all `*StepDefinitions.cs` files for an existing binding whose expression matches the step. If one exists, you MUST reuse its exact text.
2. Bindings MUST use Cucumber expressions with only `{int}` and `{string}`. Regular expressions, `{word}`, `{float}`, `{}` and alternation (`active/inactive`) are forbidden.
3. Step definitions for a feature MUST live in `<FeatureFileName>StepDefinitions.cs` next to the feature file, in a `sealed` class marked `[Binding]` and `[Scope(Feature = "<exact Feature title>")]`.
4. Steps reused by more than one feature MUST live in `Common<Area>StepDefinitions.cs` in the nearest common folder, without `[Scope]`. When a scoped step becomes needed by a second feature, move it there.
5. Each method MUST bind exactly one attribute (`[Given]`, `[When]` or `[Then]`). Multiple attributes on one method are forbidden.
6. `[Given]` methods MUST only arrange state. `[When]` methods MUST only perform the action and store its result. `[Then]` methods MUST only assert, and MUST assert the single outcome their step states.
7. Scenario state MUST be held in private fields of the step definition class. `ScenarioContext` dictionary access (`ScenarioContext["key"]`) is forbidden.
8. `{string}` parameters that represent enums, booleans, dates, decimals (money, prices) or large numbers MUST be converted with a `[StepArgumentTransformation]` method or `Enum.Parse`/`DateTime.ParseExact`/`long.Parse` using `CultureInfo.InvariantCulture`.
9. Method names MUST be the keyword followed by the step text in PascalCase, without parameters: `Given an import job exists for a file of {int} bytes` → `GivenAnImportJobExistsForAFileOfBytes`.
10. The class MUST follow the `csharp-regions` skill (`Fields` → `Constructors` → `Given Steps` → `When Steps` → `Then Steps` → `Step Argument Transformations` → `Private Helpers`, bare `#endregion`) and the `csharp-xml-documentation` skill (Pivot-style `Author / Date / Purpose` header).
11. Assertions use **Shouldly**; collaborators are **Moq** mocks from the shared mock factories; step methods that call async code are `async Task` — never `.Result`/`.Wait()` (see `dotnet-unit-tests`).
12. Failure outcomes on Pivot `Result`s assert the **exact** `Error.Message`, copied character for character from the domain's message catalogue — Pivot's `BaseDomainErrors` messages contain irregular spacing (e.g. `Value  PRD-0001 for product sku  already exists !`), which MUST be preserved inside the quoted value.

## 12. Procedure

When asked to write or change scenarios, perform these steps in order and do not skip any:

1. Identify the work item id. If missing, ask.
2. Search existing `.feature` files for a feature covering the same capability. If one exists, add to it; do not create a second file.
3. List the business rules. Write one `Rule` per rule.
4. Write one scenario per distinct outcome of each rule.
5. Apply rule 5.7: merge value-only variants into outlines.
6. Apply rule 5.1: extract a Background if required.
7. Search existing bindings (rule 11.1) and reuse matching step text.
8. Write new bindings for the remaining steps.
9. Run the checklist below. Fix every failure before finishing.

## 13. Example

```gherkin
@workitem:48213
Feature: Attribute import job tracking
	As a data steward
	I want each attribute import to report its progress and rejected rows
	So that I can correct rejected rows without re-reading the file

	Background:
		Given a data steward is signed in

	Rule: An import job status reflects its progress

		Scenario Outline: Job status follows processed bytes
			Given an import job exists for a file of <totalBytes> bytes
			And the import job has processed <processedBytes> bytes
			When the data steward opens the import job
			Then the import job status is "<status>"

			Examples:
				| totalBytes | processedBytes | status     |
				| 1000       | 0              | Pending    |
				| 1000       | 500            | InProgress |

	Rule: A completed import job keeps every rejected row

		Scenario: Completed job lists its rejected rows
			Given an import job exists for a file of "5000000000" bytes
			And the import job has completed with these rejected rows
				| row | reason        |
				| 12  | Missing value |
				| 48  | Invalid date  |
			When the data steward opens the import job
			Then the import job lists 2 rejected rows

		Scenario: Missing job reports that it does not exist
			Given no import job exists
			When the data steward opens an import job
			Then the data steward sees that the import job does not exist
```

## 14. Checklist

Every item MUST pass before you finish.

- [ ] File name, location, encoding and final newline follow section 1.
- [ ] Feature has exactly one `@workitem:<id>` tag and a three-line narrative.
- [ ] Every scenario is inside a Rule; Rule and scenario counts are within limits.
- [ ] Only allowed keywords are used; no `But`, no `*`, no Rule-level Background.
- [ ] Background exists if and only if all scenarios share their leading Given steps.
- [ ] Each scenario is Given → When → Then, one When, at most 9 steps.
- [ ] No two scenarios in a Rule differ only by values.
- [ ] Every step is one fact, present tense, third person, no `should`, no implementation words, no trailing punctuation.
- [ ] Integers in range are unquoted; every other value is quoted.
- [ ] Tables and doc strings follow section 8.
- [ ] No tags other than `@workitem` and justified `@ignore`; no other comments.
- [ ] Indentation, blank lines and table alignment follow section 10.
- [ ] Bindings use only `{int}` and `{string}`, are scoped or shared per section 11, and each `[Then]` asserts one outcome.
