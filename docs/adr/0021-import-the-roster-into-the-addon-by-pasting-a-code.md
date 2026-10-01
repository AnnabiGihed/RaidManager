# ADR-0021: Import the roster into the addon by pasting a code

- Status: Proposed
- Date: 2026-10-01
- Deciders: Gihed Annabi

## Context

On raid night, the officer needs the published roster in the WoW 3.3.5a addon, so they can see whom to invite in game
(story #35). A WoW 3.3.5a addon can't read files, open sockets or call HTTP. It sees only its SavedVariables, which
the client loads at login and writes back on `/reload`, logout or exit. Anything written to SavedVariables while the
game is running is overwritten when the client saves.

That leaves two ways in:

- **A text code pasted into the addon** while the game runs.
- **The desktop companion writing SavedVariables** while the game is closed, so the roster is there at login.

The roster often changes shortly before or during raid night: a late swap, a filled placeholder, a lockout found
after publication.

## Decision

- **The roster reaches the addon as a pasted text code.** The website's raid-night page shows a code for the current
  published roster, with a **Copy** button. In game, `/raidmanager import` opens a box to paste it.
- **The code is versioned and specific to one raid and one published version.** It starts with a format version and
  identifies the raid and its published roster version. When a newer version is published, the addon refuses the old
  code and says so.
- **The code holds only what inviting needs:** the raid, the published version, each character's name and realm,
  and its group and role. It never holds a token, a pairing code or any credential.
- **Locked and unapproved characters are left out of the code,** and the website lists them, with the reason, next
  to the code. The addon can't invite a character the code doesn't contain.
- **No export before publication.** The website offers no code while the raid's roster isn't published.
- **The addon treats the code as unsafe input:** it checks the format version, parses strictly with length limits,
  shows a clear error for any part it can't read, and never executes it.
- **Invitations happen only on a click,** never on import, and never in combat.

## Consequences

**Positive**

- Works at any moment, including after a last-minute swap, without a logout or `/reload`.
- Needs no companion on the raid leader's computer, and no file access from the game.

**Negative**

- The officer copies and pastes the code again after each publication; a stale code is refused rather than updated.
- A 25-player roster makes a long code. It must fit the game's edit box, which limits how much the code can carry.

## Alternatives considered

- **The companion writes SavedVariables before login:** no pasting, but a change after login needs a logout or
  `/reload`, and it requires the companion on the raid leader's computer.
- **Both a pre-loaded roster and a paste code:** covers both cases, but two paths to build, test and explain for the
  same result.
- **Addon messages from another player's client:** needs that player online with the addon, and still needs the data
  to reach their client first.
