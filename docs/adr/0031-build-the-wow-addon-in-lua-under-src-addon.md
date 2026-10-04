# ADR-0031: Build the WoW addon in Lua 5.1 in `src/Addon`, checked by busted, luacheck and StyLua

- Status: Proposed
- Date: 2026-10-04
- Deciders: Gihed Annabi

## Context

Story #16 captures each character a player logs into with a World of Warcraft 3.3.5a addon, which writes the
SavedVariables file the desktop companion uploads ([ADR-0002](0002-use-desktop-companion-for-character-sync.md)).
The repository holds .NET code only, so the addon brings a new language and its own tools. #38 fixed the file's shape
in the [addon snapshot contract](../reference/addon-savedvariables.md). Task #383 builds the addon's first part.

## Decision

- **Language and client:** Lua 5.1, the version the 3.3.5a client embeds, for Interface 30300 (build 12340). Every
  API the addon calls exists in 3.3.5a; nothing from later expansions.
- **Place:** `src/Addon/RaidManager/`, the folder players copy into `Interface/AddOns`. Its name equals the TOC's.
  Specs and their helpers are in `test/Addon/`, and the contract's fixtures in `test/Fixtures/Addon/`.
- **Contract:** the addon writes only the shape of the [addon snapshot contract](../reference/addon-savedvariables.md),
  schema 1, into the account-wide `RaidManagerDB`, keyed `"<realm>|<name>"`. A section the addon doesn't capture yet
  is `unavailable` with `reason = "not-captured"`, never left out.
- **Testable code:** capture functions receive the WoW API as a table and the time as a number, so busted runs them
  outside the client with stubs. A spec helper loads the files in TOC order into a sandbox, as the client does.
- **Tools:** busted 2.2.0 for specs, luacheck 1.2.0 (`.luacheckrc`, Lua 5.1, lines of 120 characters, the addon's
  global variables listed), and StyLua 2.5.2 (`.stylua.toml`, Lua 5.1, four spaces, 120 columns). The `addon`
  workflow runs all three on every pull request, with its third-party actions pinned to commit ids. Locally, they run
  in Docker, as [Test the addon](../how-to/test-the-addon.md) shows.
- **Deployment:** the addon never runs on the server, so a change under `src/Addon/` doesn't deploy dev
  (owner decision on #491).
- **Verification in the game:** specs prove the logic against the fixtures. The owner checks each addon pull
  request in a 3.3.5a client on Warmane and pastes the SavedVariables file for comparison (owner decision on #16).

## Consequences

**Positive**

- The contract is checked twice: the fixtures and every snapshot the specs write pass the same contract helper.
- Players install one folder; nothing in it reaches the network or holds a credential.

**Negative**

- A second language, with its own tools and a workflow, in a .NET repository.
- Specs can't prove that the client's API answers as the stubs do; only the owner's check in the game can.

## Alternatives considered

- **A separate repository for the addon:** keeps the tools apart, but splits the contract, its fixtures and
  the companion that reads them across two repositories.
- **No automated tests, only checks in the game:** cheaper at first, but every capture change would need a game
  session to catch a mistake the stubs find in seconds.
