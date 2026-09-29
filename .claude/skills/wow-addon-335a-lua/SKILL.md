---
name: wow-addon-335a-lua
description: 'Build, extend, review or test the RaidManager World of Warcraft 3.3.5a (WotLK, build 12340, Interface 30300,
  Warmane) addon in Lua 5.1: TOC layout, the event-frame bootstrap, SavedVariables as the versioned contract read by the
  desktop companion, capture of character identity, equipped gear and equipment sets, dual-spec talents, glyphs,
  professions, stats and raid lockouts with the exact 3.3.5a API signatures, observed-versus-unavailable encoding,
  roster import and invites, APIs that do not exist in 3.3.5a, taint and combat lockdown, luacheck/busted/StyLua
  tooling and CI. Use for any .lua, .toc or .xml addon file, SavedVariables schema change, or addon-related design.'
---

# World of Warcraft 3.3.5a addon (Lua)

Use this skill for the RaidManager in-game addon. The addon runs inside the WoW 3.3.5a client (build 12340) that
Warmane serves. It captures character facts into SavedVariables and shows the raid-night roster. It cannot do
network or file I/O: per ADR-0002 the desktop companion reads the SavedVariables file after WoW writes it and
uploads the snapshot. Read `docs/adr/0002-use-desktop-companion-for-character-sync.md`,
`docs/explanation/product.md` and `docs/glossary.md` before designing anything new.

## 1. Non-negotiable constraints

- **Lua 5.1 only.** No `goto`, no `//`, no bitwise operators (use the client's `bit.band`/`bit.bor`), no
  `table.unpack` (use `unpack`), no `table.pack`, no integer subtype. `#t` is only reliable on sequences without holes.
- **Interface 30300.** Many APIs you know from modern WoW or from later expansions do not exist (section 7). Never
  write code for an API you have not confirmed exists in 3.3.5a. When unsure, feature-detect
  (`if GetSavedInstanceEncounterInfo then ... end`) and record the fact as unavailable instead of failing.
- **No I/O.** Lua cannot read or write files, open sockets or call HTTP. SavedVariables are loaded before
  `ADDON_LOADED` and written by the client on `/reload`, logout or exit. A client crash loses unsaved changes.
- **No secrets.** Never put a RaidManager token, pairing code or any credential in SavedVariables or chat. Pairing
  and authentication belong to the companion (architecture: "must not place authentication secrets in addon files").
- **No hidden automation.** The addon never invites, kicks or changes loot on its own. Every group action is the
  direct result of a player click or slash command, and never in combat (`InCombatLockdown()`).
- **Missing is not negative.** The product requires the snapshot to distinguish an unavailable field from an
  observed empty value. Lua drops `nil` table fields, so encode status explicitly (section 4).

## 2. Repository placement and first-delivery obligations

- The addon is a new language and toolchain in a .NET repository: the first addon PR needs an ADR with status
  `Proposed` (`docs-adr` R1) covering Lua 5.1, the source location, the SavedVariables contract and the tooling below.
  Do not mark it `Accepted`.
- Proposed layout (confirm in that ADR before creating it):

```text
src/Addon/RaidManager/            ← folder name MUST equal the .toc name; this folder is what players install
├── RaidManager.toc
├── Core.lua                      ← namespace, event frame, slash commands
├── Capture/Identity.lua
├── Capture/Gear.lua
├── Capture/Talents.lua
├── Capture/Lockouts.lua
├── Snapshot.lua                  ← builds the SavedVariables snapshot
└── Roster/Import.lua, Roster/Invite.lua, Roster/Frame.lua
test/Addon/                       ← busted specs + WoW API stubs, never shipped
.luacheckrc  .stylua.toml         ← repository root
docs/reference/addon-savedvariables.md   ← the contract (section 4)
```

- Keep all pure logic (item-link parsing, snapshot building, roster-string decoding) in functions that take their
  inputs as arguments, so busted can test them outside the client with stubbed API globals.
- Documentation, CHANGELOG, diagrams (`docs/diagrams/character-sync.mmd`) and the glossary are updated in the same
  PR as any change to what the addon captures or exports (`docs-as-code` R4).

## 3. TOC and bootstrap

```text
## Interface: 30300
## Title: RaidManager
## Notes: Captures raid-relevant character data for the RaidManager companion.
## Author: Gihed Annabi
## Version: 0.1.0
## SavedVariables: RaidManagerDB

Core.lua
Capture\Identity.lua
Capture\Gear.lua
Capture\Talents.lua
Capture\Lockouts.lua
Snapshot.lua
```

- Files load in TOC order; a file may only use namespace members defined by files listed above it.
- `## SavedVariables` is account-wide: `WTF/Account/<ACCOUNT>/SavedVariables/RaidManager.lua`. Use it and key
  characters by realm and name, so the companion reads one file per WoW account (players use several accounts).
  Do not add `SavedVariablesPerCharacter` unless the ADR changes the contract.
- Every file starts with the private namespace; never create globals except `RaidManagerDB` and the `SLASH_`/
  `SlashCmdList` entries:

```lua
-- Author: Gihed Annabi
-- Purpose: Addon namespace, event dispatch and slash commands.
local addonName, ns = ...

ns.SCHEMA_VERSION = 1   -- bump with every contract change (section 4)

local frame = CreateFrame("Frame")
ns.handlers = {}

frame:SetScript("OnEvent", function(self, event, ...)
    local handler = ns.handlers[event]
    if handler then
        handler(...)
    end
end)

function ns.On(event, handler)
    ns.handlers[event] = handler
    frame:RegisterEvent(event)
end

ns.On("ADDON_LOADED", function(loadedName)
    if loadedName ~= addonName then
        return
    end
    RaidManagerDB = RaidManagerDB or {}
    RaidManagerDB.schemaVersion = ns.SCHEMA_VERSION
    RaidManagerDB.characters = RaidManagerDB.characters or {}
    frame:UnregisterEvent("ADDON_LOADED")
end)

SLASH_RAIDMANAGER1 = "/raidmanager"
SLASH_RAIDMANAGER2 = "/rm"
SlashCmdList["RAIDMANAGER"] = function(message)
    ns.HandleSlashCommand(strtrim(message or ""))
end
```

- Event handlers use `(self, event, ...)`. Never use the legacy globals `this`, `event`, `arg1`.
- `ADDON_LOADED` for your own addon is the first point where `RaidManagerDB` holds the saved data. Use
  `PLAYER_ENTERING_WORLD` for data that needs the character in the world. `PLAYER_LOGOUT` is the last event before
  the file is written: only copy cached values there, never start asynchronous requests.
- Timers: there is no `C_Timer`. Throttle with an `OnUpdate` script that accumulates `elapsed` and removes itself.

## 4. SavedVariables are a versioned contract

The companion parses this file; treat its shape like an API contract (`docs-api-contracts`).

- Document the schema in `docs/reference/addon-savedvariables.md` with every field, type, unit and example.
  A change to it updates that page, bumps `schemaVersion`, adds a CHANGELOG entry, and ships with the companion
  parser change. Removing or renaming a field is a breaking change.
- Only strings, numbers, booleans and tables. No functions, no frames, no shared or cyclic table references.
- Key characters as `RaidManagerDB.characters["<realm>|<name>"]`, with `realm` and `name` also stored as fields.
- Every captured section carries `status` (`"observed"` or `"unavailable"`) and `observedAt` (`time()` at capture).
  An observed empty lockout list is `{ status = "observed", observedAt = t, items = {} }`; a lockout query that never
  answered is `{ status = "unavailable" }`. Never omit a section to mean "none".
- Store ids and raw game values, not derived judgments: item strings, item ids, enchant and gem ids, talent points,
  glyph spell ids, the lockout seconds-to-reset and flags. GearScore, eligibility and instance mapping are computed
  by RaidManager, where the domain rules live. Do not read another addon's globals (for example a GearScore addon).
- Record `locale = GetLocale()` and `clientBuild = select(2, GetBuildInfo())`: instance names and profession names
  are localized strings, and the backend maps them.
- Record `GetGameTime()` (server hour and minute) alongside `time()` so the backend can detect a wrong client clock.
- Keep the file small: overwrite a character's snapshot, never append history in the addon.

## 5. Capturing data with the 3.3.5a API

Return values below are the 3.3.5a signatures, read by position. Anything later expansions added after the listed
values must not be relied on.

### Identity

- `UnitName("player")`, `GetRealmName()`, `UnitLevel("player")`, `UnitGUID("player")` (a hex string in 3.3.5a).
- `local _, classToken = UnitClass("player")` → locale-independent token such as `"PALADIN"`.
- `local _, raceToken = UnitRace("player")` → token such as `"Human"`, `"BloodElf"`, `"Scourge"` (Undead).
- `local factionToken = UnitFactionGroup("player")` → `"Alliance"` or `"Horde"`.
- `GetGuildInfo("player")` can return `nil` shortly after login; capture it again on `PLAYER_GUILD_UPDATE`.

### Equipped gear (full detail)

- Slots 1–19 (`GetInventorySlotInfo("HeadSlot")` resolves names; 4 is the shirt, 19 the tabard, 0 the ammo).
- `GetInventoryItemLink("player", slot)` → `|Hitem:itemId:enchantId:gem1:gem2:gem3:gem4:suffixId:uniqueId:level|h`.
  Parse the item string with `link:match("item:([%-%d:]+)")` and split on `:`; `suffixId` can be negative.
  Store the item string as well as the parsed ids.
- `GetItemInfo` returns `nil` for items missing from the client cache, so never depend on it for the snapshot.

### Equipment sets (multiple loadouts)

- `GetNumEquipmentSets()`, `local name, icon = GetEquipmentSetInfo(index)`.
- `GetEquipmentSetItemIDs(name)` returns item ids per slot; FrameXML's EquipmentManager uses sentinel values for
  empty and ignored slots. Check those constants in the 3.3.5a FrameXML before mapping them.
- Enchants and gems of a set item require its link: resolve `GetEquipmentSetLocations(name)` with
  `EquipmentManager_UnpackLocation(location)` and read `GetContainerItemLink(bag, slot)` or
  `GetInventoryItemLink`. Items in the bank are only readable while the bank is open: mark them `"unavailable"`.

### Talents and glyphs (dual specialization)

- `GetNumTalentGroups()` and `GetActiveTalentGroup()`; capture every talent group, flag the active one.
- `local tabName, _, pointsSpent = GetTalentTabInfo(tab, false, false, group)` for `tab = 1, GetNumTalentTabs()`.
  The points per tab give the build string the domain uses (`TalentConfiguration`, for example `"5-11-55"`).
- `GetTalentInfo(tab, index, false, false, group)` → name, icon, tier, column, rank, maxRank; capture rank per talent
  only if the contract asks for it.
- `GetNumGlyphSockets()`, `local enabled, glyphType, glyphSpellId = GetGlyphSocketInfo(socket, group)`
  (`glyphType` 1 major, 2 minor; an empty socket has no spell id).

### Professions and stats

- `GetProfessions` does not exist: iterate `GetNumSkillLines()` / `GetSkillLineInfo(i)` and keep non-header rows
  with their localized name, rank and max rank.
- Stats (`UnitStat`, `UnitAttackPower`, `GetCritChance`, `GetSpellBonusDamage`, `GetCombatRating`, `UnitArmor`,
  `UnitHealthMax`) reflect current auras and only the equipped gear. Attach them to the equipped loadout, record
  `observedAt`, and never copy them onto an equipment set that is not worn.

### Raid lockouts

- Call `RequestRaidInfo()` on `PLAYER_ENTERING_WORLD`, on `ZONE_CHANGED_NEW_AREA` and from `/rm sync`, then read on
  `UPDATE_INSTANCE_INFO`. Throttle the request (at most once every few seconds).
- For `i = 1, GetNumSavedInstances()`:
  `local name, lockoutId, resetSeconds, difficulty, locked, extended, idMostSig, isRaid, maxPlayers,
  difficultyName = GetSavedInstanceInfo(i)`.
- Keep entries with `locked == false` too (expired but extendable) and store the flag; eligibility is decided by
  the backend at the scheduled raid start, not by the addon.
- `resetSeconds` is relative to the moment of the event: store it together with `observedAt = time()`.
- For raids, `difficulty` is 1 = 10 normal, 2 = 25 normal, 3 = 10 heroic, 4 = 25 heroic. Store it raw with
  `maxPlayers` and `difficultyName`; the backend maps it to `RaidDifficulty`.
- `lockoutId` with `idMostSig` can exceed 32 bits; store both numbers, the backend builds the string id.
- Per-encounter progress is not guaranteed in 3.3.5a: feature-detect and mark it `"unavailable"`.

## 6. Roster import and invitations

- The addon cannot receive data from the companion while the game runs, and writing SavedVariables while the client
  is open is overwritten on logout. The import path (pasted text in an edit box versus a file prepared while the game
  is closed) is an ADR decision — ask if none exists.
- Treat imported text as untrusted: a version prefix, strict parsing, length limits, and a clear error for any row it
  cannot parse. Never execute imported text (`loadstring` is forbidden).
- The roster view shows who is selected, benched and already in the group. Invitations happen only on a click.
- 3.3.5a group API: `InviteUnit(name)`, `ConvertToRaid()` once someone joined the party, `GetNumPartyMembers()`,
  `GetNumRaidMembers()`, `GetRaidRosterInfo(i)`, event `PARTY_MEMBERS_CHANGED` and `RAID_ROSTER_UPDATE`.
  A party holds 5 players: invite at most 4 before converting to a raid, then continue.
- Addon-to-addon messages: `SendAddonMessage(prefix, text, "RAID" | "PARTY" | "GUILD" | "WHISPER", target)`;
  there is no prefix registration in 3.3.5a. Keep prefix plus text under 255 bytes and never put a tab in the prefix.

## 7. APIs and patterns that do not exist in 3.3.5a

| Do not use | Use in 3.3.5a |
| --- | --- |
| `C_Timer.After`, `C_Timer.NewTicker` | `OnUpdate` with accumulated `elapsed` |
| `C_ChatInfo.*`, `RegisterAddonMessagePrefix` | `SendAddonMessage` + `CHAT_MSG_ADDON` |
| `IsInGroup`, `IsInRaid`, `GetNumGroupMembers`, `GROUP_ROSTER_UPDATE` | `GetNumPartyMembers`, `GetNumRaidMembers`, `PARTY_MEMBERS_CHANGED`, `RAID_ROSTER_UPDATE` |
| `GetSpecialization`, `GetSpecializationInfo` | talent tabs and points spent (section 5) |
| `GetProfessions` | `GetNumSkillLines` / `GetSkillLineInfo` |
| `GetItemInfoInstant`, `C_Item.*`, `C_Container.*` | item-string parsing, `GetContainerItemLink` |
| `"BackdropTemplate"` template | `frame:SetBackdrop(...)` directly |
| `Mixin`, `CreateFromMixins`, `C_*` namespaces in general | plain Lua tables and functions |
| `table.unpack`, `goto`, `//`, `&`, `|` | `unpack`, loops, `math.floor(a / b)`, `bit.*` |

## 8. Taint, performance and UI

- Never replace or modify Blizzard functions or secure frames; use `hooksecurefunc` for post-hooks only.
- Nothing protected in combat: check `InCombatLockdown()` and defer with `PLAYER_REGEN_ENABLED`.
- Scan on events, never every frame. Unregister events you no longer need. Reuse tables in hot paths (`wipe(t)`).
- Frames are created once and reused; give them names only when Blizzard code or saved positions need them.
- User-facing text goes through a locale table (`ns.L`); captured data stays raw and unlocalized.
- Chat output via `print` or `DEFAULT_CHAT_FRAME:AddMessage`, prefixed with `RaidManager:`, and only on action.

## 9. Code style, tooling and tests

- Follow the repository's `.editorconfig` (4 spaces). `local` everything; one module per file attached to `ns`.
- Document public functions with Lua Language Server annotations (`---@param`, `---@return`, `---@class`), and
  begin each file with the author and purpose header shown in section 3 (author: Gihed Annabi).
- `.luacheckrc`: `std = "lua51"`, `max_line_length = 120`, and a `read_globals` list of exactly the WoW API
  functions the addon calls (keeping that list short documents the API surface). `globals = { "RaidManagerDB",
  "SLASH_RAIDMANAGER1", "SLASH_RAIDMANAGER2", "SlashCmdList" }`.
- `.stylua.toml`: `syntax = "Lua51"`, `indent_type = "Spaces"`, `indent_width = 4`, `column_width = 120`.
- Tests: busted on Lua 5.1 in `test/Addon/`, with a stub file defining the WoW functions a spec needs. Cover item
  string parsing (negative suffix, empty gem sockets), the snapshot status encoding, lockout mapping and roster
  string decoding (valid, truncated, wrong version, oversized). Name specs by behaviour.
- CI (GitHub Actions): `leafo/gh-actions-lua` with Lua 5.1 and `leafo/gh-actions-luarocks`, then
  `luarocks install luacheck`, `luarocks install busted`, and run `luacheck src/Addon test/Addon`,
  `stylua --check src/Addon test/Addon` and `busted test/Addon`. Pin action versions.
- Package a release as a zip whose top folder is `RaidManager/` (the TOC folder); exclude tests and tooling files.

## 10. Verifying against the real client

- Automated tests prove logic, not API behaviour. Before finishing, state which behaviour was checked in a 3.3.5a
  client and which was not. Never claim in-game verification you did not do.
- Quick in-game checks: `/run print(GetSavedInstanceInfo(1))`, `/reload` then read
  `WTF/Account/<ACCOUNT>/SavedVariables/RaidManager.lua`, and `/console scriptErrors 1` to surface Lua errors.
- Test on a character with an extended lockout, one with no equipment sets, one with a single talent group, a
  non-English client when possible, and a character name with non-ASCII letters (the file stores UTF-8 bytes).

## Completion criteria

- Lua 5.1 and Interface 30300 only; every API used exists in 3.3.5a or is feature-detected.
- No globals besides the saved table and slash-command entries; no secrets; no automatic group actions.
- Snapshot sections carry `status` and `observedAt`; unavailable is never encoded as empty.
- SavedVariables contract page, `schemaVersion`, CHANGELOG, glossary and diagrams updated with any shape change.
- luacheck, StyLua and busted pass; the in-game verification status is stated plainly.
- First addon delivery includes the `Proposed` ADR for the toolchain, location and contract.

## Example prompts

- "Create the RaidManager addon skeleton with the TOC, namespace and lockout capture."
- "Capture every equipment set with enchants and gems into the snapshot."
- "Add a /rm import command that reads the officer's roster string and offers invite buttons."
- "Review this addon Lua for APIs that do not exist in 3.3.5a."
