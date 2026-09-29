# Version 1 visual guide

This guide describes the complete first-version design agreed with Gihed Annabi.
The [product scope](product.md) remains the source of product requirements.
The diagrams explain how those requirements fit together; they do not mark unimplemented features as delivered.

Start with use cases, then the domain model and components.
The sequence and state diagrams explain normal operation, failure handling, and decisions that require a person.

| Question | Diagram collection |
| --- | --- |
| Who can do what? | [Use cases](use-cases.md) |
| What information belongs together? | [Domain model](domain-model.md) |
| Which part does the work? | [Components and system boundaries](components.md) |
| How do sign-in, pairing, sync, and approval work? | [Account and character sequences](sequence-identity.md) |
| How do scheduling, signup, publication, and raid night work? | [Raid sequences](sequence-raids.md) |
| How do states and readiness decisions change? | [State and decision models](state-models.md) |
| What runs today? | [Architecture and implementation status](architecture.md#current-scaffold) |

## Reading the diagrams

All views describe the intended version 1 unless their caption explicitly says **current local development**.
Class diagrams describe domain concepts and multiplicities, not database tables or a final persistence schema.
Component names describe responsibilities; they do not prescribe a separate service for every box.
Messages in sequence diagrams are logical operations, not implemented application programming interface (API) routes.

Each picture links to its full-size Scalable Vector Graphics (SVG) export for zooming.
Its editable Mermaid or PlantUML source appears directly below it.
Use-case diagrams use Unified Modeling Language (UML) actors and use cases; other diagrams use Mermaid.
The [diagram maintenance guide](../how-to/diagrams.md) explains how to regenerate and validate the exports.

## Cross-diagram rules

- One Discord identity can approve many characters across multiple WoW accounts.
- A snapshot records what was observed. An incomplete scan does not prove a character has no raid save.
- Character ownership approval, signup availability, readiness, and roster assignment are separate decisions.
- The website and Discord bot update the same signup and apply the same community permissions.
- A published composition selects at most one character per player.
- Every required raid target is evaluated at the scheduled start, using the relevant realm's rules.
- A confirmed active lock blocks participation. An unknown verdict can receive a visible, reasoned officer exception.
- A schedule or snapshot change rechecks published selections; no automatic substitution occurs.
- Retry handling preserves one logical operation. A failed Discord message does not erase the saved signup or roster.

## Design details still requiring implementation validation

The diagrams expose these details without claiming a completed implementation:

- Validate exact realm reset, shared difficulty, extension, and encounter rules against supported WoW 3.3.5a realms.
- Define freshness windows per source and document what proves a complete saved-instance scan.
- Define the evidence and authorized review process for conflicting character claims.
  Website approval alone is not cryptographic proof of ownership, and a local file can be modified.
- Finalize companion pairing, secure credential storage, snapshot schema, and addon roster import format.
- Choose the scheduling and delivery mechanisms that implement the illustrated retries and reminders.
- Select the production hosting topology. The local Aspire view is not a production deployment plan.

These are engineering details inside the version 1 scope, not features deferred to a later release.
