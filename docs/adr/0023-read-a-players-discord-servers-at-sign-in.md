# ADR-0023: Read a player's Discord servers at sign-in

- Status: Proposed
- Date: 2026-10-02
- Deciders: Gihed Annabi

## Context

A community is a Discord server linked to RaidManager (story #14). Its Administrator sees it because RaidManager
stores who added the bot. Everyone else in the server is a Member, but RaidManager doesn't store members
([ADR-0022](0022-check-discord-roles-at-action-time.md)), so the website can't tell which community a signed-in
member belongs to.

Discord can tell: with the `guilds` scope, `GET /users/@me/guilds` lists the servers the signed-in user is in. The
other ways to find a member's community are to ask the bot about every linked server at each page, which costs a
Discord call per server and page, or to keep a copy of every server's members, which ADR-0022 rejected.

The owner decided on #14 (2026-10-01) to read the servers at sign-in and keep the matches until the next sign-in.

## Decision

- **Sign-in asks Discord for `identify` and `guilds`.** Discord's consent screen then also says that RaidManager can
  see the user's servers.
- **The website reads the server list once, while the sign-in completes**, with the sign-in token, in one call:
  `GET /users/@me/guilds?limit=200`. 200 is Discord's page limit and the most servers an account can be in. The token
  isn't kept afterwards, as [ADR-0011](0011-website-session-and-api-trust.md) requires.
- **The API matches the servers against linked communities** (`POST /internal/communities/by-discord-servers`). The
  session cookie keeps only the matched communities' identifiers, not the server list.
- **Pages list the communities the user administers first, then the matched ones.** A community's roles and
  members still check the user's membership with the bot (ADR-0022), so a stale match shows the community's name and
  realm but none of its roles or members, and gives no officer rights.
- **A failed read never blocks sign-in.** When Discord or the API can't answer, the user signs in without matched
  communities, the failure is logged, and the next sign-in tries again.

## Consequences

**Positive**

- A member sees their community right after signing in, without RaidManager storing members.
- One Discord call per sign-in, made with the user's own token, so it doesn't count against the bot's rate limits.
- The cookie stays small: one identifier per matched community.

**Negative**

- A member who joins a linked server, or a server linked after they signed in, appears after their next sign-in.
- A member who leaves a server keeps seeing its name and realm until they sign in again. The membership check
  refuses its roles and members, so they see an explanation instead.
- Discord's consent screen asks for one more permission.

## Alternatives considered

- **Ask the bot about every linked server on each page:** always current, but one Discord call per linked server and
  page, which grows with the number of communities.
- **Keep the server list in the session and match on each page:** current for newly linked servers, but the cookie
  grows with every server the user is in, up to 200 identifiers.
