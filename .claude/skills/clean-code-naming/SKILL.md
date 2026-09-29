---
name: clean-code-naming
description: FAVV-AFSCA Clean Code naming rules for C#. Use whenever you name or rename a type, member, variable, parameter, constant, test or file, or review names in a pull request. Covers casing, one type per file, the food safety domain vocabulary (French terms kept), one name per concept, verb semantics (Get/Find/Calculate/Create/Ensure), abbreviations and acronyms, booleans, Async suffix, collections, generics, magic numbers and strings, and test names.
---

# Naming and Vocabulary

Source: Development Standards Wiki → *Clean Code* → *Naming and Vocabulary*. Cite rules as `Naming and Vocabulary / R<n>`. 🔴 always applies; 🟡 applies by default and is only deviated from when the user asks, with the reason stated in the PR; 🟢 applies when asked. Rules marked *analyzer* fail the build, so code you write must satisfy them; rules marked *review* are yours to check before finishing.

## 1. Casing and layout

- 🔴 **R1.** `PascalCase` for types, methods, properties, events, constants; `camelCase` for parameters and locals; `_camelCase` for private fields; `I` prefix for interfaces; `T` prefix for type parameters. *Analyzer:* S101, S100, S117, IDE1006.
- 🔴 **R2.** Formatting is decided by the shared `.editorconfig` and `dotnet format`, never discussed in review. Run `dotnet format` on files you change; never hand-format against it. *Analyzer:* IDE0055, `dotnet format --verify-no-changes`.
- 🔴 **R3.** One top-level type per file; the file is named after the type. *Analyzer:* MA0048, SA1649.

## 2. Domain vocabulary

- 🔴 **R4.** In the Application Core, use the business's own terms in the business's language. Official French terms stay French: `Établissement`, `Agrément`, `Autocontrôle`. Never translate them into an approximate English word. *Review.*
- 🔴 **R5.** One concept, one name across all layers. `Dossier` must not become `File`, `Case`, `Record` or `ApplicationItem` elsewhere. When you find a synonym, resolve it in the same PR. Role suffixes are allowed (`DossierResponse`, `DossierRow`); a different noun is not. *Review.*
- 🟡 **R6.** Add each new domain term to `docs/glossary.md` in the same PR, and to the Vale terminology pack if used in prose. *Review.*
- 🟡 **R7.** No infrastructure words in Application Core names: `DossierRow`, `DossierDto`, `DossierEntity` there signal a leaked persistence concern. *Review.*
- 🟡 **R8.** Technical types carry their role suffix: `DossierRepository`, `CreateDossierCommandHandler`, `DossierStatusProjection`, `DossierController`. *Review.*
  Pivot.Framework role suffixes to follow in services built on it:

  | Kind | Pattern | Base type / interface |
  | --- | --- | --- |
  | Command / handler / validator / response | `<UseCase>Command`, `<UseCase>CommandHandler`, `<UseCase>CommandValidator`, `<UseCase>Response` | `ICommand`/`ICommand<T>`, `ICommandHandler<,>`, `AbstractValidator<T>` |
  | Query / handler | `<UseCase>Query`, `<UseCase>QueryHandler` | `IQuery<T>`, `IQueryHandler<,>` |
  | Strongly typed id | `<Aggregate>Id` | `StronglyTypedGuidId<TSelf>` |
  | Domain event | `<Aggregate><PastTenseVerb>DomainEvent` (e.g. `OrderShippedDomainEvent`) | `DomainEvent` |
  | Integration event | `<Aggregate><PastTenseVerb>IntegrationEvent` | `IntegrationEvent` |
  | Event handler | `<Effect>On<Event>` or `<Event>Handler` | `DomainEventHandlerBase<T>`, `IntegrationEventHandlerBase<T>` |
  | Projection | `<ReadModel>Projection` | `ProjectionHandler<TEvent>` |
  | Domain→integration mapper | `<DomainEvent>Mapper` | `IIntegrationEventMapper<T>` |
  | Error catalogue | `<Aggregate>Errors` with `static readonly Error` fields, codes `Aggregate.Property.Reason` | `Error` |
  | Read model / spec | `<Name>ReadModel` or `<Name>Summary`, `<Criteria>Specification` | `IReadModel<TId>`, `ReadModelSpecification<T>` / `EntitySpecification<T,TId>` |
  | Unit of work / DbContext | `<Service>UnitOfWork`, `<Service>DbContext` | `UnitOfWork<TContext>`, `PivotDbContextBase` |
  | Saga | `<Process>Saga`, `<Action>Step`, `<Process>SagaData` | `ISagaDefinition<T>`, `ISagaStep<T>` |

Before introducing a domain name, search the solution and `docs/glossary.md` for the existing term and reuse it.

## 3. Intention-revealing names

- 🔴 **R9.** Names say what a thing is or does, not its type or implementation. Forbidden: `dossierList`, `strName`, `objResult`, `myHelper`. *Review.*
- 🟡 **R10.** Methods are verb phrases whose verb states the effect:

| Verb | Meaning |
| --- | --- |
| `Get` | Returns a result, no side effects; throws if absent |
| `Find` / `TryGet` | May return nothing |
| `Calculate` / `Compute` | Derives a value, pure |
| `Create` / `Register` | Produces something new |
| `Apply` / `Update` | Mutates |
| `Ensure` | Idempotent |

- 🟡 **R11.** Name length proportional to scope: `i` in a three-line loop, never a field named `d`. *Review.*
- 🟡 **R12.** Do not repeat context the type provides: inside `Dossier`, `Status` not `DossierStatus`, `Deadline` not `DossierDeadlineDate`. *Review.*
- 🟢 **R13.** Avoid noise words `Data`, `Info`, `Manager`, `Processor`, `Helper`, `Utils`. *Review.*

## 4. Abbreviations and acronyms

- 🔴 **R14.** No abbreviations except the accepted glossary (`FAVV`, `AFSCA`, `STS`, `SLA`, `Id`, `Url`, `Http`, plus the Pivot.Framework terms `Bff`, `Grpc`, `Jwt`, `Oidc`, `Pkce`, `Dlq`) and loop counters. `Uow`, `Repo`, `Evt` are rejected: write `UnitOfWork`, `Repository`, `Event`. `insp`, `dsr`, `nbrDoc`, `tmpVal`, `Svc`, `Chk` are rejected. *Review.*
- 🔴 **R15.** Acronyms of three or more letters are PascalCase (`HttpClient`, `StsToken`, `XmlReader`); two-letter acronyms stay upper case (`IOException`). `Db` is an abbreviation: `DbContext`. *Analyzer:* SA1305, CA1709.
- 🟡 **R16.** Names are pronounceable. *Review.*

## 5. Booleans

- 🔴 **R17.** Boolean members and parameters read as true/false assertions: `IsApproved`, `HasPendingAnalysis`, `CanBeClosed`, `RequiresInspection`. Never `Status`, `Flag`, `Check`, `Approval`. *Review.*
- 🔴 **R18.** No negative names (`IsNotEligible`). Name the positive and negate at the call site. *Review.*
- 🟡 **R19.** A boolean argument at a call site is a design problem; see `Functions and Control Flow / R12`. *Review.*

## 6. Async, collections, generics

- 🔴 **R20.** Methods returning `Task`/`ValueTask` end in `Async`; methods that do not, do not. *Analyzer:* CA1849; *review.*
- 🟡 **R21.** Collections are plural nouns (`Inspections`); scalars are singular. *Review.*
- 🟡 **R22.** With more than one type parameter, name them by role (`TCommand`, `TResult`, `TKey`). *Analyzer:* CA1715.

## 7. Literals

- 🔴 **R23.** No magic numbers except `0`, `1`, `-1` and values in a test's arrange block. Business numbers become a named constant, configuration value or enum member. *Analyzer:* S109.
- 🔴 **R24.** A string literal used more than twice becomes a constant. Route templates, claim types, configuration keys, cache key prefixes and policy names are always constants. *Analyzer:* S1192.
- 🔴 **R25.** A closed set of business values is an enum or value object, never a `string`/`int` compared to literals. *Review.*
- 🟡 **R26.** Constant names state the business reason: `MaxRetryAttempts = 3`, `DefaultTimeoutSeconds = 30`, never `Three`. *Review.*

```csharp
public static class SlaPolicy
{
    /// <summary>Working days allowed between submission and first review.</summary>
    public const int FirstReviewWorkingDays = 15;
}
```

## 8. Tests

- 🟡 **R27.** Test names follow `Method_Condition_ExpectedResult` so a CI failure is diagnosable from the name alone. *Review.*
- 🟡 **R28.** Test classes are `<TypeUnderTest>Tests`; builders and fixtures say what they build (`ApprovedDossierBuilder`, not `TestHelper2`). *Review.*

## 9. Checklist before finishing

- [ ] Every new name uses the existing domain term; no synonyms introduced; new terms in the glossary.
- [ ] No type prefixes, abbreviations outside the glossary, noise words or negative booleans.
- [ ] Method verbs match their effect; `Async` suffix exactly on awaitables.
- [ ] No magic numbers or repeated string literals.
- [ ] One type per file, file named after it; `dotnet format` applied.
