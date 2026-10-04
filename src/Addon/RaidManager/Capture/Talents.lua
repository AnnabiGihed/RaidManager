-- Author: Gihed Annabi
-- Purpose: Captures the talent groups (dual specialization) with points, ranks and glyphs (WoW 3.3.5a API).
local _, ns = ...

--- One talent tree of a group: its name, the points spent and one rank digit per talent, in GetTalentInfo order.
---@param api table the WoW API
---@param tab number the tree, 1 to GetNumTalentTabs()
---@param group number the talent group
---@return table|nil the tree, or nil while the game hasn't loaded it
function ns.CaptureTalentTab(api, tab, group)
    local name, _, pointsSpent = api.GetTalentTabInfo(tab, false, false, group)
    if not name then
        return nil
    end
    local ranks = {}
    for index = 1, api.GetNumTalents(tab, false, false) do
        local _, _, _, _, rank = api.GetTalentInfo(tab, index, false, false, group)
        ranks[index] = tostring(rank or 0)
    end
    return { name = name, pointsSpent = pointsSpent, ranks = table.concat(ranks) }
end

--- One glyph socket of a group: its type, whether it is unlocked, and its glyph or empty = true.
---@param api table the WoW API
---@param socket number 1 to GetNumGlyphSockets()
---@param group number the talent group
---@return table
function ns.CaptureGlyph(api, socket, group)
    local enabled, glyphType, spellId = api.GetGlyphSocketInfo(socket, group)
    local glyph = { socket = socket, type = glyphType, enabled = enabled and true or false }
    if glyph.enabled and spellId then
        glyph.spellId = spellId
    elseif glyph.enabled then
        glyph.empty = true
    end
    return glyph
end

--- The talents section: every talent group, flagging the active one.
---@param api table the WoW API
---@param now number seconds since 1970
---@return table
function ns.CaptureTalents(api, now)
    local groupCount, tabCount = api.GetNumTalentGroups(), api.GetNumTalentTabs()
    if not groupCount or groupCount == 0 or not tabCount or tabCount == 0 then
        return ns.Unavailable("not-loaded", now)
    end
    local groups = {}
    for group = 1, groupCount do
        local tabs, glyphs = {}, {}
        for tab = 1, tabCount do
            tabs[tab] = ns.CaptureTalentTab(api, tab, group)
            if not tabs[tab] then
                return ns.Unavailable("not-loaded", now)
            end
        end
        for socket = 1, api.GetNumGlyphSockets() do
            glyphs[socket] = ns.CaptureGlyph(api, socket, group)
        end
        groups[group] = { group = group, tabs = tabs, glyphs = glyphs }
    end
    return ns.Observed(now, { activeGroup = api.GetActiveTalentGroup(), groups = groups })
end
