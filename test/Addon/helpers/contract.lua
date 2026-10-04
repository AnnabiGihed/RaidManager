-- Author: Gihed Annabi
-- Purpose: Checks a RaidManagerDB against schema 1 of docs/reference/addon-savedvariables.md.
local M = {}

local SECTIONS = { "identity", "guild", "professions", "equipped", "equipmentSets", "talents", "lockouts" }
local UNAVAILABLE_FIELDS = { status = true, reason = true, attemptedAt = true }
local LISTS = { professions = true, equipmentSets = true, lockouts = true }

local function itemProblems(where, entry, problems)
    local ids = {}
    for part in (entry.itemString:match("^item:([%-%d:]+)$") or ""):gmatch("[^:]+") do
        ids[#ids + 1] = tonumber(part)
    end
    if #ids ~= 9 then
        problems[#problems + 1] = where .. ": the item string has " .. #ids .. " parts"
        return
    end
    if ids[1] ~= entry.itemId or ids[2] ~= entry.enchantId or ids[7] ~= entry.suffixId or ids[8] ~= entry.uniqueId then
        problems[#problems + 1] = where .. ": the item string doesn't match its ids"
    end
    for gem = 1, 4 do
        if entry.gems[gem] ~= ids[2 + gem] then
            problems[#problems + 1] = where .. ": gem " .. gem .. " doesn't match the item string"
        end
    end
end

local function slotProblems(where, slots, inSet, problems)
    if #slots ~= 19 then
        problems[#problems + 1] = where .. ": " .. #slots .. " slots instead of 19"
    end
    for index, entry in ipairs(slots) do
        local at = where .. ".slots[" .. index .. "]"
        local kinds = (entry.empty and 1 or 0)
            + (entry.status == "unavailable" and 1 or 0)
            + (entry.itemString and 1 or 0)
            + (entry.ignored and 1 or 0)
        if entry.slot ~= index then
            problems[#problems + 1] = at .. ": slot " .. tostring(entry.slot)
        end
        if kinds ~= 1 then
            problems[#problems + 1] = at .. ": not exactly one of item, empty, ignored or unavailable"
        end
        if entry.itemString then
            itemProblems(at, entry, problems)
        end
        if inSet and entry.itemString and not entry.location then
            problems[#problems + 1] = at .. ": a set item without its location"
        end
        if not inSet and entry.ignored then
            problems[#problems + 1] = at .. ": ignored outside an equipment set"
        end
    end
end

local function talentProblems(where, section, problems)
    for _, group in ipairs(section.groups) do
        for _, tab in ipairs(group.tabs) do
            local sum = 0
            for digit in tab.ranks:gmatch("%d") do
                sum = sum + tonumber(digit)
            end
            if sum ~= tab.pointsSpent then
                problems[#problems + 1] = where .. ": " .. tab.name .. " ranks add up to " .. sum
            end
        end
        for _, glyph in ipairs(group.glyphs) do
            if glyph.enabled and not glyph.empty and not glyph.spellId then
                problems[#problems + 1] = where .. ": an unlocked glyph socket without a glyph or empty = true"
            end
        end
    end
end

local function observedProblems(where, name, section, problems)
    if name == "guild" and section.inGuild == false and (section.name or section.rank) then
        problems[#problems + 1] = where .. ": no guild, but a guild name"
    end
    if name == "guild" and section.inGuild == nil then
        problems[#problems + 1] = where .. ": inGuild is missing"
    end
    if LISTS[name] and type(section.items) ~= "table" then
        problems[#problems + 1] = where .. ": items is missing"
    end
    if name == "lockouts" and type(section.complete) ~= "boolean" then
        problems[#problems + 1] = where .. ": complete is missing"
    end
    if name == "equipped" then
        slotProblems(where, section.slots, false, problems)
    end
    if name == "equipmentSets" then
        for index, set in ipairs(section.items) do
            slotProblems(where .. ".items[" .. index .. "]", set.slots, true, problems)
        end
    end
    if name == "talents" then
        talentProblems(where, section, problems)
    end
end

local function sectionProblems(where, name, section, problems)
    if type(section) ~= "table" then
        problems[#problems + 1] = where .. ": missing"
    elseif section.status == "unavailable" then
        for field in pairs(section) do
            if not UNAVAILABLE_FIELDS[field] then
                problems[#problems + 1] = where .. ": an unavailable section with " .. field
            end
        end
    elseif section.status ~= "observed" then
        problems[#problems + 1] = where .. ": status " .. tostring(section.status)
    elseif type(section.observedAt) ~= "number" then
        problems[#problems + 1] = where .. ": observed without observedAt"
    else
        observedProblems(where, name, section, problems)
    end
end

--- Lists what in a RaidManagerDB breaks the contract; an empty list means it complies.
---@param db table
---@return string[]
function M.Problems(db)
    local problems = {}
    if db.schemaVersion ~= 1 then
        problems[#problems + 1] = "schemaVersion is " .. tostring(db.schemaVersion)
    end
    for key, character in pairs(db.characters or {}) do
        if key ~= tostring(character.realm) .. "|" .. tostring(character.name) then
            problems[#problems + 1] = key .. ": the key doesn't match realm and name"
        end
        if type(character.capturedAt) ~= "number" then
            problems[#problems + 1] = key .. ": capturedAt is missing"
        end
        for _, name in ipairs(SECTIONS) do
            sectionProblems(key .. "." .. name, name, character[name], problems)
        end
    end
    return problems
end

return M
