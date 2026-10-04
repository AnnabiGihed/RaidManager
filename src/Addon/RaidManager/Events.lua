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

ns.On("PLAYER_GUILD_UPDATE", function(unit)
    if unit == nil or unit == "player" then
        ns.CaptureGuildAgain(ns.api, RaidManagerDB, ns.api.time())
    end
end)

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
