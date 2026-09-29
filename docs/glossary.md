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

A Discord-backed raiding community that organizes raid events and manages member participation.

## Discord identity

The external Discord account that authenticates a platform user. The platform does not manage local passwords.

## Eligibility

Whether an approved character can join a scheduled raid target.
The decision compares the matching instance and difficulty lockout reset with the raid start.
Missing or stale lockout data produces an unknown result.

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

## Roster selection

The single signup option chosen by a raid leader for a user in a concrete raid roster.

## SavedVariables

The WoW addon data that the game writes to a local file on reload, logout, or exit.
The addon cannot send this data directly to the website.

## Signup preset

A reusable selection of characters, specializations, preferences, and notes for raid signups.

## Warmane realm

A Warmane game realm such as Icecrown, Lordaeron, Blackrock or Onyxia.
