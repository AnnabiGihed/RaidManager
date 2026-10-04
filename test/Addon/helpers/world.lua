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

--- The equipment manager, bag and location functions that answer with a character's equipment sets.
---@param sets table[] the items of an equipmentSets section
---@return table
function M.EquipmentManager(sets)
    local bag, bagSlot, links = 0, 0, {}
    local itemIds, locations = {}, {}
    for _, set in ipairs(sets) do
        local ids, places = {}, {}
        for _, entry in ipairs(set.slots) do
            if entry.ignored then
                ids[entry.slot], places[entry.slot] = 1, 1
            elseif entry.empty then
                ids[entry.slot], places[entry.slot] = 0, 0
            elseif entry.location == "bank" then
                ids[entry.slot] = entry.itemId
                places[entry.slot] = { bank = true, slot = entry.slot }
            elseif entry.location == "bags" then
                bagSlot = bagSlot + 1
                links[bag .. ":" .. bagSlot] = M.Link(entry.itemString)
                ids[entry.slot] = entry.itemId
                places[entry.slot] = { bags = true, bag = bag, slot = bagSlot }
            else
                ids[entry.slot] = entry.itemId or 0
                places[entry.slot] = { player = true, slot = entry.slot }
            end
        end
        itemIds[set.name], locations[set.name] = ids, places
    end
    return {
        GetNumEquipmentSets = function()
            return #sets
        end,
        GetEquipmentSetInfo = function(index)
            return sets[index].name, sets[index].icon
        end,
        GetEquipmentSetItemIDs = function(name)
            return itemIds[name]
        end,
        GetEquipmentSetLocations = function(name)
            return locations[name]
        end,
        EquipmentManager_UnpackLocation = function(location)
            return location.player, location.bank, location.bags, location.slot, location.bag
        end,
        GetContainerItemLink = function(atBag, atSlot)
            return links[atBag .. ":" .. atSlot]
        end,
    }
end

--- The talent and glyph functions that answer with a talents section.
---@param talents table an observed talents section
---@return table
function M.TalentFrame(talents)
    return {
        GetNumTalentGroups = function()
            return #talents.groups
        end,
        GetActiveTalentGroup = function()
            return talents.activeGroup
        end,
        GetNumTalentTabs = function()
            return #talents.groups[1].tabs
        end,
        GetTalentTabInfo = function(tab, _, _, group)
            local entry = talents.groups[group].tabs[tab]
            return entry.name, "Interface\\TalentFrame\\Icon", entry.pointsSpent
        end,
        GetNumTalents = function(tab)
            return #talents.groups[1].tabs[tab].ranks
        end,
        GetTalentInfo = function(tab, index, _, _, group)
            local rank = tonumber(talents.groups[group].tabs[tab].ranks:sub(index, index))
            return "Talent " .. index, "Interface\\Icons\\Talent", 1, 1, rank, 5
        end,
        GetNumGlyphSockets = function()
            return #talents.groups[1].glyphs
        end,
        GetGlyphSocketInfo = function(socket, group)
            local glyph = talents.groups[group].glyphs[socket]
            return glyph.enabled and 1 or nil, glyph.type, glyph.spellId
        end,
    }
end

--- The saved-instance functions that answer with a lockouts section; an entry false stands for one not read.
---@param lockouts table an observed lockouts section, or a list of entries and false
---@return table
function M.SavedInstances(lockouts)
    local entries = lockouts.items or lockouts
    local function flag(value)
        return value and 1 or nil
    end
    local world = { requests = 0 }
    world.RequestRaidInfo = function()
        world.requests = world.requests + 1
    end
    world.GetNumSavedInstances = function()
        return #entries
    end
    world.GetSavedInstanceInfo = function(index)
        local entry = entries[index]
        if not entry then
            return nil
        end
        return entry.name,
            entry.lockoutId,
            entry.resetSeconds,
            entry.difficulty,
            flag(entry.locked),
            flag(entry.extended),
            entry.idMostSig,
            flag(entry.isRaid),
            entry.maxPlayers,
            entry.difficultyName
    end
    return world
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
    local character = addon.FixtureCharacter()
    local managers = {
        M.EquipmentManager(character.equipmentSets.items),
        M.TalentFrame(character.talents),
        M.SavedInstances(character.lockouts),
    }
    for _, functions in ipairs(managers) do
        for name, value in pairs(functions) do
            world[name] = value
        end
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
