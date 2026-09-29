# API contract scope

The RaidManager application programming interface (API) currently exposes a foundation endpoint only.
No product operation, request shape, response code, or event contract has been implemented yet.
OpenAPI will be the source of truth for HTTP operations once the first product slice exists.

Planned contract areas include:

- Discord sign-in, session, and profile access.
- Companion pairing and authenticated character snapshot ingestion.
- Pending character review, approval, rejection, and ownership conflict handling.
- Character profiles, loadouts, synchronization timestamps, and raid lockouts.
- Community membership and raid scheduling.
- Confirmed and tentative signups, presets, roster selection, and raid-buff coverage.
- Eligibility results for a scheduled raid target, including the reason and lockout reset time.

Transport models must stay separate from Domain types.
The web app and Discord bot must receive the same eligibility decision from the application layer.
