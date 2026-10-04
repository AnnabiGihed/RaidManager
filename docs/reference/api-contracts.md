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
| `POST /internal/communities/by-discord-servers` | Website only | At sign-in, list the linked communities of the Discord servers the user is in, by name; servers that aren't linked are left out. |
| `GET /internal/users/{userId}/communities?memberOf={communityId}` | Website only | List the communities the user administers, then those in `memberOf` (repeatable), the ones the user's servers matched at sign-in; each group by name. |
| `GET /internal/users/{userId}/communities/{communityId}/roles` | Website only | Read the community's roles from Discord: the mappable roles, and rows for the Administrator, each role (with its id and permissions) and Member, each with its Discord roles and member count. Says whether the user may change roles, may let a role manage roles, and may change each role. Only a member of the server may; refreshes the stored server name. |
| `GET /internal/users/{userId}/communities/{communityId}/members` | Website only | List the server's members from Discord, without bots: each one's display name, avatar, Discord roles and roles (Administrator, the roles their Discord roles give, or Member), Administrator first, with the time Discord was asked. Only a member of the server may; refreshes the stored server name. |
| `POST /internal/users/{userId}/communities/{communityId}/roles` | Website only | Create a role with a `name` (at most 50 characters, unique in the community, 409 otherwise) and `permissions` (`ManageRaids`, `BuildRosters`, `RunRaidNight`, `ReviewConflicts`, `ManageCommunityRoles`); 201 with the `roleId`. The Administrator or a role manager may; only the Administrator can include `ManageCommunityRoles`. |
| `PUT /internal/users/{userId}/communities/{communityId}/roles/{roleId}` | Website only | Rename a role and change what it allows. Only the Administrator changes a role that grants `ManageCommunityRoles` or lets one grant it. |
| `DELETE /internal/users/{userId}/communities/{communityId}/roles/{roleId}` | Website only | Delete a role; its mappings go with it. Only the Administrator deletes a role that grants `ManageCommunityRoles`. |
| `PUT /internal/users/{userId}/communities/{communityId}/role-mappings/{roleId}/{discordRoleId}` | Website only | Make a Discord role give one of the community's roles; 404 for a role the community doesn't have. One Discord role can give several roles. The Administrator or a role manager may; only the Administrator maps a role that grants `ManageCommunityRoles`. |
| `DELETE /internal/users/{userId}/communities/{communityId}/role-mappings/{roleId}/{discordRoleId}` | Website only | Stop a Discord role giving that role; any other role it gives stays. The same people may as for mapping. |
| `POST /companion/pairings` | Companion, anonymous | Start pairing: returns a device code to poll with, a pairing code such as `K7M-4QX` to show, its expiry 10 minutes later and the polling interval of 5 seconds. Takes a `computerLabel` of at most 200 characters, kept to 64. |
| `POST /companion/pairings/token` | Companion, anonymous | Poll with the `deviceCode`: 400 `CompanionPairing.Pending` until the player confirms, then 200 with the `companionId`, the `deviceToken` and the player's name, once. Afterwards, and for an unknown device code, 400 `CompanionPairing.Invalid`; after expiry, 400 `CompanionPairing.Expired`. |
| `GET /companion/me` | Companion, device token | Check the pairing: the companion's id and computer label. |
| `GET /internal/users/{userId}/companion-pairings/{pairingCode}` | Website only | Describe the pairing a code belongs to (code, computer label, request time, expiry) for the player to check before confirming. |
| `POST /internal/users/{userId}/companion-pairings/{pairingCode}/confirm` | Website only | Confirm the code: binds the computer to the player, whose companion gets its token at its next poll. |
| `GET /internal/users/{userId}/companions` | Website only | List the player's companions, revoked ones included, oldest first, with when each was paired and last used and its status: `Active`, `Revoked` or `Expired`. |
| `POST /internal/users/{userId}/companions/{companionId}/revoke` | Website only | Revoke a companion: its next request gets 401. What it uploaded stays. |

Operations under `/internal/` require the website key in the `X-RaidManager-Service-Key` header, as
[ADR-0011](../adr/0011-website-session-and-api-trust.md) describes. A missing or wrong key returns 401. The website
passes the signed-in player's user id from its session in the route.

A claim decision returns 204 on success, 404 when the character or claim doesn't exist, and 409 when the claim was
already decided or another player owns the character. Claims carry realm, class, race and claim state as names, such
as `Icecrown` or `Conflict`.

A community carries its realm as a name too. Linking needs a server id that is a Discord snowflake, a name of at most
100 characters, one of the realm names and a known user; anything else returns 400 naming the field, and an unknown
user returns 404.

The officer roles operations ask Discord with the bot ([ADR-0022](../adr/0022-check-discord-roles-at-action-time.md)).
Someone outside the Discord server, or anyone but the Administrator changing a mapping, gets 403. A Discord role that
isn't one of the server's mappable roles returns 400: `@everyone` and roles of other bots can't be mapped. Discord not
answering returns 503, and a server the bot was removed from returns 409.

### Companion pairing

The companion pairs as [ADR-0030](../adr/0030-pair-the-companion-with-a-confirmed-code.md) decides. The public API
hostnames forward only `/companion/` (owner decision on #369).

- **Device token.** Routes under `/companion/` other than the two pairing routes need the device token in
  `Authorization: Bearer <token>`. The API keeps only its SHA-256 hash and checks it on every request. A missing or
  unknown token gets 401 `Companion.TokenUnknown`, a revoked companion 401 `Companion.Revoked`, and one unused for
  more than 180 days 401 `Companion.Expired`, each with `WWW-Authenticate: Bearer error="invalid_token"`. Last use is
  recorded at most once an hour.
- **Pairing codes** are six characters from `23456789ABCDEFGHJKMNPQRSTUVWXYZ`, shown as `XXX-XXX`; the routes accept
  them with or without the dash and in any case. A code another player can't use: the lookup and the confirmation
  answer 404 for an unknown code, and 409 `CompanionPairing.Expired` or `CompanionPairing.AlreadyConfirmed`.
- **Revocation** answers 404 for a companion the player doesn't have and 409 when it was already revoked.
- **Rate limits** answer 429 `RateLimit.Exceeded` with `Retry-After`: 10 pairing starts per address per 10 minutes,
  60 token polls per address per minute, and 10 code lookups or confirmations per player per 10 minutes. The
  `Companion:RateLimits` configuration section changes them.

## Planned contract areas

- User-scoped website calls, session, and profile access.
- Monitored-installation status, and authenticated, idempotent character snapshot ingestion by a paired companion.
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
