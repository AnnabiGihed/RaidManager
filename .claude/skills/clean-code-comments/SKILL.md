---
name: clean-code-comments
description: Clean Code rules for comments in C#, configuration and pipeline files. Use whenever you write, keep or remove a comment, a TODO/FIXME/HACK, an XML doc comment, or review comments in a pull request. Covers why-not-what, when a comment is required (decisions, workarounds, regulatory sources), forbidden comments (restating code, commented-out code, change history, section dividers, personal data), the TODO format, and how XML documentation is written.
---

# Comments and self-documenting code

Source: Development Standards Wiki → *Clean Code* → *Comments and Self-Documenting Code*. Cite rules as
`Comments and Self-Documenting Code / R<n>`. Severity markers: 🔴 always, 🟡 by default (deviation justified in the PR),
🟢 when asked; *Analyzer* rules fail the build, *Review* rules are yours to check.

**Precedence.** `csharp-xml-documentation` and `csharp-regions` take precedence over this page (both codify
Pivot.Framework's style). Where a rule is marked *Adjusted*, follow the adjusted text:
- The type header (`Author / Date / Purpose` inside `<summary>`) is required and is **not** an author banner (R10).
- `#region` blocks are required and are **not** section dividers (R11).
- Summaries on non-private members are mandatory and must add information — never omit one (R13, R18).

## 1. Default: make the code say it
- 🔴 **R1.** Never restate what the code does. Fix the name or extract a method/predicate instead. *Review.*
- 🔴 **R2.** A comment must be true. Correct or delete contradicted comments in the same PR, whoever wrote them. *Review.*
- 🟡 **R3.** Prefer a named constant, predicate, value object or enum member over a comment. *Review.*

## 2. Comments you must write
- 🔴 **R4.** When code encodes a decision a reader might reverse: a workaround, a deliberate deviation, an ordering constraint, a performance trade-off. *Review.*
- 🔴 **R5.** When a business rule comes from an external source: cite the regulation, ticket or ADR. *Review.*
- 🟡 **R6.** Third-party defect workaround: link the upstream issue and when it can be removed. *Review.*
- 🟡 **R7.** Code that looks redundant but is needed (apparent no-op, deliberate ordering, intentionally empty implementation). *Review.*
- 🟢 **R8.** A short strategy note before a genuinely intricate algorithm. *Review.*

Pivot examples of R4/R7 done right: `IDomainEventHandler` ("Result is intentionally ignored here to comply with MediatR's
notification contract"), `RabbitMQReceiver` (why unknown types are nacked without requeue), `HangfireCookieDashboardAuthorizationFilter`
(why it returns `true` after redirecting).

```csharp
// The SLA clock is suspended while the operator has an outstanding request for
// information: those working days are not counted against the agency.
// AR/KB 2016-03-14, art. 12 §2. See ADR-0009.
var effectiveDays = totalWorkingDays - suspendedWorkingDays;
```

## 3. Forbidden comments
- 🔴 **R9.** Commented-out code. Delete it; Git is the archive. *Analyzer:* S125.
- 🔴 **R10.** Change history, "modified by X on date", in-file change logs, file-header banners. Git and `CHANGELOG.md` own history. *Review,* SA1633 off.
  *Adjusted:* the type header required by `csharp-xml-documentation` records **creation** only — never add "modified by" lines or update Author/Date later.
- 🔴 **R11.** Dividers such as `//======== HELPERS ========`. *Review.*
  *Adjusted:* use `#region` exactly as `csharp-regions` defines; a type that still feels too large inside its regions should be split (`clean-code-types` R1).
- 🔴 **R12.** Personal data, credentials, internal host names, remarks about colleagues or users. *Review,* secret scanning.
- 🟡 **R13.** XML docs that only repeat the member name. *Adjusted:* never remove a required summary to satisfy this; rewrite it to add information (what it refers to, valid values, units, why it exists).

## 4. TODO, FIXME, HACK
- 🔴 **R14.** Format: `// TODO(<author>, <YYYY-MM-DD>, <work item>): <what and why>`. Author from `git config user.name` (short form); ask for the work item if missing — never write a TODO without it. *Analyzer:* S1135.
- 🔴 **R15.** `FIXME`/`HACK` use the same format and also state the consequence of leaving it. *Analyzer:* S1134.
- 🔴 **R16.** Never leave a TODO for work that belongs to the current change. TODOs are only for pre-existing debt found along the way. *Review.*
- 🟡 **R17.** TODOs older than two releases are closed or re-ticketed in a quarterly sweep.

```csharp
// TODO(gihed, 2026-09-29, PIVOT-42): remove once DomainEvent/IntegrationEvent preserve Id through
// Newtonsoft round-trips. Until then this event declares a [JsonConstructor] so inbox dedup keys stay stable.
```

## 5. XML documentation
Which members must be documented, the header format and wording are defined by `csharp-xml-documentation`. These rules govern the writing:
- 🔴 **R18.** Visible types and members carry XML docs and the build fails otherwise (CS1591 as error). *Adjusted:* extends to internal members and test code per `csharp-xml-documentation` §2.
- 🔴 **R19.** `<summary>` states what the member does and the domain constraint it enforces, not the mechanics. *Review.*
- 🟡 **R20.** `<param>`, `<returns>`, `<exception>` cover what callers can't infer: units, ranges, time zone (Pivot: always UTC), empty collections, which `Error` codes / `ResultExceptionType` a `Result` failure carries. *Review.*
- 🟡 **R21.** Cross-reference with `<see cref="…"/>`. *Analyzer:* CS1574.

## 6. Tests, configuration and infrastructure
- 🟡 **R22.** Tests express intent through name and arrange/act/assert; comment only to cite a real-world case (defect ticket).
- 🔴 **R23.** In configuration, pipelines and IaC, comment every non-self-evident value (ports, retention periods, SKUs, `RevokeAllTtl` = realm SSO Session Max, 32-char `EncryptionKey` requirement).
- 🟡 **R24.** No commented-out steps in pipelines/configuration.

## 7. Checklist
- [ ] No comment restates code; no stale comments near your change.
- [ ] Workarounds, deviations, ordering constraints and external rules are explained and cited.
- [ ] No commented-out code, history lines, banners, dividers or personal data; the required type header and regions are present.
- [ ] Every TODO/FIXME/HACK has author, date, work item (and consequence), and none is for the current deliverable.
- [ ] Summaries describe the contract; non-obvious config values are commented.
