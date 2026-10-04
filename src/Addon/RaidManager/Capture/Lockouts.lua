-- Author: Gihed Annabi
-- Purpose: Captures the saved instances, raids included, with their reset data (WoW 3.3.5a API).
-- The game answers RequestRaidInfo with UPDATE_INSTANCE_INFO; the saves are read then, never before.
local _, ns = ...

-- RequestRaidInfo asks the server; once every few seconds is enough.
ns.RAID_INFO_INTERVAL = 5

--- Asks the game for the saved instances, at most once per interval.
---@param api table the WoW API
---@param now number seconds since 1970
---@return boolean whether the request was sent
function ns.RequestRaidInfo(api, now)
    if ns.lastRaidInfoRequest and now - ns.lastRaidInfoRequest < ns.RAID_INFO_INTERVAL then
        return false
    end
    ns.lastRaidInfoRequest = now
    api.RequestRaidInfo()
    return true
end

--- The lockouts section, read after the game answered: one entry per saved instance, in the game's order.
--- complete is false when an instance the game counted couldn't be read, so RaidManager keeps the earlier saves.
---@param api table the WoW API
---@param now number seconds since 1970
---@return table
function ns.CaptureLockouts(api, now)
    local items, complete = {}, true
    for index = 1, api.GetNumSavedInstances() do
        -- The ten values of GetSavedInstanceInfo in 3.3.5a, read by position.
        local name, lockoutId, resetSeconds, difficulty, locked, extended, idMostSig, isRaid, maxPlayers, sizeName =
            api.GetSavedInstanceInfo(index)
        if name then
            items[#items + 1] = {
                name = name,
                lockoutId = lockoutId,
                idMostSig = idMostSig,
                resetSeconds = resetSeconds,
                difficulty = difficulty,
                difficultyName = sizeName,
                maxPlayers = maxPlayers,
                isRaid = isRaid and true or false,
                locked = locked and true or false,
                extended = extended and true or false,
            }
        else
            complete = false
        end
    end
    return ns.Observed(now, { complete = complete, items = items })
end
