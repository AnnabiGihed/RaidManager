-- Author: Gihed Annabi
-- Purpose: Captures who the character is: identity, guild, client and the server's clock (WoW 3.3.5a API).
-- Each function receives the WoW API as a table, the game's globals in the client and stubs in the tests.
local _, ns = ...

--- The identity section: GUID, level and the locale-independent class, race and faction tokens.
---@param api table the WoW API
---@param now number seconds since 1970
---@return table
function ns.CaptureIdentity(api, now)
    local guid = api.UnitGUID("player")
    local _, class = api.UnitClass("player")
    local _, race = api.UnitRace("player")
    local faction = api.UnitFactionGroup("player")
    if not (guid and class and race and faction) then
        return ns.Unavailable("not-loaded", now)
    end
    return ns.Observed(now, {
        guid = guid,
        level = api.UnitLevel("player"),
        class = class,
        race = race,
        faction = faction,
        sex = api.UnitSex("player"),
    })
end

--- The guild section; GetGuildInfo can return nothing shortly after login, which is unavailable, not "no guild".
---@param api table the WoW API
---@param now number seconds since 1970
---@return table
function ns.CaptureGuild(api, now)
    if not api.IsInGuild() then
        return ns.Observed(now, { inGuild = false })
    end
    local name, rank, rankIndex = api.GetGuildInfo("player")
    if not name then
        return ns.Unavailable("not-loaded", now)
    end
    return ns.Observed(now, { inGuild = true, name = name, rank = rank, rankIndex = rankIndex })
end

--- The client's locale and build, so RaidManager can map the localized names.
---@param api table the WoW API
---@return table
function ns.CaptureClient(api)
    local _, build = api.GetBuildInfo()
    return { locale = api.GetLocale(), build = build }
end

--- The game server's hour and minute at the moment of the capture, to detect a wrong local clock.
---@param api table the WoW API
---@param now number seconds since 1970
---@return table
function ns.CaptureServerTime(api, now)
    local hour, minute = api.GetGameTime()
    return { hour = hour, minute = minute, observedAt = now }
end
