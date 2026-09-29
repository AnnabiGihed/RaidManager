# API contract scope

The RaidManager application programming interface (API) currently exposes a foundation endpoint only.
No product operation, request shape, response code, or event contract has been implemented yet.
OpenAPI will be the source of truth for HTTP operations once the first product slice exists.

Planned contract areas include:

- Discord sign-in, session, and profile access.
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
