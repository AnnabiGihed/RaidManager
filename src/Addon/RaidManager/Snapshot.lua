-- Author: Gihed Annabi
-- Purpose: Builds the SavedVariables snapshot of the contract in docs/reference/addon-savedvariables.md (schema 1).
local _, ns = ...

ns.SCHEMA_VERSION = 1
ns.ADDON_VERSION = "0.2.0"

-- Every character snapshot has these sections; one is never left out to mean "none".
ns.SECTIONS = { "identity", "guild", "professions", "equipped", "equipmentSets", "talents", "lockouts" }

-- The reason given for a section this version of the addon doesn't capture yet.
ns.NOT_CAPTURED = "not-captured"

--- A section the game answered: its status, the time of the observation and its data.
---@param observedAt number seconds since 1970, from time()
---@param data table the section's fields
---@return table
function ns.Observed(observedAt, data)
    local section = { status = "observed", observedAt = observedAt }
    for key, value in pairs(data) do
        section[key] = value
    end
    return section
end

--- A section the game didn't answer, or that can't be read now: never written as an empty one.
---@param reason string such as "not-loaded"
---@param attemptedAt number seconds since 1970, from time()
---@return table
function ns.Unavailable(reason, attemptedAt)
    return { status = "unavailable", reason = reason, attemptedAt = attemptedAt }
end

--- The key of a character in RaidManagerDB.characters.
---@param realm string
---@param name string
---@return string
function ns.CharacterKey(realm, name)
    return realm .. "|" .. name
end

--- Prepares the saved table: the schema and addon versions, and the characters it keeps.
---@param db table|nil the saved RaidManagerDB, nil on the first load
---@return table
function ns.InitializeDatabase(db)
    db = db or {}
    db.schemaVersion = ns.SCHEMA_VERSION
    db.addonVersion = ns.ADDON_VERSION
    db.characters = db.characters or {}
    return db
end

--- Returns a character's snapshot; a new one has every section unavailable until it is captured.
---@param db table the prepared RaidManagerDB
---@param realm string
---@param name string
---@param now number seconds since 1970
---@return table
function ns.CharacterSnapshot(db, realm, name, now)
    local key = ns.CharacterKey(realm, name)
    local character = db.characters[key]
    if not character then
        character = { realm = realm, name = name, capturedAt = now }
        for _, section in ipairs(ns.SECTIONS) do
            character[section] = ns.Unavailable(ns.NOT_CAPTURED, now)
        end
        db.characters[key] = character
    end
    return character
end

--- Replaces one section of a character's snapshot and records when the snapshot changed.
---@param character table
---@param section string one of ns.SECTIONS
---@param value table an observed or unavailable section
---@param now number seconds since 1970
function ns.StoreSection(character, section, value, now)
    character[section] = value
    character.capturedAt = now
end
