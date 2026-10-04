-- Author: Gihed Annabi
-- Purpose: Captures the logged-in character into its snapshot, from the sections this version knows.
local _, ns = ...

--- Captures the logged-in character into the saved table and returns its snapshot.
---@param api table the WoW API
---@param db table the prepared RaidManagerDB
---@param now number seconds since 1970
---@return table|nil the snapshot, or nil while the game doesn't know the character's name and realm yet
function ns.CaptureCharacter(api, db, now)
    local realm, name = api.GetRealmName(), api.UnitName("player")
    if not realm or not name then
        return nil
    end
    local character = ns.CharacterSnapshot(db, realm, name, now)
    character.client = ns.CaptureClient(api)
    character.serverTime = ns.CaptureServerTime(api, now)
    ns.StoreSection(character, "identity", ns.CaptureIdentity(api, now), now)
    ns.StoreSection(character, "guild", ns.CaptureGuild(api, now), now)
    return character
end

--- Captures the guild again, once the game has loaded it after login.
---@param api table the WoW API
---@param db table the prepared RaidManagerDB
---@param now number seconds since 1970
---@return table|nil the snapshot, or nil when the character isn't captured yet
function ns.CaptureGuildAgain(api, db, now)
    local realm, name = api.GetRealmName(), api.UnitName("player")
    local character = realm and name and db.characters[ns.CharacterKey(realm, name)]
    if not character then
        return nil
    end
    ns.StoreSection(character, "guild", ns.CaptureGuild(api, now), now)
    return character
end

--- Describes a character's snapshot in one line per section, for the slash command.
---@param character table
---@return string[]
function ns.DescribeSnapshot(character)
    local lines = { ns.CharacterKey(character.realm, character.name) }
    for _, section in ipairs(ns.SECTIONS) do
        local value = character[section]
        local detail = value.status == "unavailable" and (" (" .. tostring(value.reason) .. ")") or ""
        lines[#lines + 1] = section .. ": " .. value.status .. detail
    end
    return lines
end
