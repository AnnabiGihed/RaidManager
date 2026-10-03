# ADR-0029: Store data in PostgreSQL, one instance per environment

- Status: Proposed
- Date: 2026-10-03
- Deciders: Gihed Annabi

## Context

RaidManager stores its data in SQL Server: `Microsoft.EntityFrameworkCore.SqlServer` in the persistence project,
`Aspire.Hosting.SqlServer` in the AppHost, six SQL Server migrations, and SQL Server test containers.
[ADR-0027](0027-run-the-test-environment-on-one-ovh-vps.md) planned one SQL Server Express instance capped at 2 GB
on the shared OVH server, about half its memory.

On 2026-10-03 the owner decided on three environments on that server, dev, test and production, each with its website,
API and bot (#369). SQL Server on Linux needs 2 GB per instance, so three instances can't fit in the 3.7 GiB the
server has, and even one shared instance leaves too little for nine application containers and the applications
already there. The owner decided to move to PostgreSQL (#409), and to give each environment its own instance (#409).

Nothing is deployed yet, so no stored data has to move.

## Decision

### PostgreSQL through Pivot.Framework

- **The persistence project uses PostgreSQL** through `Pivot.Framework.Infrastructure.Persistence.PostgreSQL`:
  `AddPostgreSqlContext` registers the context on `Npgsql.EntityFrameworkCore.PostgreSQL`, and
  `ApplyPostgreSqlConfigurations` maps the framework's tables (outbox, inbox, event history) to `jsonb` and
  `timestamptz`. `Microsoft.EntityFrameworkCore.SqlServer` goes away.
- **The AppHost runs PostgreSQL locally** with `Aspire.Hosting.PostgreSQL` 13.5.4, the pinned Aspire version, and a
  data volume, in place of `Aspire.Hosting.SqlServer`. The server runs the same major version as the local run; #411
  records the image tag the package uses.
- **One fresh initial migration replaces the six SQL Server migrations.** No environment holds data yet. Local data
  in the old SQL Server volume isn't carried over: the owner's local test data starts again, and the old volume can
  be removed.
- **Tests run against PostgreSQL** in containers, as they run against SQL Server today.

### One instance per environment

- **Each environment has its own PostgreSQL container** in its own Compose project, with one database and one login
  (owner decision on #409). Dev can't reach production's data or slow it with a lock, and each environment upgrades
  on its own.
- **No instance publishes a port.** Each is reachable only on its environment's Compose network, never on the shared
  `web` network.
- **Its password is a secret** delivered like the others ([ADR-0028](0028-keep-the-test-secrets-in-a-github-environment.md));
  the AppHost's PostgreSQL password parameter replaces `sql-password` in ADR-0028's table, which #386 revises for the
  three environments.
- **Memory is capped per instance:** `shared_buffers` of 64 MB with a 192 MB container limit for dev and test, and
  128 MB with a 320 MB limit for production.

### Behavior that must not change

SQL Server's default collation compares text without regard to case; PostgreSQL compares it case-sensitively. #411
lists every query that compares or orders names (characters, communities, roles, Discord names) and keeps today's
behavior on purpose, for example with a case-insensitive collation on those columns, with tests that prove it.

### Memory on the shared server

| Process | Planned memory |
| --- | --- |
| Ubuntu and Docker | 0.5 GB |
| Shared Caddy, website and Delivery Atlas (measured: 46 MB) | 0.1 GB |
| PostgreSQL: dev, test and production | 0.7 GB at most (0.19 + 0.19 + 0.32) |
| Website, API and bot, three environments | 1.5 GB (0.5 GB each, with container limits) |
| Total | 2.8 GB of the 3.7 GiB (about 3.9 GB) the system sees |

The 2 GB swap file absorbs peaks. The Aspire dashboard doesn't run on the server: the budget has no room for one
per environment, and the logs stay available through `docker compose logs`.

## Consequences

**Positive**

- The three environments fit on the server with about 1 GB to spare, where SQL Server couldn't fit them.
- Each environment is isolated in its own database process.
- PostgreSQL's image and memory are a fraction of SQL Server's, and it needs no license choice.

**Negative**

- Every persistence test and the API tests move to a new engine; differences such as case sensitivity can surface as
  behavior changes, which #411 must catch.
- The owner's local data starts again.
- Three instances use about 150 MB more than one shared instance.

**Residual risk**

- OVH's daily server backup covers the database volumes, but restoring one database alone from it is coarse. A
  logical backup of production's database (`pg_dump` on a schedule) belongs to the production work (#433) before
  real data arrives.

## Alternatives considered

- **SQL Server Express, one shared instance:** keeps the code, but takes 2 GB of the server and leaves too little for
  three environments.
- **One shared PostgreSQL instance with three databases:** about 150 MB less, but production shares a process, its
  locks and its upgrades with dev; rejected by the owner (#409).
- **A managed database service:** no memory on the server, but a monthly cost and a network dependency that a test
  setup on one VPS doesn't need yet.
- **SQLite:** almost no memory, but no concurrent writers across processes and no match with the framework's
  persistence packages.
