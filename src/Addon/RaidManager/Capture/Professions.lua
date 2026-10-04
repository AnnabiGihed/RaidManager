-- Author: Gihed Annabi
-- Purpose: Captures the character's primary and secondary professions from the skill lines (WoW 3.3.5a API).
-- GetProfessions doesn't exist in 3.3.5a: the professions are the rows under two headers of the skill list.
local _, ns = ...

-- The headers' English names, used when the client's global strings are missing.
local FALLBACK_HEADERS = { "Professions", "Secondary Skills" }

local function withoutColon(text)
    return text and (text:gsub(":%s*$", ""))
end

--- The localized names of the headers the professions are listed under, read from the client's global strings.
---@param api table the WoW API and global strings
---@return table<string, boolean>
function ns.ProfessionHeaders(api)
    local headers = {}
    for _, key in ipairs({ "TRADE_SKILLS", "SECONDARY_SKILLS" }) do
        local header = withoutColon(api[key])
        if header then
            headers[header] = true
        end
    end
    if next(headers) == nil then
        for _, header in ipairs(FALLBACK_HEADERS) do
            headers[header] = true
        end
    end
    return headers
end

--- The professions section: each skill line under the professions and secondary skills headers, in the game's order.
--- A collapsed header hides its rows, so the section is then unavailable rather than missing professions.
---@param api table the WoW API
---@param now number seconds since 1970
---@return table
function ns.CaptureProfessions(api, now)
    local count = api.GetNumSkillLines()
    if not count or count == 0 then
        return ns.Unavailable("not-loaded", now)
    end
    local headers = ns.ProfessionHeaders(api)
    local items, current = {}, nil
    for index = 1, count do
        local name, isHeader, isExpanded, rank, _, _, maxRank = api.GetSkillLineInfo(index)
        if isHeader then
            current = headers[withoutColon(name)] and name or nil
            if current and not isExpanded then
                return ns.Unavailable("collapsed", now)
            end
        elseif current then
            items[#items + 1] = { name = name, header = current, rank = rank, maxRank = maxRank }
        end
    end
    return ns.Observed(now, { items = items })
end
