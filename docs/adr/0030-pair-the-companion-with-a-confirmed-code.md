# ADR-0030: Pair the companion with a confirmed code and a revocable device token

- Status: Proposed
- Date: 2026-10-03
- Deciders: Gihed Annabi

## Context

The desktop companion uploads character snapshots to the API for its player
([ADR-0002](0002-use-desktop-companion-for-character-sync.md)). Story #15 needs pairing, revocation and an
authenticated upload, and story #171 needs evidence when two players claim the same character. Spike #37 asks how
long a pairing lasts, where the companion keeps its credential, how revocation and replay are handled, and what
settles a conflicting claim. The [threat model](../explanation/companion-threat-model.md) lists the threats these
decisions answer.

Already fixed elsewhere:

- The pairing mockup (#179): the companion shows a code such as `K7M-4QX`, the player confirms it on the website,
  codes last 10 minutes, and the website lists paired computers with a revoke action that stops uploads at once.
- The public API serves only `/companion/...` routes (owner decision on #369); everything else is reachable only
  from the website inside Docker.
- Characters enter a pending claim, and an upload never transfers ownership (ADR-0002, `Character.RequestClaim`).

## Decision

### Pairing: a code shown on the companion, confirmed on the website

The flow follows the OAuth 2.0 device authorization grant (RFC 8628), without an OAuth server:

1. The companion calls `POST /companion/pairings` with the computer's label. The API creates a pending pairing with a
   **device code** (32 random bytes, kept only as a SHA-256 hash) and a **user code**: six characters from a
   31-symbol alphabet without characters that look alike (no `0`, `O`, `1`, `I` or `L`), shown as `XXX-XXX`. Both
   expire after **10 minutes**. Creating pairings is rate-limited per address.
2. The companion shows the user code and opens the website's pairing page with the code in its address.
3. The player, signed in with Discord, sees the code, the computer's label and the request time, checks the code
   against the companion and confirms. The website calls the API's internal route with its website key and the
   player's id (ADR-0011). Wrong codes are rate-limited per player.
4. The companion polls `POST /companion/pairings/token` with its device code every five seconds or more. After the
   confirmation, the API answers **once** with the companion's device token and id, and the pairing can't be used
   again. An expired code answers "expired", and the companion offers a new code.

### The device token

- **Opaque and random:** 32 bytes from a cryptographic generator, sent as `Authorization: Bearer <token>` on
  `/companion/...` routes over HTTPS only. The API keeps only its SHA-256 hash, with the companion's id, player,
  label, pairing time and last use.
- **Checked on every request**, so revocation applies at once: a revoked, expired or unknown token gets 401 with a
  reason the companion shows (mockup state 8).
- **Expires after 180 days unused** (owner decision on #37); a companion in regular use never expires. Last use is
  recorded at most once an hour, to keep writes low.
- **Never in addon files:** the token never touches the WoW folder or SavedVariables, and the companion never logs it.

### Storage on the player's computer

The first companion runs on **Windows only** (owner decision on #37). It keeps the token encrypted with the Windows
Data Protection API for the current Windows user (`ProtectedData`, `CurrentUser` scope), in
`%LOCALAPPDATA%\RaidManager\companion.dat`. Another Windows user, or a copy of the file to another computer, can't
decrypt it. macOS and Linux would add their own keychain later.

### Revocation

The website's paired computers list (mockup) revokes a companion through the API's internal route. Revocation is
immediate and final: the companion learns it at its next request, deletes its token, and offers to pair again.
Characters and snapshots it uploaded stay, as the mockup states.

### Replay and duplicate uploads

- **HTTPS only**, ended by the shared Caddy with Let's Encrypt (ADR-0027), so a token or an upload can't be read or
  replayed from the network.
- **Each snapshot carries an id the companion generates** (a UUID) and a hash of its content. The API records the ids
  it processed per companion: a repeated snapshot, from a retry or a replay with a stolen token, is acknowledged
  without being imported again.
- **Observation times are checked:** a snapshot observed more than five minutes in the future is refused, and the
  domain already refuses an older complete raid-save scan than the one it holds.
- **Size and rate limits** per companion bound what a stolen token can send.

### Evidence for conflicting claims

Each claim records, besides its player and time, the companion that made it, the first and last time the character
was observed, the number of snapshots, and a **fingerprint of the WoW account folder** the character was seen in
(owner decision on #37). The companion sends the folder name over HTTPS. The API immediately computes an HMAC-SHA256
of the lower-cased name with a per-environment secret, `COMPANION_EVIDENCE_KEY`, and never stores or logs the name.
Officers resolving a conflict (#171) see, for the two claims, whether they come from the same WoW account, and each
side's history; never the fingerprint or the name.

The key is a new secret of each GitHub environment ([ADR-0028](0028-keep-the-test-secrets-in-a-github-environment.md)),
added with the task that first uses it. It is not rotated unless it leaks, because a new key makes old and new
fingerprints incomparable.

## Consequences

**Positive**

- The player confirms every pairing on the website they're signed in to, and sees and revokes each computer.
- A stolen token is limited to one player's uploads, which only ever create pending claims, and is revoked at once.
- No WoW account name is stored, yet officers can tell a shared account from a disputed one.

**Negative**

- Each companion request looks its token up in the database, a small cost the API accepts.
- The companion is Windows-only at first.
- A player can still be tricked into confirming a code that isn't theirs (phishing); the page asks them to compare the
  code and pair only their own computer, and the result is limited to pending claims they must approve.

**Residual risk**

- Malware running as the player on their computer can use the token while it runs; DPAPI doesn't stop that. Revoking
  the computer ends it.
- Snapshot content comes from the player's own game files and can be edited before upload; the website marks its
  source, and officers see it. The threat model lists this as accepted.

## Alternatives considered

- **Short-lived access tokens with rotating refresh tokens:** standard for OAuth clients, but more moving parts, and
  revocation would wait for the access token to expire; a database query per request is cheap at this scale.
- **The player types the code into the companion** (the website shows it): the flow most sites use the other way
  round; the mockup and the device grant put the code on the device, which the website confirms.
- **Storing the token in the Windows Credential Manager:** equivalent protection, but its size and naming limits add
  nothing over a DPAPI-encrypted file in the user's profile.
- **No account information for conflicts:** less data leaves the computer, but officers couldn't tell a shared account
  from a false claim (owner decision on #37).
