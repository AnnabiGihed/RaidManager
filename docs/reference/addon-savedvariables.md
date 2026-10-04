# Addon snapshot contract (SavedVariables)

The RaidManager addon for World of Warcraft 3.3.5a writes what it observes about each character the player logs
into to `WTF/Account/<ACCOUNT>/SavedVariables/RaidManager.lua`. The desktop companion reads that file and uploads
it, and the API turns it into character facts. This page is the contract between the three: the addon writes exactly
this shape, and the companion and the API accept it. It is schema version **1**.

The addon has no network access and never stores the website's authentication secret or any other credential
(story #16). WoW writes the file on `/reload`, logout or client exit, never while the player plays.

## Rules

- **Values:** only strings, numbers, booleans and tables; no functions and no shared or cyclic tables.
- **Raw game values:** the addon stores what the game returns (ids, item strings, tokens, localized names, seconds),
  never a judgment such as a GearScore, a role, an instance mapping or an eligibility. RaidManager derives those.
- **One snapshot per character:** a new capture replaces the character's sections; the file keeps no history.
- **Observed or unavailable:** every section is a table with a `status`:
  - `"observed"`: the game answered. The section has `observedAt` and its data. An observed absence is explicit,
    such as an empty `items` list, `empty = true` on a gear slot or `inGuild = false`.
  - `"unavailable"`: the game didn't answer, or the data can't be read now. The section has no data, and may have
    `reason` and `attemptedAt`. The addon gives `reason = "not-captured"` for a section its version doesn't capture
    yet.

  A section is never left out to mean "none", and an unavailable value is never written as an empty one.
- **Times:** `observedAt` and `attemptedAt` are `time()` on the player's computer, in seconds since 1970 (UTC).
  `serverTime` records the game server's clock at the same moment, so RaidManager can detect a wrong local clock.
- **Versions:** `schemaVersion` changes with any change to this shape. Removing or renaming a field is a breaking
  change and needs a new version, this page, a changelog entry and the companion change in the same release.

## File

```lua
RaidManagerDB = {
    ["schemaVersion"] = 1,
    ["addonVersion"] = "0.1.0",
    ["characters"] = {
        ["Icecrown|Arthasdk"] = { --[[ one character snapshot ]] },
    },
}
```

| Field | Type | Meaning |
| --- | --- | --- |
| `schemaVersion` | number | The version of this contract: `1`. |
| `addonVersion` | string | The addon's version from its TOC. |
| `characters` | table | One snapshot per character, keyed `"<realm>\|<name>"` with the names as the game returns them, in `UTF-8`. |

## Character snapshot

| Field | Type | Meaning |
| --- | --- | --- |
| `realm` | string | `GetRealmName()`, such as `"Icecrown"`. |
| `name` | string | `UnitName("player")`. |
| `capturedAt` | number | `time()` when the addon last wrote this snapshot. |
| `client` | table | `locale` (`GetLocale()`, such as `"enUS"`) and `build` (`"12340"`), to map localized names. |
| `serverTime` | table | `hour` and `minute` from `GetGameTime()`, with `observedAt`. |
| `identity` | section | Who the character is. |
| `guild` | section | The character's guild. |
| `professions` | section | Primary and secondary professions. |
| `equipped` | section | The gear the character wears. |
| `equipmentSets` | section | Saved equipment sets: the character's loadouts. |
| `talents` | section | Talent groups (dual specialization) with points, ranks and glyphs. |
| `lockouts` | section | Saved instances, raids included, with reset data. |

### `identity`

| Field | Type | Meaning |
| --- | --- | --- |
| `guid` | string | `UnitGUID("player")`, a hex string such as `"0x0700000001A2B3C4"`. |
| `level` | number | `UnitLevel("player")`. |
| `class` | string | The class token from `UnitClass("player")`, such as `"DEATHKNIGHT"`. |
| `race` | string | The race token from `UnitRace("player")`, such as `"Scourge"`. |
| `faction` | string | `"Alliance"` or `"Horde"`. |
| `sex` | number | `UnitSex("player")`: `2` male, `3` female. |

### `guild`

| Field | Type | Meaning |
| --- | --- | --- |
| `inGuild` | boolean | `false` when the character is in no guild; then no other field is present. |
| `name` | string | The guild name. |
| `rank` | string | The character's rank name. |
| `rankIndex` | number | The rank index, `0` for the guild master. |

`GetGuildInfo("player")` can return nothing shortly after login: the section is then `"unavailable"` with
`reason = "not-loaded"` until `PLAYER_GUILD_UPDATE` answers.

### `professions`

`items` lists the skill lines under the professions and secondary skills headers, in the game's order.

| Field | Type | Meaning |
| --- | --- | --- |
| `items[].name` | string | The localized profession name, such as `"Tailoring"`. |
| `items[].header` | string | The localized header it was listed under, such as `"Professions"` or `"Secondary Skills"`. |
| `items[].rank` | number | The current skill. |
| `items[].maxRank` | number | The maximum skill of the character's current training. |

A character with no profession has `items = {}`. The headers are found by the client's localized names
(`TRADE_SKILLS` and `SECONDARY_SKILLS`, without a final colon). The section is `"unavailable"` with
`reason = "collapsed"` when the player collapsed one of these headers in the skill list, which hides its rows, and
with `reason = "not-loaded"` while the skill list is empty.

### `equipped`

`slots` lists the inventory slots 1 to 19 in order, one entry each.

| Field | Type | Meaning |
| --- | --- | --- |
| `slots[].slot` | number | The inventory slot, `1` (head) to `19` (tabard); `4` is the shirt. |
| `slots[].empty` | boolean | `true` when nothing is equipped there; then no item field is present. |
| `slots[].status` | string | `"unavailable"` when the slot holds an item the game didn't describe; then no item field is present. |
| `slots[].itemString` | string | The item string from the link, `"item:itemId:enchantId:gem1:gem2:gem3:gem4:suffixId:uniqueId:level"`, where `gem1` to `gem4` are gem enchantment ids. |
| `slots[].itemId` | number | The item id. |
| `slots[].enchantId` | number | The enchant id, `0` when none. |
| `slots[].gems` | table | The four gem enchantment ids in socket order, such as `3628`, `0` for an empty or missing socket. They aren't the gems' item ids: RaidManager maps them to gems. |
| `slots[].suffixId` | number | The random suffix id; it can be negative. |
| `slots[].uniqueId` | number | The unique id. |

A slot is `"unavailable"` with `reason = "not-cached"` when the game shows an item there but gives no link yet, and
with `reason = "unreadable-link"` when the link holds no complete item string.

### `equipmentSets`

`items` lists the equipment sets saved in the game's equipment manager. A character with none has `items = {}`.

| Field | Type | Meaning |
| --- | --- | --- |
| `items[].name` | string | The set's name, such as `"Unholy"`. |
| `items[].icon` | string | The set's icon texture path. |
| `items[].slots[]` | table | One entry per slot 1 to 19, with the fields of `equipped`, plus the ones below. |
| `items[].slots[].ignored` | boolean | `true` when the set ignores the slot. |
| `items[].slots[].location` | string | Where the item is: `"equipped"`, `"bags"` or `"bank"`. |

A set item in the bank is only readable while the bank is open: its slot is `status = "unavailable"` with
`reason = "in-bank"` and the `itemId` the set records.

### `talents`

| Field | Type | Meaning |
| --- | --- | --- |
| `activeGroup` | number | `GetActiveTalentGroup()`: `1` or `2`. |
| `groups[]` | table | One entry per talent group, `1` to `GetNumTalentGroups()`. |
| `groups[].group` | number | The talent group. |
| `groups[].tabs[]` | table | One entry per talent tree, in the game's order. |
| `groups[].tabs[].name` | string | The localized tree name, such as `"Blood"`. |
| `groups[].tabs[].pointsSpent` | number | The points spent in the tree. |
| `groups[].tabs[].ranks` | string | One digit per talent of the tree, in `GetTalentInfo` index order: its current rank. |
| `groups[].glyphs[]` | table | One entry per glyph socket, `1` to `GetNumGlyphSockets()`. |
| `groups[].glyphs[].socket` | number | The socket. |
| `groups[].glyphs[].type` | number | `1` major, `2` minor. |
| `groups[].glyphs[].enabled` | boolean | Whether the socket is unlocked at the character's level. |
| `groups[].glyphs[].empty` | boolean | `true` when an unlocked socket has no glyph. |
| `groups[].glyphs[].spellId` | number | The spell id of the glyph. |

### `lockouts`

| Field | Type | Meaning |
| --- | --- | --- |
| `complete` | boolean | `true` when every saved instance the game counted was read. Only a complete scan replaces a character's raid saves; an incomplete one is kept as evidence without erasing them. |
| `items[]` | table | One entry per saved instance, raids and dungeons, in `GetSavedInstanceInfo` order. |
| `items[].name` | string | The localized instance name, such as `"Icecrown Citadel"`. |
| `items[].lockoutId` | number | The lower part of the lockout id. |
| `items[].idMostSig` | number | The upper part of the lockout id; RaidManager combines the two. |
| `items[].resetSeconds` | number | Seconds until the reset, counted from the section's `observedAt`. |
| `items[].difficulty` | number | The raw difficulty; for raids `1` 10 normal, `2` 25 normal, `3` 10 heroic, `4` 25 heroic. |
| `items[].difficultyName` | string | The localized difficulty name. |
| `items[].maxPlayers` | number | The instance size. |
| `items[].isRaid` | boolean | `true` for a raid. |
| `items[].locked` | boolean | `false` for an expired save the player can still extend. |
| `items[].extended` | boolean | `true` when the player extended the save. |

A character with no save has `complete = true` and `items = {}`. When `UPDATE_INSTANCE_INFO` never answered,
the section is `"unavailable"` with `reason = "not-answered"`.

## Not in schema 1

Combat statistics (`UnitStat` and the like), per-encounter progress and the bank's contents aren't captured: the
stories of this release don't need them, and statistics depend on auras at the moment of capture. Adding one is a
new schema version.

## Fixtures

The fixtures in `test/Fixtures/Addon/SavedVariables/` are files in the shape WoW writes, each under the folder tree a
player's installation has. The addon's tests, the companion and the API parse them. The game indents with tabs and
the fixtures with spaces, so a parser must not depend on spacing or on the `-- [n]` comments after list entries.

| Fixture | What it shows |
| --- | --- |
| `one-character` | One account, one character, every section observed: full gear with enchants, gems and a negative suffix, two talent groups with glyphs and an empty socket, two equipment sets, a guild, and observed empty saves. |
| `multiple-accounts` | Two account folders and three characters on two realms, one with an accented letter in its name, one in no guild, one with no profession and no equipment set. |
| `incomplete-scan` | A character captured right after login: guild and saves unavailable, one gear slot unavailable, a set item in the bank, an incomplete second capture of saves. |
| `raid-save` | Raid saves with reset data: a locked 25-player save, an extended 10-player heroic save, an expired save that can still be extended, a dungeon save, and a lockout id with an upper part. |
