# ADR-0024: Give each community its own roles with permissions

- Status: Proposed
- Date: 2026-10-02
- Deciders: Gihed Annabi

## Context

[ADR-0022](0022-check-discord-roles-at-action-time.md) gave every community the same two roles, Officer and Raid
leader, ranked so that a member got their highest mapped role. While checking #306, the owner asked to create their
own roles, such as Veteran, and decided on story #308 (2026-10-02) what such a role is: a name and a chosen set of
permissions.

## Decision

- **A community holds its roles.** Each role has a name (at most 50 characters) and the permissions it allows:
  Manage raids, Build rosters, Run raid night, Review conflicts and Manage community roles. Discord roles are mapped
  to roles by id; one Discord role can give several roles.
- **Officer and Raid leader are presets.** Every community starts with them. Officer allows the four raid
  permissions; Raid leader allows Manage raids, Build rosters and Run raid night; neither allows Manage community
  roles. Existing communities get the same presets, with their mappings kept.
- **A member has every permission of every role their Discord roles give them.** Roles aren't ranked any more. The
  Administrator has every permission, and everyone else in the server is a Member with none.
- **Actions ask for a permission, not a role.** The shared check of ADR-0022 now answers with the member's permissions,
  still read from Discord at action time and failing closed.
- **Only the Administrator changes a role that grants Manage community roles**, and only the Administrator can give a
  role that permission, so nobody widens their own rights (owner decision on #308). Creating, editing and deleting
  roles is task #313.

## Consequences

**Positive**

- Each community shapes its raid team without a change to RaidManager, and a new permission is a new flag.
- A member with several roles gets all of them, so mapping one Discord role to two roles never takes anything away.

**Negative**

- Permissions are stored as a number of flags, so a permission must never change its value once released.
- The members page and the sidebar show several roles where they showed one.

## Alternatives considered

- **Fixed roles with a rank** (ADR-0022): simplest, but a community can't add a role such as Veteran.
- **One permission per mapping, without roles:** no names to manage, but members and the roles card couldn't say who
  is what, and the same permissions would be repeated on every Discord role.
