# Product scope and workflows

RaidManager is intended for players and raid leaders who organize Warmane raids through a Discord community.
It combines a website, Discord bot, World of Warcraft (WoW) 3.3.5a addon, and desktop companion.
The target is full functional parity with [raiding.site](https://raiding.site/), plus automatic character discovery,
account linking, and time-aware raid-lock eligibility.
The reference is a product benchmark, not a dependency or an implemented feature set.

## Reference workflows

Screens supplied by the project owner show these guild workflows:

- A raid list with instance, schedule, signup counts, signup status, and roster actions.
- A signup form where a player offers several characters and specializations, marks a preferred option, adds notes,
  saves a confirmed or tentative response, withdraws, and reuses presets.
- A raid workspace with signup filters, a 25-player roster, role counts, and raid-buff coverage.
- A character list with class, realm, specializations, GearScore, professions, and management controls.
- A raid-save matrix by character, instance, and difficulty.
- Signup presets for preferred characters, specializations, and notes.

The public [Baneveil Armory profile](https://armory.raiding.site/characters/Baneveil/Icecrown) also demonstrates
equipment, gems, enchants, talents, glyphs, professions, and other character details.
The reference site's guild pages require Discord sign-in; these observations do not establish every private
feature or administrative workflow.

## Character synchronization and approval

1. A player signs in to RaidManager with Discord and pairs the desktop companion with that account.
2. The player installs the addon in each WoW 3.3.5a installation used for their characters.
3. When the player logs into a character, the addon captures available character and loadout data, including
   raid lockouts. It can learn about characters across several WoW accounts as they are visited.
4. WoW writes the addon's SavedVariables to disk on reload, logout, or client exit.
5. The companion reads those files and sends new snapshots to the RaidManager application programming interface
   (API).
6. Newly discovered characters remain pending. At the next website sign-in, the player goes directly to an
   approval page to review and accept or reject them.
7. Approved characters appear in the player's character list and become selectable for raid signups.
   A character already linked to another user needs conflict review; an upload does not transfer ownership.

The addon cannot upload directly to a website, and installing it cannot enumerate unvisited characters.
The companion is part of the intended automatic synchronization flow.
The website should show when each snapshot was last updated.

## Signup and raid eligibility

A player can offer multiple approved character loadouts for one raid and name a preferred option.
A roster selects at most one option per player.
The Discord bot lets the same player select their approved characters for a raid without leaving Discord.
The website and bot must show the same eligibility result and the same saved signup response.
For each offered character, RaidManager compares the raid's scheduled start with the saved lockout for the
matching instance and difficulty. A lockout that resets before the raid starts does not make the character
ineligible. A lockout still active at that time does.

If a raid covers several instances or difficulties, eligibility must be evaluated for each required target.
Missing or stale lockout data is **unknown**, never interpreted as unsaved. The player must synchronize again
before the system treats the character as eligible.
The signup experience should explain which lockout prevents a choice and when it resets.
Unlike the manually toggled raid-save matrix in the reference screens, RaidManager's raid-save status comes from
synchronized character data and displays its last observation time.
Capture the saved instance, difficulty, reset, and any available extension or encounter-progress details.

## Delivery status

The repository currently contains domain concepts and placeholder hosts.
Discord login, addon capture, companion upload, approval screens, raid scheduling, signups, and roster management
remain planned work. The screenshots are references for behavior, not evidence that these features are implemented.
