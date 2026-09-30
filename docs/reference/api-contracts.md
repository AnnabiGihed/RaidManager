# API contract scope

The RaidManager application programming interface (API) publishes its operations in an OpenAPI document at
`/openapi/v1.json`, which is the source of truth for request shapes, response codes, and security.
Failures are returned as ProblemDetails; validation failures list each field under `errors`.

In Development, the interactive reference page at `https://localhost:55366/scalar` renders that document and lets you
try each operation ([ADR-0013](../adr/0013-scalar-api-reference.md)). The Aspire dashboard links to it as
"API reference" on the `api` resource. To call a website-only operation, enter the website key in the page's
authentication panel; `dotnet user-secrets list --project src/Containers/Aspire/Hosting/RaidManager.AppHost` shows it
as `Parameters:website-service-key` in your own terminal.

## Implemented operations

| Operation | Caller | Purpose |
| --- | --- | --- |
| `POST /internal/identity/discord-sign-in` | Website only | Resolve a Discord identity confirmed by OAuth to its single local user id. |

Operations under `/internal/` require the website key in the `X-RaidManager-Service-Key` header, as
[ADR-0011](../adr/0011-website-session-and-api-trust.md) describes. A missing or wrong key returns 401.

## Planned contract areas

- User-scoped website calls, session, and profile access.
- Companion pairing, monitored-installation status, and authenticated, idempotent character snapshot ingestion.
- Pending character review, approval, rejection, and ownership conflict handling.
- Character profiles, loadouts, synchronization timestamps, and raid lockouts.
- Community membership, officer permissions, raid templates, recurrence, scheduling, and publication.
- Confirmed, tentative, late, and declined signups, presets, compositions, bench, boss assignments,
  gear-readiness warnings, and raid-buff coverage.
- Eligibility results per scheduled raid target, including verdict, source, freshness, reason, and reset time.
- Recorded officer exceptions for unknown data, reminders, roster-change notifications, attendance history,
  and addon roster export.

Transport models must stay separate from Domain types.
The web app and Discord bot must receive the same eligibility decision from the application layer.
