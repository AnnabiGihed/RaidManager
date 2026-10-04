-- Author: Gihed Annabi
-- Purpose: Captures the logged-in character into its snapshot, from the sections this version knows.
local _, ns = ...

-- The sections this version captures, each with its capture function; the others stay unavailable (not-captured).
ns.CAPTURES = {
    { section = "identity", capture = ns.CaptureIdentity },
    { section = "guild", capture = ns.CaptureGuild },
    { section = "professions", capture = ns.CaptureProfessions },
    { section = "equipped", capture = ns.CaptureEquipped },
}

local function captureOf(section)
    for _, entry in ipairs(ns.CAPTURES) do
        if entry.section == section then
            return entry.capture
        end
    end
    return nil
end

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
    for _, entry in ipairs(ns.CAPTURES) do
        ns.StoreSection(character, entry.section, entry.capture(api, now), now)
    end
    return character
end

--- Captures one section again, when the game reports that it changed or finished loading.
---@param api table the WoW API
---@param db table the prepared RaidManagerDB
---@param now number seconds since 1970
---@param section string a section of ns.CAPTURES
---@return table|nil the snapshot, or nil when the character isn't captured yet
function ns.CaptureAgain(api, db, now, section)
    local realm, name = api.GetRealmName(), api.UnitName("player")
    local character = realm and name and db.characters[ns.CharacterKey(realm, name)]
    local capture = captureOf(section)
    if not character or not capture then
        return nil
    end
    ns.StoreSection(character, section, capture(api, now), now)
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
