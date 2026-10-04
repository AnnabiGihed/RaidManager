-- Author: Gihed Annabi
-- Purpose: Captures the saved equipment sets, the character's loadouts, from the equipment manager (WoW 3.3.5a API).
local _, ns = ...

-- FrameXML's EquipmentManager sentinels, used when the client doesn't define them.
local DEFAULT_EMPTY_SLOT, DEFAULT_IGNORED_SLOT, DEFAULT_ITEM_MISSING = 0, 1, -1

--- One slot of a set: an item with its location, empty, ignored, or unavailable when it can't be read now.
---@param api table the WoW API
---@param slot number 1 to 19
---@param itemId number the set's item id for the slot, or a sentinel
---@param location any the set's location for the slot, as GetEquipmentSetLocations gives it
---@return table
function ns.CaptureSetSlot(api, slot, itemId, location)
    if itemId == (api.EQUIPMENT_SET_IGNORED_SLOT or DEFAULT_IGNORED_SLOT) then
        return { slot = slot, ignored = true }
    end
    if not itemId or itemId == (api.EQUIPMENT_SET_EMPTY_SLOT or DEFAULT_EMPTY_SLOT) then
        return { slot = slot, empty = true }
    end
    if location == nil or location == (api.EQUIPMENT_SET_ITEM_MISSING or DEFAULT_ITEM_MISSING) then
        local missing = { slot = slot, status = "unavailable", reason = "missing" }
        -- Warmane's client gives -1 instead of the id of an item that is gone; only a real id is kept (#503).
        if itemId > DEFAULT_IGNORED_SLOT then
            missing.itemId = itemId
        end
        return missing
    end
    local onPlayer, inBank, inBags, itemSlot, bag = api.EquipmentManager_UnpackLocation(location)
    if inBank then
        -- The bank is only readable while it is open.
        return { slot = slot, status = "unavailable", reason = "in-bank", itemId = itemId, location = "bank" }
    end
    local link, where
    if inBags then
        link, where = api.GetContainerItemLink(bag, itemSlot), "bags"
    elseif onPlayer then
        link, where = api.GetInventoryItemLink("player", itemSlot), "equipped"
    end
    local item = ns.ParseItemLink(link)
    if not item then
        return { slot = slot, status = "unavailable", reason = "not-cached", itemId = itemId }
    end
    item.slot = slot
    item.location = where
    return item
end

--- The equipmentSets section: each saved set with its name, icon and slots 1 to 19.
---@param api table the WoW API
---@param now number seconds since 1970
---@return table
function ns.CaptureEquipmentSets(api, now)
    local items = {}
    for index = 1, api.GetNumEquipmentSets() do
        local name, icon = api.GetEquipmentSetInfo(index)
        if not name then
            return ns.Unavailable("not-loaded", now)
        end
        local itemIds = api.GetEquipmentSetItemIDs(name) or {}
        local locations = api.GetEquipmentSetLocations(name) or {}
        local slots = {}
        for slot = 1, ns.EQUIPPED_SLOTS do
            slots[slot] = ns.CaptureSetSlot(api, slot, itemIds[slot], locations[slot])
        end
        items[#items + 1] = { name = name, icon = icon, slots = slots }
    end
    return ns.Observed(now, { items = items })
end
