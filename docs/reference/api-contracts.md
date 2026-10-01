# API contract scope

The RaidManager application programming interface (API) publishes its operations in an OpenAPI document at
`/openapi/v1.json`, which is the source of truth for request shapes, response codes, and security.
Failures are returned as `ProblemDetails`; validation failures list each field under `errors`.

In Development, the interactive reference page at `https://localhost:55366/scalar` renders that document and lets you
try each operation ([ADR-0013](../adr/0013-scalar-api-reference.md)). The Aspire dashboard links to it as
"API reference" on the `api` resource. To call a website-only operation, enter the website key in the page's
authentication panel; `dotnet user-secrets list --project src/Containers/Aspire/Hosting/RaidManager.AppHost` shows it
as `Parameters:website-service-key` in your own terminal.

## Implemented operations

| Operation | Caller | Purpose |
| --- | --- | --- |
| `POST /internal/identity/discord-sign-in` | Website only | Resolve a Discord identity confirmed by OAuth to its single local user id. |
| `GET /internal/users/{userId}/character-claims/pending` | Website only | List the player's pending and conflicted character claims, oldest first. |
| `POST /internal/users/{userId}/character-claims/{characterId}/approve` | Website only | Approve a pending claim. Returns 409 and keeps the claim in conflict review when another player owns the character. |
| `POST /internal/users/{userId}/character-claims/{characterId}/reject` | Website only | Reject a pending claim, so the character never becomes the player's signup option. |
| `POST /internal/communities` | Website only | Link a Discord server as a community, with the user who added the bot as its Administrator. Returns 201 with the community id, or 409 when the server is already linked. |
| `GET /internal/communities/{communityId}` | Website only | Get a community's Discord server, realm and Administrator. |
| `GET /internal/communities/by-discord-server/{discordGuildId}` | Website only | Get the community a Discord server links to, or 404 when it isn't linked. |
| `GET /internal/users/{userId}/communities` | Website only | List the communities the user administers, by name. |

Operations under `/internal/` require the website key in the `X-RaidManager-Service-Key` header, as
[ADR-0011](../adr/0011-website-session-and-api-trust.md) describes. A missing or wrong key returns 401. The website
passes the signed-in player's user id from its session in the route.

A claim decision returns 204 on success, 404 when the character or claim doesn't exist, and 409 when the claim was
already decided or another player owns the character. Claims carry realm, class, race and claim state as names, such
as `Icecrown` or `Conflict`.

A community carries its realm as a name too. Linking needs a server id that is a Discord snowflake, a name of at most
100 characters, one of the realm names and a known user; anything else returns 400 naming the field, and an unknown
user returns 404.

## Planned contract areas

- User-scoped website calls, session, and profile access.
- Companion pairing, monitored-installation status, and authenticated, idempotent character snapshot ingestion.
- Resolving an ownership conflict between two players.
- Character profiles, loadouts, synchronization timestamps, and raid lockouts.
- Community membership and officer permissions, raid templates, recurrence, scheduling, and publication.
- Confirmed, tentative, late, and declined signups, presets, compositions, bench, boss assignments,
  gear-readiness warnings, and raid-buff coverage.
- Eligibility results per scheduled raid target, including verdict, source, freshness, reason, and reset time.
- Recorded officer exceptions for unknown data, reminders, roster-change notifications, attendance history,
  and addon roster export.

Transport models must stay separate from Domain types.
The web app and Discord bot must receive the same eligibility decision from the application layer.
