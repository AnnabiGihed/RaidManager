-- Author: Gihed Annabi
-- Purpose: Stubs of the WoW 3.3.5a API that answer as the client does for the one-character fixture's character.
local addon = dofile("test/Addon/helpers/addon.lua")

local M = {}

-- 2026-10-04 12:00:00 UTC, the fixture's observation time.
M.NOW = 1791043200

-- The skill list as the client shows it: headers, then their rows; the fixture's professions are under two of them.
M.SKILL_LINES = {
    { "Professions", true, true },
    { "Blacksmithing", false, false, 450, 0, 0, 450 },
    { "Mining", false, false, 450, 0, 0, 450 },
    { "Secondary Skills", true, true },
    { "Cooking", false, false, 300, 0, 0, 450 },
    { "First Aid", false, false, 450, 0, 0, 450 },
    { "Fishing", false, false, 1, 0, 0, 75 },
    { "Weapon Skills", true, true },
    { "Two-Handed Swords", false, false, 400, 0, 0, 400 },
    { "Languages", true, true },
    { "Orcish", false, false, 300, 0, 0, 300 },
}

--- The client's link for an item string, as GetInventoryItemLink returns it.
---@param itemString string
---@return string
function M.Link(itemString)
    return "|cffa335ee|H" .. itemString .. "|h[Item]|h|r"
end

--- The skill list functions for a list of rows.
---@param lines table[]
---@return table
function M.SkillList(lines)
    return {
        GetNumSkillLines = function()
            return #lines
        end,
        GetSkillLineInfo = function(index)
            return unpack(lines[index])
        end,
    }
end

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
    -- The localized header names, as the client's global strings define them.
    world.TRADE_SKILLS = "Professions"
    world.SECONDARY_SKILLS = "Secondary Skills:"
    for name, value in pairs(M.SkillList(M.SKILL_LINES)) do
        world[name] = value
    end
    local equipped = addon.FixtureCharacter().equipped.slots
    world.GetInventoryItemLink = function(_, slot)
        local entry = equipped[slot]
        return entry and entry.itemString and M.Link(entry.itemString) or nil
    end
    world.GetInventoryItemTexture = function(_, slot)
        local entry = equipped[slot]
        return entry and entry.itemString and "Interface\\Icons\\INV_Misc_QuestionMark" or nil
    end
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
