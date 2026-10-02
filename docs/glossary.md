# Glossary

## Character approval

The website review step that lets a Discord-authenticated player accept or reject a newly imported character.
An unapproved character cannot be offered for a raid.

## Companion

The desktop application paired with a player's RaidManager account.
It reads the addon's saved data after World of Warcraft (WoW) writes it to disk and uploads character snapshots
to the application programming interface (API).

## Character

A Warmane World of Warcraft character identified by realm and character name.
Character-wide state includes approval, ownership, synchronization freshness, and raid lockouts.

## Community

A Discord server linked to RaidManager, raiding on one Warmane realm. Everyone in the server is a member; the user
who added the bot is its Administrator, and the Discord roles it maps give its roles, such as Officer or Raid leader.

## Community role

A role in a community, such as Officer, Raid leader or one the community created, with the permissions it allows:
Manage raids, Build rosters, Run raid night, Review conflicts and Manage community roles. A member has every
permission of every role their Discord roles give them.

## Role manager

A member whose roles grant Manage community roles. A role manager creates, changes and deletes the community's roles
and maps Discord roles to them, except a role that itself grants Manage community roles, which only the Administrator
changes.

## Discord identity

The external Discord account that authenticates a platform user. The platform does not manage local passwords.

## Eligibility

Whether an approved character can join a scheduled raid target.
The decision compares the matching instance and difficulty lockout reset with the raid start.
Missing or stale lockout data produces a needs-fresh-sync verdict, not an available result.

## Officer exception

A recorded, visible reason for rostering a character whose lockout status needs a fresh sync.
It does not make that character eligible and cannot override a confirmed active lockout.

## GearScore

The WotLK community metric calculated for one concrete character loadout.
A character can therefore have multiple GearScores.

## Loadout

A raid-capable character configuration containing a specialization, role, equipment set, GearScore, talents, glyphs,
combat statistics and synchronization timestamp.

## Raid lockout

A character-wide saved-instance state for a raid instance and difficulty, with a reset time.
The synchronized record can also include extension and encounter-progress details when the game exposes them.
Lockouts do not belong to a loadout.

## Raid signup

A user's expression of availability for a raid, including one or more offered verified character loadouts.
The website and Discord bot edit the same response. Availability can be confirmed, tentative, late, or declined.

## Roster selection

The single signup option chosen by a raid leader for a user in a concrete raid roster.

## Raid-start readiness

The verdict for one character and raid target at the scheduled start: available, resets before raid,
locked through raid, or needs fresh sync.

## SavedVariables

The WoW addon data that the game writes to a local file on reload, logout, or exit.
The addon cannot send this data directly to the website.

## Signup preset

A reusable selection of characters, specializations, preferences, and notes for raid signups.

## Warmane realm

A Warmane game realm such as Icecrown, Lordaeron, Blackrock or Onyxia.
