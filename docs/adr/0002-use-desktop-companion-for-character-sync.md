# ADR-0002: Use a desktop companion for character synchronization

- Status: Accepted
- Date: 2026-09-29
- Deciders: Gihed Annabi

## Context

RaidManager needs character data that Warmane Armory cannot reliably supply, especially multiple equipment sets
and character-specific raid lockouts. The World of Warcraft (WoW) 3.3.5a addon can read in-game data and save it
through SavedVariables, but it cannot make Hypertext Transfer Protocol (HTTP) requests to RaidManager.
WoW writes SavedVariables on reload, logout, or exit.
Players may use characters across several WoW accounts and expect them to appear under one Discord identity.

The considered options are manual file upload, addon-only synchronization, and an installed desktop companion.

## Decision

We use a desktop companion alongside the WoW addon. The addon captures the data available for each character
visited in game. The companion reads SavedVariables after the game writes them and uploads snapshots through an
authenticated application programming interface (API) connection paired with the player's RaidManager account.

New characters enter a pending state. The next website sign-in directs the player to review and approve or reject
them before they are selectable for a raid. An imported snapshot does not automatically transfer a character already
linked to another user. Every snapshot records its source and observation time.

## Consequences

**Positive**

- Players can synchronize character and lockout data without copying files into a browser.
- One Discord identity can review characters discovered across several WoW accounts.
- Approval and conflict review keep imports separate from usable signup choices.

**Negative**

- Players must install and maintain a second local application.
- Synchronization cannot be instantaneous when WoW has not written SavedVariables to disk.
- The companion needs secure pairing, authenticated uploads, and careful parsing of local files.

## Alternatives considered

- **Addon-only upload** - rejected because the WoW addon API cannot send HTTP requests to the website.
- **Manual file upload** - rejected because repeated player action does not meet the automatic synchronization goal.
