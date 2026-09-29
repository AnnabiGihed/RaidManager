# Product scope and workflows

RaidManager is a Warmane raid-planning product for players and raid leaders in Discord communities.
Version 1 includes a website, Discord bot, World of Warcraft (WoW) 3.3.5a addon, and desktop companion.
It aims for the raid-planning workflows demonstrated by [raiding.site](https://raiding.site/), plus automatic
character synchronization and a reliable answer to whether a character can join a raid at its scheduled start.
This page defines **version 1 scope**, not features available in the current scaffold.

## Reference and product principles

The owner's Cadence screenshots and the public [raiding.site demo](https://demo.raiding.site/raids) show raid lists,
multi-character signups, presets, character management, raid saves, alternative compositions, and buff coverage.
The public [Baneveil Armory profile](https://armory.raiding.site/characters/Baneveil/Icecrown) demonstrates character
equipment, gems, enchants, talents, glyphs, and professions.
Other useful patterns include [Raid-Helper's](https://www.raid-helper.dev/documentation/advanced) signup administration
and [Warband's](https://www.warband.io/for-raid-leaders) availability, roster, and addon-export workflows.
These products are references, not dependencies or proof that RaidManager already has the features.
Retail-only World of Warcraft systems are not part of the WoW 3.3.5a scope.

- Show the same raid, signup, roster, and eligibility state on the website and in Discord.
- Explain every eligibility verdict with source and observation time; never treat missing data as an unsaved raid.
- Keep player actions short and officer decisions inspectable. Automation may suggest, but officers control rosters.
- Scope player data and officer actions to the appropriate Discord community.

## Player setup and character ownership

1. The player signs in with Discord and pairs the desktop companion with that RaidManager identity.
2. The companion discovers configured WoW installations and account folders; the player can review which folders
   are monitored, exclude an account, or pause synchronization.
3. The player installs the addon in each relevant WoW 3.3.5a installation and visits each character to capture it.
   Installing the addon alone cannot enumerate characters that have not been visited.
4. The addon records available identity, realm, class, professions, specializations, gear, and raid-lockout data.
   The snapshot distinguishes unavailable fields from observed empty values.
5. WoW writes SavedVariables on reload, logout, or client exit. The companion waits for a complete file write,
   uploads the snapshot securely, and retries after connection failures without duplicating imports.
6. Newly discovered characters stay pending. At the next website sign-in, the player is directed to a review page
   to approve or reject each one before it appears as a signup option.
7. A character already claimed by another identity enters conflict review. An upload never transfers ownership.

The website shows each character's realm, class, loadouts, professions, raid saves, last successful sync, and data
source. The companion shows pairing, monitored folders, last success, pending uploads, and actionable failures.
Players can unpair the companion and control which local accounts contribute data. Upload only data required for
character, raid, and readiness workflows; addon files never contain the website's authentication secret.
Players can also manage character visibility, notes, professions, and loadout labels on the website.
Manual character or raid-save edits remain visibly player-reported; they never overwrite synchronized facts
or turn an unknown lockout into verified availability.

## Raids and scheduling

Officers create and edit raids with a community, title, description, instance, difficulty, size, scheduled start,
signup deadline, and roster publication state. A combined raid identifies every required instance and difficulty.
The interface shows the guild's local time and the corresponding server reset information without losing the
canonical Coordinated Universal Time (UTC) instant. Changes to time or targets recalculate eligibility.

Version 1 includes reusable raid templates and weekly recurrence so officers can generate a schedule once.
Officers can copy a previous roster into a draft, edit the new raid, lock or reopen signups, and publish a single
Discord event or post whose status follows later edits. Only authorized community roles can manage raids.
The raid list shows upcoming and historical raids, time, instance, status, signup totals, and roster actions.

## Signup and availability

Players can respond through Discord or the website. Both channels edit one signup, rather than creating separate
responses. A Discord interaction provides a short path to choose approved characters; the website supports the
full multi-character form. A signup can offer several character specializations, mark a preferred option, add
per-character notes, and save a reusable preset. Players can select multiple options quickly, edit a response,
or withdraw. Availability includes confirmed, tentative, late, and declined; a late response carries an arrival
time. Officers see the response history and current state, not duplicate signups from different channels.

The signup view shows the readiness verdict beside each offered option before saving. A player cannot submit a
character confirmed to remain locked through raid start for that target. A roster may select at most one character
from each player, regardless of how many options the player offered. Players receive concise notifications when
their response, roster assignment, or raid schedule materially changes. Configurable reminders reach players
before signup closes and before raid start without sending duplicate messages from both channels.

## Raid-start readiness

RaidManager evaluates character eligibility for the scheduled raid start, not the current moment.
It matches realm, instance, and difficulty against character-wide lockouts and the applicable realm reset schedule.
For a combined raid, every required target receives its own verdict; the overall result uses the most restrictive
target. A loadout does not own a lockout. For example, a save that resets Tuesday does not prevent a Wednesday
raid, but a save that lasts until Thursday prevents it.

| Verdict | Meaning | Player and officer action |
| --- | --- | --- |
| Available | Fresh data confirms no matching active save at raid start. | Character can be offered and rostered. |
| Resets before raid | A matching save expires before raid start. | Character can be offered; show reset time. |
| Locked through raid | A matching save remains active at raid start. | Block signup and roster assignment. |
| Needs fresh sync | Save, reset, realm mapping, or observation is missing or stale. | Request a sync. |

Show the relevant instance, difficulty, raid start, reset time when known, data source, and last observation.
Raid saves are synchronized facts rather than manually toggled truth. Preserve available extension and
encounter-progress details. Freshness must be defined per data source and reset cycle; a once-observed empty
save must not remain valid indefinitely. Re-evaluate signups and published rosters after a new snapshot, reset,
raid time change, or target change, then notify affected players and officers.

Only an officer may place a **Needs fresh sync** character in a roster as an explicit exception, with a reason
recorded and visible to the player and officers. This does not change the verdict to Available. A confirmed
**Locked through raid** character has no override in version 1. A player can offer a different eligible character.

## Officer planning and publication

The officer workspace shows signups, availability, preferred options, notes, role counts, data freshness, and
eligibility reasons. Officers can search and filter by player, character, role, assignment, response, and verdict.
They can prepare multiple draft compositions, duplicate a prior composition, assign placeholders, maintain a bench,
and compare role and class/spec balance with buff and debuff coverage. A draft warns about duplicate players,
overlapping raids, unavailable players, unknown data, missing roles, and other visible conflicts.
Officers can also prepare boss-specific assignments within a raid and copy assignments from a prior run.

Officers select and swap participants manually; the product does not silently optimize or publish a roster.
Review before publication shows changed assignments and unresolved warnings. Publishing updates the Discord post
and informs selected and benched players. A schedule or lockout change that invalidates a published assignment
flags the roster for review instead of silently replacing that participant.

## Raid-night workflow

The published roster supports attendance check-in, late arrivals, bench and substitute swaps, and an export for
the WoW 3.3.5a addon so an officer can see whom to invite in game. The export is specific to the selected raid
and current published composition; it does not invite a locked or unapproved character silently.
Officers can keep an attendance history across raids. Version 1 also shows practical gear-readiness warnings for
missing gems or enchants when the synchronized data supports them, alongside an inspectable loadout and
configurable raid requirements. GearScore and those requirements are advisory to officers, not a substitute
for the lockout verdict or an automatic exclusion rule.

## Version 1 acceptance journeys

- A new player pairs one companion, visits characters across two WoW accounts, approves each new character at
  the next website sign-in, and sees its last sync and raid saves.
- A player offers multiple specializations through either channel, saves a preset, and sees one consistent
  confirmed, tentative, late, or declined response on both channels.
- A character saved today can join a raid after its reset, but cannot be offered for a raid before that reset.
  An unknown save requests synchronization and never appears as confirmed available.
- An officer creates a recurring raid, reviews candidates and lockout reasons, publishes one character per
  player, and later replaces an absent participant from the bench without rebuilding the raid.
- A changed raid time or fresh lockout snapshot triggers a new verdict and alerts the affected player and officer.
  The officer can record a reasoned exception for unknown data, but not for a confirmed active lockout.
- At raid time, the officer checks attendance and exports the current roster to the addon for in-game invitations.
- Before raid time, the player receives one reminder and can see boss assignments; afterwards, the officer
  can review attendance history and gear-readiness warnings for future planning.

## Implementation status

The repository contains domain concepts and placeholder hosts. Discord login, addon capture, companion upload,
approval, scheduling, signup, readiness evaluation, roster management, and raid-night workflows remain planned.
All behaviors on this page are required for the first usable version; their presence here does not imply they
are already implemented.
