# ADR-0010: Read queries from the write database

- Status: Proposed
- Date: 2026-09-30
- Deciders: Gihed Annabi

## Context

The first query, a player's pending character claims, needs a read side. The house conventions keep queries off the
command repositories and suggest projected read models or a read database context. Projected read models are updated
by handlers reacting to domain events. RaidManager records domain events in an outbox but does not deliver them yet
(see the conversation behind #87), so a projection could not be kept up to date today.

## Decision

Queries read the write database directly, through no-tracking queries, until a query needs something more.

- The Application layer defines a small reader interface per feature, such as `ICharacterClaimReader`. Query
  handlers depend on it, never on a command repository or the DbContext.
- Persistence implements each reader with no-tracking Entity Framework Core queries against the existing tables,
  projecting straight to the query's response. It is registered in `AddRaidManagerPersistence`.
- Readers use the write `RaidManagerDbContext` with `AsNoTracking`, not a second read context with its own
  mapping: the model would be duplicated for no gain while both sides share one database.
- A projected read model replaces a reader when a query becomes too expensive to answer from the write tables,
  after event delivery has its own ADR.

## Consequences

**Positive**

- Query results are always current; there is no eventual consistency to explain to players.
- No projection tables, handlers, or event delivery are needed yet.
- Swapping a reader for a projection later changes only the Persistence implementation, not the query or its callers.

**Negative**

- Queries run against the write schema, so a mapping change can affect them; the integration tests cover each reader.
- Complex screens may need joins that a projection would avoid.

## Alternatives considered

- **Projected read models:** can be rebuilt from events and are fast to read, but they need event delivery first
  and add eventual consistency.
- **Separate read DbContext (Pivot's read/write split):** adds a second context and mapping over the same database,
  with no benefit until reads move elsewhere.
