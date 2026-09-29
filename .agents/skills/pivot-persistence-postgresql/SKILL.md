---
name: pivot-persistence-postgresql
description: Pivot.Framework.Infrastructure.Persistence.PostgreSQL — Npgsql support for the EF Core write side. Use when registering a PivotDbContextBase context on PostgreSQL (AddPostgreSqlContext), applying jsonb/timestamptz overrides for outbox and event history (ApplyPostgreSqlConfigurations), splitting read/write PostgreSQL contexts, taking a pg advisory lock for single-instance work, or modifying Src/Infrastructure/Pivot.Framework.Infrastructure.Persistence.PostgreSQL.
---

# Pivot.Framework.Infrastructure.Persistence.PostgreSQL

Location: `Src/Infrastructure/Pivot.Framework.Infrastructure.Persistence.PostgreSQL`.
References Persistence.EntityFrameworkCore + `Npgsql.EntityFrameworkCore.PostgreSQL` 10.
Everything else (UoW, outbox, repositories) comes from `pivot-persistence-efcore` / `pivot-messaging-outbox`.

## Registration

```csharp
services.AddPostgreSqlContext<AppDbContext>(
	configuration.GetConnectionString("Default")!,
	options => options.EnableSensitiveDataLogging(env.IsDevelopment()));   // optional extra config
services.AddEfCoreWritePersistence<AppDbContext, AppUnitOfWork>(includeEventStore: true);
```
`AddPostgreSqlContext<TContext>` (`Extensions/PostgreSqlExtensions`) = `AddDbContext<TContext>(o => o.UseNpgsql(cs); configure?.Invoke(o))`;
throws on blank connection string. `TContext : DbContext, IPersistenceContext`.

## Model configuration

Call **after** the provider-neutral framework configurations so the overrides win:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
	base.OnModelCreating(modelBuilder);
	modelBuilder.Entity<OutboxMessage>().ToTable("OutboxMessages");
	modelBuilder.ApplyConfiguration(new OutboxMessageConsumerConfiguration());
	modelBuilder.ApplyPostgreSqlConfigurations();   // outbox + event history overrides
	modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
}
```
`ApplyPostgreSqlConfigurations()` applies:
- `PostgreSqlOutboxMessageConfiguration` — `Payload` → `jsonb`; `CreatedAtUtc`, `ProcessedAtUtc`, `FailedAtUtc` → `timestamptz`. (Only column types; table name/key still yours to set.)
- `PostgreSqlEventHistoryEntryConfiguration` — runs the base `EventHistoryEntryConfiguration` (table `EventHistory`, indexes) then `Payload` → `jsonb`, `OccurredOnUtc`/`CreatedAtUtc` → `timestamptz`. **This registers the EventHistory entity**, so only call `ApplyPostgreSqlConfigurations()` if you are fine having that table (or include the event store).

### SQL-Server-only column types to override on PostgreSQL
The base package hardcodes `nvarchar(max)`, which PostgreSQL rejects. If you use these tables, override after applying their configuration:
```csharp
modelBuilder.ApplyConfiguration(new SagaInstanceConfiguration());
modelBuilder.Entity<SagaInstance>().Property(s => s.SerializedData).HasColumnType("jsonb");
modelBuilder.ApplyConfiguration(new AuditEntryConfiguration());
modelBuilder.Entity<AuditEntry>().Property(a => a.Details).HasColumnType("text");
```
Also note Npgsql maps `DateTime` to `timestamp with time zone` and requires `DateTimeKind.Utc` values — the framework
already writes UTC everywhere, keep it that way in your entities.

## Read/write split

```csharp
services.AddReadWritePostgreSqlDbContexts<AppWriteDbContext, AppReadDbContext>(writeCs, readCs);
```
Read context gets `UseQuerySplittingBehavior(SplitQuery)` + `NoTracking`. Neither context is required to implement
`IPersistenceContext` here, but the write one must for UoW/outbox registrations. Pair the read context with
`AddEfCoreReadModelStore<AppReadDbContext>()`.

## Advisory locks (`Locking/PostgreSqlAdvisoryLock`)

Use to ensure only one instance runs a critical section (e.g. an outbox drain or projection rebuild across replicas):
```csharp
var lockId = PostgreSqlAdvisoryLock.ComputeLockId("outbox-drain:orders");   // stable djb2 hash, not GetHashCode
await using var handle = await PostgreSqlAdvisoryLock.TryAcquireAsync(dbContext, lockId, ct);
if (handle is null)
	return;   // another instance holds it
// … critical section …
```
- Session-level `pg_try_advisory_lock` on the context's connection (opened if needed); non-blocking.
- `DisposeAsync` calls `pg_advisory_unlock` best-effort; the lock also dies with the session. Keep the same `DbContext`/connection alive for the whole critical section — with connection pooling, don't let EF close/reopen the connection in between (it opened it explicitly, so EF keeps it open until the context is disposed).
- The built-in `OutboxPublisherService` does **not** take this lock: with several replicas in `BackgroundPolling` mode they can publish the same message concurrently. Consumers must be idempotent (inbox), or wrap your own drain in this lock.

## Changing this package

Keep it a thin provider layer: provider-specific registrations, column-type overrides and PostgreSQL-only helpers.
Provider-neutral logic belongs in Persistence.EntityFrameworkCore. No test project exists; if you add logic, add tests
(Npgsql behaviour needs a real database — consider Testcontainers).
