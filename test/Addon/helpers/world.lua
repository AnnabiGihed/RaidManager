-- Author: Gihed Annabi
-- Purpose: Stubs of the WoW 3.3.5a API that answer as the client does for the one-character fixture's character.
local M = {}

-- 2026-10-04 12:00:00 UTC, the fixture's observation time.
M.NOW = 1791043200

--- The API of Arthasdk on Icecrown, with any function replaced by the given overrides.
---@param overrides table|nil
---@return table
function M.Arthasdk(overrides)
    local world = {
        time = function()
            return M.NOW
        end,
        GetRealmName = function()
            return "Icecrown"
        end,
        UnitName = function()
            return "Arthasdk"
        end,
        UnitGUID = function()
            return "0x0700000001A2B3C4"
        end,
        UnitLevel = function()
            return 80
        end,
        UnitClass = function()
            return "Death Knight", "DEATHKNIGHT"
        end,
        UnitRace = function()
            return "Undead", "Scourge"
        end,
        UnitFactionGroup = function()
            return "Horde", "Horde"
        end,
        UnitSex = function()
            return 2
        end,
        IsInGuild = function()
            return 1
        end,
        GetGuildInfo = function()
            return "Dark Templars", "Officer", 1
        end,
        GetLocale = function()
            return "enUS"
        end,
        GetBuildInfo = function()
            return "3.3.5", "12340", "Jun 24 2010", 30300
        end,
        GetGameTime = function()
            return 14, 0
        end,
    }
    for name, value in pairs(overrides or {}) do
        world[name] = value
    end
    return world
end

--- A function that returns nothing, as the client does for data it hasn't loaded.
function M.Nothing()
    return nil
end

return M
