# ADR-0022: Check Discord roles at action time

- Status: Proposed
- Date: 2026-10-01
- Deciders: Gihed Annabi

## Context

A community is a Discord server linked to RaidManager (story #14). Its Administrator is the user who added the bot,
everyone in the server is a Member, and the Discord roles the Administrator maps give Officer or Raid leader. When a
member loses a mapped role or leaves the server, they must stop managing raids, and the website and the bot must
apply the same check.

RaidManager therefore needs each member's current membership and Discord roles. There are three ways to get them:

- **Ask Discord when an action needs it:** the API calls Discord's REST API with the bot token.
- **Keep a live copy:** the bot holds a gateway connection and stores every member and role change as it happens.
- **Read the roles at sign-in:** the website asks for the `guilds.members.read` scope and reads the roles when the
  user signs in with Discord.

## Decision

- **RaidManager asks Discord's REST API, with the bot token, when an action needs a member's role.** The answer is
  cached for about a minute per member and server, so a removed role stops counting within that time.
- **RaidManager stores only what it owns:** the linked server, its name, the Warmane realm, the Administrator, and
  which Discord roles give Officer or Raid leader. It doesn't store members or their roles. The `Community` aggregate
  gives a member's role from the Discord roles the check returns: Administrator for the Administrator, otherwise the
  highest mapped role, otherwise Member.
- **One application service performs the check**, and both the website (through the API) and the bot use it.
- **The Members page asks Discord when it opens** and shows when it last checked.
- **A failed check fails closed.** When Discord can't be reached, the action is refused with an explanation; it never
  falls back to an older answer or to officer rights.
- **The bot token is a new secret**, `discord-bot-token`, added as an Aspire parameter like `discord-client-secret`.
  The Discord application enables the Server Members intent, which listing a server's members requires.

## Consequences

**Positive**

- A lost role stops counting within about a minute, whatever the user does, which meets the role-loss criterion
  of story #14.
- The website and the bot share one check, so they can't disagree.
- No gateway connection is needed for permissions, so the bot isn't on the path of every website action.
- Nothing about members needs to be kept in sync or cleaned up when a member leaves.

**Negative**

- Officer actions depend on Discord being reachable; when it isn't, they are refused until it is.
- Each check after the cache expires costs a Discord API call and counts against its rate limits. One minute keeps
  this to about one call per active member a minute.
- The bot token can read every server the bot is in, so it is stored and handled like the client secret.

## Alternatives considered

- **Live gateway sync:** changes apply instantly and checks never call Discord, but the bot becomes a required,
  always-on part of every permission check, and the stored copy must be repaired after any missed event.
- **Read roles at sign-in:** simplest, but a removed officer keeps their rights until they sign in again, which
  doesn't meet the role-loss criterion.
