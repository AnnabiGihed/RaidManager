-- Author: Gihed Annabi
-- Purpose: Connects the game's events and the slash command to the capture.
local addonName, ns = ...

-- In the client, the WoW API is the game's global table.
ns.api = ns.api or _G

local function capture()
    return ns.CaptureCharacter(ns.api, RaidManagerDB, ns.api.time())
end

ns.On("ADDON_LOADED", function(loadedName)
    if loadedName ~= addonName then
        return
    end
    RaidManagerDB = ns.InitializeDatabase(RaidManagerDB)
    ns.Off("ADDON_LOADED")
end)

ns.On("PLAYER_ENTERING_WORLD", capture)

local function captureAgain(sections)
    local now = ns.api.time()
    for _, section in ipairs(sections) do
        ns.CaptureAgain(ns.api, RaidManagerDB, now, section)
    end
end

-- For events about a unit: only the player's own changes count.
local function forPlayer(sections)
    return function(unit)
        if unit == nil or unit == "player" then
            captureAgain(sections)
        end
    end
end

-- For events without a unit.
local function always(sections)
    return function()
        captureAgain(sections)
    end
end

-- Sections the game finishes loading, or that change, after the character entered the world.
ns.On("PLAYER_GUILD_UPDATE", forPlayer({ "guild" }))
ns.On("SKILL_LINES_CHANGED", always({ "professions" }))
-- A set's items move between the character and the bags when the gear changes.
ns.On("UNIT_INVENTORY_CHANGED", forPlayer({ "equipped", "equipmentSets" }))
ns.On("EQUIPMENT_SETS_CHANGED", always({ "equipmentSets" }))
ns.On("PLAYER_TALENT_UPDATE", always({ "talents" }))
ns.On("ACTIVE_TALENT_GROUP_CHANGED", always({ "talents" }))
ns.On("GLYPH_ADDED", always({ "talents" }))
ns.On("GLYPH_REMOVED", always({ "talents" }))
ns.On("GLYPH_UPDATED", always({ "talents" }))

--- Handles /rm and /raidmanager: "sync" captures again, anything else shows what the snapshot holds.
---@param message string the text after the command, trimmed
function ns.HandleSlashCommand(message)
    local character = message == "sync" and capture() or nil
    if not character then
        local realm, name = ns.api.GetRealmName(), ns.api.UnitName("player")
        character = realm and name and RaidManagerDB.characters[ns.CharacterKey(realm, name)]
    end
    if not character then
        ns.api.print("RaidManager: this character isn't captured yet; type /rm sync.")
        return
    end
    for _, line in ipairs(ns.DescribeSnapshot(character)) do
        ns.api.print("RaidManager: " .. line)
    end
end
